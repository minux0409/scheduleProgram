using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using scheduleProgram.Services;

namespace scheduleProgram.ProgramList.Lotto
{
    // schedule: once
    // enabled: false
    internal class getRecommandDate
    {
        private string memo = "추천번호 이력 재생성";
        private readonly LottoDatabaseService _dbService;

        public getRecommandDate()
        {
            _dbService = new LottoDatabaseService();
        }

        public async void Execute()
        {
            var programName = "Lotto";
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 추천번호 이력 재생성 시작");
            MainForm.LogToHistory(programName, "추천번호 이력 재생성 시작");

            try
            {
                // DB 연결 테스트
                var dbConnected = await _dbService.TestConnectionAsync();
                if (!dbConnected)
                {
                    var errorMsg = "로또 DB 연결 실패 - 작업을 중단합니다.";
                    Console.WriteLine(errorMsg);
                    MainForm.LogToHistory(programName, errorMsg);
                    return;
                }

                // 가장 최신 회차 조회
                var latestRound = await _dbService.GetLatestRoundAsync();
                var maxRound = latestRound + 1; // 다음 회차까지

                var statusMsg = $"추천번호 이력 재생성 범위: 27회차 ~ {maxRound}회차";
                Console.WriteLine(statusMsg);
                MainForm.LogToHistory(programName, statusMsg);

                var processedCount = 0;
                var errorCount = 0;

                // 27회차부터 maxRound까지 반복 처리
                for (int currentRound = 27; currentRound <= maxRound; currentRound++)
                {
                    try
                    {
                        var processMsg = $"🔄 {currentRound}회차 추천번호 이력 재생성 중...";
                        Console.WriteLine(processMsg);
                        MainForm.LogToHistory(programName, processMsg);

                        // 1. recommand_history에서 현재 회차 데이터 조회 (기본 확률로 사용)
                        var recommendHistory = await _dbService.GetRecommendHistoryAsync(currentRound);

                        // 2. winner_history에서 27회차부터 현재 회차까지의 번호별 등장 간격 계산
                        var oldNumbers = await _dbService.GetNumberFrequencyUntilRoundAsync(currentRound);

                        // 3. winner_rate_history에서 현재 회차의 순위별 빈도 데이터 조회
                        var rankFrequency = await _dbService.GetRankFrequencyForRoundAsync(currentRound);

                        // 4. 가중치 적용 확률 계산
                        var weightedProbabilities = _dbService.CalculateWeightedProbabilitiesForHistoricalData(
                            recommendHistory, oldNumbers, rankFrequency);

                        if (weightedProbabilities.Count == 0)
                        {
                            var skipMsg = $"⚠️ {currentRound}회차 데이터 부족으로 건너뜀";
                            Console.WriteLine(skipMsg);
                            MainForm.LogToHistory(programName, skipMsg);
                            errorCount++;
                            continue;
                        }

                        // 5. number_probability_new 테이블에 저장
                        var saved = await _dbService.SaveWeightedProbabilitiesAsync(currentRound, weightedProbabilities);

                        if (saved)
                        {
                            processedCount++;
                            var successMsg = $"✅ {currentRound}회차 가중치 확률 재생성 완료";
                            Console.WriteLine(successMsg);
                            MainForm.LogToHistory(programName, successMsg);

                            // 추가 정보 출력
                            var avgProbability = weightedProbabilities.Values.Average();
                            var maxProbability = weightedProbabilities.Values.Max();
                            var minProbability = weightedProbabilities.Values.Min();
                            var statsMsg = $"   평균: {avgProbability:F2}%, 최대: {maxProbability:F2}%, 최소: {minProbability:F2}%";
                            Console.WriteLine(statsMsg);
                        }
                        else
                        {
                            var errorMsg = $"❌ {currentRound}회차 저장 실패";
                            Console.WriteLine(errorMsg);
                            MainForm.LogToHistory(programName, errorMsg);
                            errorCount++;
                        }

                        // 과도한 DB 부하 방지를 위한 딜레이
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        var exceptionMsg = $"❌ {currentRound}회차 처리 중 오류: {ex.Message}";
                        Console.WriteLine(exceptionMsg);
                        MainForm.LogToHistory(programName, exceptionMsg);
                        errorCount++;
                        continue; // 다음 회차 계속 처리
                    }
                }

                // 최종 결과 출력
                var summaryMsg = $"추천번호 이력 재생성 완료 - 성공: {processedCount}개, 실패: {errorCount}개, 전체: {maxRound - 27 + 1}개";
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {summaryMsg}");
                MainForm.LogToHistory(programName, summaryMsg);

                if (processedCount > 0)
                {
                    var resultMsg = $"🎯 총 {processedCount}개 회차의 가중치 확률 데이터가 number_probability_new 테이블에 저장되었습니다.";
                    Console.WriteLine(resultMsg);
                    MainForm.LogToHistory(programName, resultMsg);
                }

                if (errorCount > 0)
                {
                    var warningMsg = $"⚠️ {errorCount}개 회차에서 오류가 발생했습니다. 로그를 확인해주세요.";
                    Console.WriteLine(warningMsg);
                    MainForm.LogToHistory(programName, warningMsg);
                }
            }
            catch (Exception ex)
            {
                var exceptionMsg = $"추천번호 이력 재생성 오류: {ex.Message}";
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {exceptionMsg}");
                MainForm.LogToHistory(programName, $"❌ {exceptionMsg}");
                throw; // 예외를 다시 던져서 상위에서 처리하도록 함
            }
        }
    }
}