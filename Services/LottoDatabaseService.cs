using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace scheduleProgram.Services
{
    /// <summary>
    /// 로또 데이터베이스 연결 및 관리 서비스
    /// </summary>
    public class LottoDatabaseService
    {
        private readonly string _connectionString;

        public LottoDatabaseService()
        {
            // 로또 전용 DB 연결 문자열
            _connectionString = "Server=223.130.128.14;Port=3306;Database=lottoapp;Uid=root;Pwd=minwook0409;CharSet=utf8mb4;";
        }

        /// <summary>
        /// 데이터베이스 연결 테스트
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"로또 DB 연결 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 가장 최신 회차 조회
        /// </summary>
        public async Task<int> GetLatestRoundAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = "SELECT MAX(round) FROM winner_history";
                using var command = new MySqlCommand(query, connection);
                
                var result = await command.ExecuteScalarAsync();
                return result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"최신 회차 조회 실패: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 당첨 번호 저장
        /// </summary>
        public async Task<bool> SaveWinnerNumbersAsync(int round, List<WinnerNumber> numbers, DateTime roundDate)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = await connection.BeginTransactionAsync();
                try
                {
                    var query = @"
                        INSERT INTO winner_history (round, seq, number, bonusFlag, roundDate, inDate) 
                        VALUES (@round, @seq, @number, @bonusFlag, @roundDate, NOW())";

                    // 모든 번호 저장 (일반 번호 6개 + 보너스 번호 1개)
                    foreach (var number in numbers)
                    {
                        using var command = new MySqlCommand(query, connection, transaction);
                        command.Parameters.AddWithValue("@round", round);
                        command.Parameters.AddWithValue("@seq", number.Seq);
                        command.Parameters.AddWithValue("@number", number.Number);
                        command.Parameters.AddWithValue("@bonusFlag", number.IsBonusNumber ? "Y" : "N"); // Y/N으로 변경
                        command.Parameters.AddWithValue("@roundDate", roundDate.ToString("yyyy-MM-dd"));
                        
                        await command.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();
                    
                    var normalCount = numbers.Count(n => !n.IsBonusNumber);
                    var bonusCount = numbers.Count(n => n.IsBonusNumber);
                    Console.WriteLine($"로또 {round}회차 당첨번호 저장 완료 (일반: {normalCount}개, 보너스: {bonusCount}개, 추첨일: {roundDate:yyyy-MM-dd})");
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"당첨번호 저장 실패 - {round}회차: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 특정 회차가 이미 저장되어 있는지 확인
        /// </summary>
        public async Task<bool> IsRoundExistsAsync(int round)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = "SELECT COUNT(*) FROM winner_history WHERE round = @round";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@round", round);
                
                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"회차 존재 확인 실패 - {round}회차: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 각 숫자별 당첨 횟수를 조회합니다
        /// </summary>
        public async Task<Dictionary<int, int>> GetNumberCountsAsync()
        {
            var numberCounts = new Dictionary<int, int>();
            
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // 1~45까지 모든 숫자를 초기화 (보너스 번호 제외)
                for (int i = 1; i <= 45; i++)
                {
                    numberCounts[i] = 0;
                }

                var query = @"
                    SELECT number, COUNT(*) as count_num 
                    FROM winner_history 
                    WHERE bonusFlag = 'N' 
                    GROUP BY number";

                using var command = new MySqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    var number = reader.GetInt32("number");
                    var count = reader.GetInt32("count_num");
                    if (number >= 1 && number <= 45)
                    {
                        numberCounts[number] = count;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"숫자별 당첨 횟수 조회 실패: {ex.Message}");
            }

            return numberCounts;
        }

        /// <summary>
        /// 전체 회차 수를 조회합니다
        /// </summary>
        public async Task<int> GetTotalRoundsAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = "SELECT COUNT(DISTINCT round) FROM winner_history";
                using var command = new MySqlCommand(query, connection);
                
                var result = await command.ExecuteScalarAsync();
                return result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"전체 회차 수 조회 실패: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// number_probability_status 테이블에 확률 데이터를 업데이트합니다 (DELETE 후 INSERT 방식)
        /// </summary>
        public async Task<bool> UpdateNumberProbabilityAsync(Dictionary<int, NumberProbability> probabilityData)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = await connection.BeginTransactionAsync();
                try
                {
                    // 1단계: 기존 데이터 전체 삭제
                    var deleteQuery = "DELETE FROM number_probability_status";
                    using var deleteCommand = new MySqlCommand(deleteQuery, connection, transaction);
                    var deletedRows = await deleteCommand.ExecuteNonQueryAsync();
                    Console.WriteLine($"🗑️ 기존 확률 데이터 삭제: {deletedRows}행");

                    // 2단계: 새로운 데이터 INSERT
                    var insertQuery = @"
                        INSERT INTO number_probability_status (number, count, probability, updateDate) 
                        VALUES (@number, @count, @probability, NOW())";

                    int insertedCount = 0;
                    foreach (var kvp in probabilityData)
                    {
                        var number = kvp.Key;
                        var data = kvp.Value;

                        using var insertCommand = new MySqlCommand(insertQuery, connection, transaction);
                        insertCommand.Parameters.AddWithValue("@number", number);
                        insertCommand.Parameters.AddWithValue("@count", data.Count);
                        insertCommand.Parameters.AddWithValue("@probability", data.Probability);
                        
                        await insertCommand.ExecuteNonQueryAsync();
                        insertedCount++;
                    }

                    await transaction.CommitAsync();
                    Console.WriteLine($"✅ 새로운 확률 데이터 저장 완료: {insertedCount}행");
                    Console.WriteLine($"📊 확률 데이터 업데이트 완료 - 삭제: {deletedRows}행, 추가: {insertedCount}행");
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"❌ 트랜잭션 롤백: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 숫자 확률 데이터 업데이트 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 확률을 계산합니다
        /// </summary>
        public async Task<bool> CalculateAndUpdateProbabilitiesAsync()
        {
            try
            {
                Console.WriteLine("📊 로또 숫자 확률 계산 시작...");

                // 1. 각 숫자별 당첨 횟수 조회
                var numberCounts = await GetNumberCountsAsync();
                
                // 2. 전체 회차 수 조회
                var totalRounds = await GetTotalRoundsAsync();
                
                if (totalRounds == 0)
                {
                    Console.WriteLine("⚠️ 회차 데이터가 없어 확률 계산을 건너뜁니다.");
                    return true;
                }

                Console.WriteLine($"📈 전체 회차: {totalRounds}, 숫자별 당첨 횟수 조회 완료");

                // 3. 확률 계산
                var probabilityData = CalculateProbabilities(numberCounts, totalRounds);

                // 4. DB 업데이트
                var updated = await UpdateNumberProbabilityAsync(probabilityData);

                if (updated)
                {
                    // 확률 정보 출력
                    var sortedByProbability = probabilityData.OrderByDescending(x => x.Value.Probability).Take(10);
                    Console.WriteLine("🎯 확률 높은 숫자 TOP 10:");
                    foreach (var item in sortedByProbability)
                    {
                        Console.WriteLine($"   숫자 {item.Key}: {item.Value.Probability:F2}% (당첨횟수: {item.Value.Count})");
                    }
                }

                return updated;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 확률 계산 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 확률 계산 로직 - 관측 확률 역산 방식
        /// </summary>
        private Dictionary<int, NumberProbability> CalculateProbabilities(Dictionary<int, int> numberCounts, int totalRounds)
        {
            var result = new Dictionary<int, NumberProbability>();
            
            if (totalRounds == 0)
            {
                // 회차가 없으면 모든 숫자 동일 확률
                for (int i = 1; i <= 45; i++)
                {
                    result[i] = new NumberProbability { Count = 0, Probability = 100.0 / 45 }; // 2.22%
                }
                return result;
            }

            // 전체 뽑힌 번호 수 (각 회차당 6개씩)
            int totalNumbers = totalRounds * 6;
            
            // 1단계: 각 숫자별 관측 확률 계산
            var observedProbabilities = new Dictionary<int, double>();
            for (int i = 1; i <= 45; i++)
            {
                // 관측 확률 = 해당 숫자 등장 횟수 / 전체 번호 수
                observedProbabilities[i] = (double)numberCounts[i] / totalNumbers;
            }

            // 2단계: 관측 확률의 역수 계산 (0이면 매우 큰 값으로 설정)
            var inverseProbabilities = new Dictionary<int, double>();
            for (int i = 1; i <= 45; i++)
            {
                if (observedProbabilities[i] > 0)
                {
                    // 관측 확률의 역수
                    inverseProbabilities[i] = 1.0 / observedProbabilities[i];
                }
                else
                {
                    // 한 번도 나오지 않은 숫자는 매우 높은 가중치
                    inverseProbabilities[i] = totalNumbers; // 최대값으로 설정
                }
            }

            // 3단계: 정규화 (전체 합이 1이 되도록)
            double totalInverse = inverseProbabilities.Values.Sum();
            
            for (int i = 1; i <= 45; i++)
            {
                // 정규화된 확률 계산 (백분율로 변환)
                double normalizedProbability = (inverseProbabilities[i] / totalInverse) * 100.0;
                
                result[i] = new NumberProbability 
                { 
                    Count = numberCounts[i], 
                    Probability = normalizedProbability 
                };
            }

            // 4단계: 계산 정보 출력
            Console.WriteLine($"📊 확률 계산 완료:");
            Console.WriteLine($"   - 전체 회차: {totalRounds}");
            Console.WriteLine($"   - 전체 번호 수: {totalNumbers}");
            Console.WriteLine($"   - 이론 확률: {100.0/45:F2}%");
            
            // 가장 높은/낮은 관측 확률
            var maxObserved = observedProbabilities.Values.Max();
            var minObserved = observedProbabilities.Values.Min();
            Console.WriteLine($"   - 최고 관측 확률: {maxObserved*100:F2}%");
            Console.WriteLine($"   - 최저 관측 확률: {minObserved*100:F2}%");

            return result;
        }

        /// <summary>
        /// 특정 회차 이전의 당첨 횟수를 조회합니다
        /// </summary>
        public async Task<Dictionary<int, int>> GetNumberCountsBeforeRoundAsync(int targetRound)
        {
            var numberCounts = new Dictionary<int, int>();
            
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // 1~45까지 모든 숫자를 초기화 (보너스 번호 제외)
                for (int i = 1; i <= 45; i++)
                {
                    numberCounts[i] = 0;
                }

                var query = @"
                    SELECT number, COUNT(*) as count_num 
                    FROM winner_history 
                    WHERE bonusFlag = 'N' AND round < @targetRound
                    GROUP BY number";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@targetRound", targetRound);
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    var number = reader.GetInt32("number");
                    var count = reader.GetInt32("count_num");
                    if (number >= 1 && number <= 45)
                    {
                        numberCounts[number] = count;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"특정 회차 이전 당첨 횟수 조회 실패: {ex.Message}");
            }

            return numberCounts;
        }

        /// <summary>
        /// 특정 회차 이전의 총 회차 수를 조회합니다
        /// </summary>
        public async Task<int> GetTotalRoundsBeforeAsync(int targetRound)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = "SELECT COUNT(DISTINCT round) FROM winner_history WHERE round < @targetRound";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@targetRound", targetRound);
                
                var result = await command.ExecuteScalarAsync();
                return result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"특정 회차 이전 총 회차 수 조회 실패: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 특정 회차의 당첨 번호들을 조회합니다 (보너스 제외)
        /// </summary>
        public async Task<List<WinnerNumber>> GetWinnerNumbersByRoundAsync(int round)
        {
            var winnerNumbers = new List<WinnerNumber>();
            
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT seq, number 
                    FROM winner_history 
                    WHERE round = @round AND bonusFlag = 'N'
                    ORDER BY seq";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@round", round);
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    winnerNumbers.Add(new WinnerNumber
                    {
                        Seq = reader.GetInt32("seq"),
                        Number = reader.GetInt32("number"),
                        IsBonusNumber = false
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"특정 회차 당첨번호 조회 실패: {ex.Message}");
            }

            return winnerNumbers;
        }

        /// <summary>
        /// winner_rate_history 테이블에 회차별 당첨번호 확률을 저장합니다
        /// </summary>
        public async Task<bool> SaveWinnerRateHistoryAsync(int round, List<WinnerRateProbability> rateData)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = await connection.BeginTransactionAsync();
                try
                {
                    // 1단계: 해당 회차의 기존 데이터 삭제 (있다면)
                    var deleteQuery = "DELETE FROM winner_rate_history WHERE round = @round";
                    using var deleteCommand = new MySqlCommand(deleteQuery, connection, transaction);
                    deleteCommand.Parameters.AddWithValue("@round", round);
                    var deletedRows = await deleteCommand.ExecuteNonQueryAsync();
                    
                    if (deletedRows > 0)
                    {
                        Console.WriteLine($"🗑️ {round}회차 기존 확률 데이터 삭제: {deletedRows}행");
                    }

                    // 2단계: 새로운 데이터 INSERT
                    var insertQuery = @"
                        INSERT INTO winner_rate_history (round, seq, number, probability, `rank`, inDate) 
                        VALUES (@round, @seq, @number, @probability, @rank, NOW())";

                    int insertedCount = 0;
                    foreach (var data in rateData)
                    {
                        using var insertCommand = new MySqlCommand(insertQuery, connection, transaction);
                        insertCommand.Parameters.AddWithValue("@round", round);
                        insertCommand.Parameters.AddWithValue("@seq", data.Seq);
                        insertCommand.Parameters.AddWithValue("@number", data.Number);
                        insertCommand.Parameters.AddWithValue("@probability", data.Probability);
                        insertCommand.Parameters.AddWithValue("@rank", data.Rank);
                        
                        await insertCommand.ExecuteNonQueryAsync();
                        insertedCount++;
                    }

                    await transaction.CommitAsync();
                    Console.WriteLine($"✅ {round}회차 당첨번호 확률 저장 완료: {insertedCount}행");
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"❌ {round}회차 확률 저장 트랜잭션 롤백: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {round}회차 당첨번호 확률 저장 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 특정 회차의 당첨번호 확률을 계산하고 저장합니다
        /// </summary>
        public async Task<bool> CalculateAndSaveRoundWinnerRateAsync(int targetRound)
        {
            try
            {
                Console.WriteLine($"📊 {targetRound}회차 당첨번호 확률 계산 시작...");

                // 1. 해당 회차의 당첨번호들 조회
                var winnerNumbers = await GetWinnerNumbersByRoundAsync(targetRound);
                if (winnerNumbers.Count == 0)
                {
                    Console.WriteLine($"⚠️ {targetRound}회차 당첨번호 데이터가 없습니다.");
                    return false;
                }

                // 2. 해당 회차 이전의 당첨 횟수 조회
                var numberCounts = await GetNumberCountsBeforeRoundAsync(targetRound);
                
                // 3. 해당 회차 이전의 총 회차 수 조회
                var totalRounds = await GetTotalRoundsBeforeAsync(targetRound);
                
                if (totalRounds == 0)
                {
                    Console.WriteLine($"⚠️ {targetRound}회차 이전에 회차 데이터가 없어 확률 계산을 건너뜁니다.");
                    return false;
                }

                Console.WriteLine($"📈 {targetRound}회차 이전 총 회차: {totalRounds}, 당첨번호: {winnerNumbers.Count}개");

                // 4. 해당 회차 이전 데이터로 확률 계산
                var probabilityData = CalculateProbabilities(numberCounts, totalRounds);

                // 5. 동일 확률 고려한 순위 계산
                var rankMapping = CalculateRankMapping(probabilityData);

                // 6. 당첨번호들의 확률과 순위 추출
                var rateData = new List<WinnerRateProbability>();
                foreach (var winner in winnerNumbers)
                {
                    if (probabilityData.ContainsKey(winner.Number))
                    {
                        rateData.Add(new WinnerRateProbability
                        {
                            Seq = winner.Seq,
                            Number = winner.Number,
                            Probability = probabilityData[winner.Number].Probability,
                            Rank = rankMapping[winner.Number]
                        });
                    }
                }

                // 7. DB에 저장
                var saved = await SaveWinnerRateHistoryAsync(targetRound, rateData);

                if (saved)
                {
                    // 확률 및 순위 정보 출력
                    Console.WriteLine($"🎯 {targetRound}회차 당첨번호들의 예상 확률 및 순위:");
                    foreach (var data in rateData.OrderBy(x => x.Seq))
                    {
                        Console.WriteLine($"   seq {data.Seq}: 번호 {data.Number} → {data.Probability:F2}% (순위: {data.Rank}/45)");
                    }
                    
                    var avgProbability = rateData.Average(x => x.Probability);
                    var avgRank = rateData.Average(x => x.Rank);
                    Console.WriteLine($"   평균 확률: {avgProbability:F2}% (이론값: {100.0/45:F2}%)");
                    Console.WriteLine($"   평균 순위: {avgRank:F1}/45 (이론값: 23.0/45)");
                }

                return saved;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {targetRound}회차 당첨번호 확률 계산 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 특정 회차에 대한 추천번호 확률을 계산하고 저장합니다
        /// </summary>
        public async Task<bool> CalculateAndSaveRecommendNumbersAsync(int targetRound)
        {
            try
            {
                Console.WriteLine($"🎯 {targetRound}회차 추천번호 확률 계산 시작...");

                // 1. 해당 회차 이전의 당첨 횟수 조회
                var numberCounts = await GetNumberCountsBeforeRoundAsync(targetRound);
                
                // 2. 해당 회차 이전의 총 회차 수 조회
                var totalRounds = await GetTotalRoundsBeforeAsync(targetRound);
                
                if (totalRounds == 0)
                {
                    Console.WriteLine($"⚠️ {targetRound}회차 이전에 회차 데이터가 없어 추천번호 계산을 건너뜁니다.");
                    return false;
                }

                Console.WriteLine($"📈 {targetRound}회차 이전 총 회차: {totalRounds}개");

                // 3. 해당 회차 이전 데이터로 확률 계산
                var probabilityData = CalculateProbabilities(numberCounts, totalRounds);

                // 4. 동일 확률 고려한 순위 계산
                var rankMapping = CalculateRankMapping(probabilityData);

                // 5. 모든 숫자(1~45)의 확률과 순위 데이터 생성
                var recommendData = new List<RecommendNumberProbability>();
                for (int number = 1; number <= 45; number++)
                {
                    if (probabilityData.ContainsKey(number))
                    {
                        recommendData.Add(new RecommendNumberProbability
                        {
                            Number = number,
                            Probability = probabilityData[number].Probability,
                            Rank = rankMapping[number]
                        });
                    }
                }

                // 6. DB에 저장
                var saved = await SaveRecommendNumbersAsync(targetRound, recommendData);

                if (saved)
                {
                    // TOP 10 추천번호 출력
                    var top10 = recommendData.OrderBy(x => x.Rank).ThenBy(x => x.Number).Take(10);
                    Console.WriteLine($"🎯 {targetRound}회차 추천번호 TOP 10:");
                    foreach (var data in top10)
                    {
                        Console.WriteLine($"   순위 {data.Rank}: 번호 {data.Number} → {data.Probability:F2}%");
                    }
                    
                    var avgProbability = recommendData.Average(x => x.Probability);
                    Console.WriteLine($"   전체 평균 확률: {avgProbability:F2}% (이론값: {100.0/45:F2}%)");
                    Console.WriteLine($"   총 {recommendData.Count}개 숫자의 확률 및 순위 저장 완료");
                    
                    // 동일 순위 그룹 정보 출력
                    var rankGroups = recommendData.GroupBy(x => x.Rank).Where(g => g.Count() > 1).Take(3);
                    if (rankGroups.Any())
                    {
                        Console.WriteLine($"   🎲 동일 순위 그룹:");
                        foreach (var group in rankGroups)
                        {
                            var numbers = string.Join(", ", group.OrderBy(x => x.Number).Select(x => x.Number));
                            Console.WriteLine($"     순위 {group.Key}: {numbers} (확률: {group.First().Probability:F2}%)");
                        }
                    }
                }

                return saved;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {targetRound}회차 추천번호 확률 계산 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// recommand_history 테이블에 회차별 추천번호 확률을 저장합니다
        /// </summary>
        public async Task<bool> SaveRecommendNumbersAsync(int round, List<RecommendNumberProbability> recommendData)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = await connection.BeginTransactionAsync();
                try
                {
                    // 1단계: 해당 회차의 기존 데이터 삭제 (있다면)
                    var deleteQuery = "DELETE FROM recommand_history WHERE round = @round";
                    using var deleteCommand = new MySqlCommand(deleteQuery, connection, transaction);
                    deleteCommand.Parameters.AddWithValue("@round", round);
                    var deletedRows = await deleteCommand.ExecuteNonQueryAsync();
                    
                    if (deletedRows > 0)
                    {
                        Console.WriteLine($"🗑️ {round}회차 기존 추천번호 데이터 삭제: {deletedRows}행");
                    }

                    // 2단계: 새로운 데이터 INSERT
                    var insertQuery = @"
                        INSERT INTO recommand_history (round, number, probability, `rank`, inDate) 
                        VALUES (@round, @number, @probability, @rank, NOW())";

                    int insertedCount = 0;
                    foreach (var data in recommendData)
                    {
                        using var insertCommand = new MySqlCommand(insertQuery, connection, transaction);
                        insertCommand.Parameters.AddWithValue("@round", round);
                        insertCommand.Parameters.AddWithValue("@number", data.Number);
                        insertCommand.Parameters.AddWithValue("@probability", data.Probability);
                        insertCommand.Parameters.AddWithValue("@rank", data.Rank);
                        
                        await insertCommand.ExecuteNonQueryAsync();
                        insertedCount++;
                    }

                    await transaction.CommitAsync();
                    Console.WriteLine($"✅ {round}회차 추천번호 확률 저장 완료: {insertedCount}행");
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    Console.WriteLine($"❌ {round}회차 추천번호 저장 트랜잭션 롤백: {ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {round}회차 추천번호 저장 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 확률 기준으로 동일 순위를 고려한 순위 매핑을 생성합니다
        /// </summary>
        private Dictionary<int, int> CalculateRankMapping(Dictionary<int, NumberProbability> probabilityData)
        {
            // 확률 기준으로 내림차순 정렬 (높은 확률이 먼저)
            var sortedByProbability = probabilityData
                .OrderByDescending(x => x.Value.Probability)
                .ThenBy(x => x.Key) // 동일 확률일 때는 번호 순으로 정렬
                .ToList();

            var rankMapping = new Dictionary<int, int>();
            int currentRank = 1;
            double? previousProbability = null;
            
            for (int i = 0; i < sortedByProbability.Count; i++)
            {
                var item = sortedByProbability[i];
                var currentProbability = Math.Round(item.Value.Probability, 10); // 소수점 오차 보정
                
                // 이전 확률과 다르면 순위 업데이트
                if (previousProbability.HasValue && Math.Abs(currentProbability - previousProbability.Value) > 1e-10)
                {
                    currentRank = i + 1; // 현재 인덱스 + 1이 새로운 순위
                }
                
                rankMapping[item.Key] = currentRank;
                previousProbability = currentProbability;
            }

            return rankMapping;
        }
    }

    /// <summary>
    /// 당첨 번호 모델
    /// </summary>
    public class WinnerNumber
    {
        public int Seq { get; set; }
        public int Number { get; set; }
        public bool IsBonusNumber { get; set; }
    }

    /// <summary>
    /// 숫자별 확률 데이터 모델
    /// </summary>
    public class NumberProbability
    {
        public int Count { get; set; }        // 당첨 횟수
        public double Probability { get; set; } // 다음 회차 당첨 확률 (%)
    }

    /// <summary>
    /// 회차별 당첨번호 확률 데이터 모델
    /// </summary>
    public class WinnerRateProbability
    {
        public int Seq { get; set; }          // 당첨번호 순번
        public int Number { get; set; }       // 당첨번호
        public double Probability { get; set; } // 해당 회차 당시 예상 확률 (%)
        public int Rank { get; set; }         // 전체 45개 숫자 중 확률 순위 (1=가장 높음)
    }

    /// <summary>
    /// 추천번호 확률 데이터 모델
    /// </summary>
    public class RecommendNumberProbability
    {
        public int Number { get; set; }       // 추천번호
        public double Probability { get; set; } // 예상 확률 (%)
        public int Rank { get; set; }         // 전체 45개 숫자 중 확률 순위 (1=가장 높음)
    }
}