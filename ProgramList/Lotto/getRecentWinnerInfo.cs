using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using scheduleProgram.Services;

namespace scheduleProgram.ProgramList.Lotto
{
    // schedule: manual
    // enabled: false
    internal class getRecentWinnerInfo
    {
        private string memo = "최신 당첨이력 재조회 및 업데이트";
        private readonly LottoApiService _apiService;
        private readonly LottoDatabaseService _dbService;

        public getRecentWinnerInfo()
        {
            _apiService = new LottoApiService();
            _dbService = new LottoDatabaseService();
        }

        public async void Execute()
        {
            var programName = "Lotto";
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 최신 당첨이력 재조회 및 업데이트 시작");
            MainForm.LogToHistory(programName, "최신 당첨이력 재조회 및 업데이트 시작");

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

                // 가장 최신 회차만 재조회
                var latestRound = await _dbService.GetLatestRoundAsync();

                var statusMsg = $"재조회 대상: {latestRound}회차 (최신 회차 1개)";
                Console.WriteLine(statusMsg);
                MainForm.LogToHistory(programName, statusMsg);

                bool isUpdated = false;
                bool hasError = false;

                try
                {
                    var queryMsg = $"?? 로또 {latestRound}회차 재조회 중...";
                    Console.WriteLine(queryMsg);
                    MainForm.LogToHistory(programName, queryMsg);

                    // API 호출
                    var lottoResult = await _apiService.GetLottoNumberAsync(latestRound);
                    
                    if (lottoResult == null)
                    {
                        var apiErrorMsg = $"? {latestRound}회차 API 호출 오류";
                        Console.WriteLine(apiErrorMsg);
                        MainForm.LogToHistory(programName, apiErrorMsg);
                        hasError = true;
                        return;
                    }
                    
                    if (lottoResult.IsDataNotFound)
                    {
                        var noDataMsg = $"?? {latestRound}회차 데이터가 아직 없음";
                        Console.WriteLine(noDataMsg);
                        MainForm.LogToHistory(programName, noDataMsg);
                        return;
                    }

                    // 기존 데이터 삭제 및 새 데이터 저장
                    var deleted = await DeleteWinnerDataAsync(latestRound);
                    if (!deleted)
                    {
                        var deleteErrorMsg = $"? {latestRound}회차 기존 데이터 삭제 실패";
                        Console.WriteLine(deleteErrorMsg);
                        MainForm.LogToHistory(programName, deleteErrorMsg);
                        hasError = true;
                        return;
                    }

                    // 새로운 데이터 저장
                    var saved = await _dbService.SaveWinnerNumbersAsync(latestRound, lottoResult.Numbers, lottoResult.DrawDate, lottoResult.TotalPrice, lottoResult.Winner, lottoResult.WinnerPrice);
                    if (saved)
                    {
                        isUpdated = true;
                        var updateMsg = $"? {latestRound}회차 업데이트 완료 - 추첨일: {lottoResult.DrawDate:yyyy-MM-dd}";
                        Console.WriteLine(updateMsg);
                        MainForm.LogToHistory(programName, updateMsg);
                        
                        // 당첨번호 출력을 위한 메시지 포맷팅
                        var normalNumbers = lottoResult.Numbers.Where(n => !n.IsBonusNumber).Select(n => n.Number).ToArray();
                        var bonusNumber = lottoResult.Numbers.FirstOrDefault(n => n.IsBonusNumber)?.Number ?? 0;
                        var numbersMsg = $"   당첨번호: {string.Join(", ", normalNumbers)} + 보너스: {bonusNumber}";
                        var priceMsg = $"   상금정보: 총상금 {lottoResult.TotalPrice:N0}원, 1등 {lottoResult.Winner}명, 1등당 {lottoResult.WinnerPrice:N0}원";
                        
                        Console.WriteLine(numbersMsg);
                        Console.WriteLine(priceMsg);
                        MainForm.LogToHistory(programName, numbersMsg);
                        MainForm.LogToHistory(programName, priceMsg);
                    }
                    else
                    {
                        var saveErrorMsg = $"? {latestRound}회차 새 데이터 저장 실패";
                        Console.WriteLine(saveErrorMsg);
                        MainForm.LogToHistory(programName, saveErrorMsg);
                        hasError = true;
                    }
                }
                catch (Exception ex)
                {
                    var exceptionMsg = $"? {latestRound}회차 처리 중 오류: {ex.Message}";
                    Console.WriteLine(exceptionMsg);
                    MainForm.LogToHistory(programName, exceptionMsg);
                    hasError = true;
                }

                // 최종 결과 출력
                var summaryMsg = isUpdated ? 
                    $"? 최신 당첨이력 재조회 완료 - {latestRound}회차 업데이트 성공" :
                    hasError ? 
                        $"? 최신 당첨이력 재조회 실패 - {latestRound}회차 처리 오류" :
                        $"?? 최신 당첨이력 재조회 완료 - {latestRound}회차 변경사항 없음";
                
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {summaryMsg}");
                MainForm.LogToHistory(programName, summaryMsg);

                // 업데이트된 데이터가 있으면 관련 테이블들도 재계산
                if (isUpdated)
                {
                    var recalcMsg = "?? 업데이트된 데이터가 있어 관련 테이블들을 재계산합니다...";
                    Console.WriteLine(recalcMsg);
                    MainForm.LogToHistory(programName, recalcMsg);

                    // 1. 전체 확률 재계산
                    Console.WriteLine("?? 전체 확률 재계산 중...");
                    MainForm.LogToHistory(programName, "전체 확률 재계산 중...");
                    await _dbService.CalculateAndUpdateProbabilitiesAsync();

                    // 2. 최신 회차의 winner_rate_history 재계산
                    Console.WriteLine("?? 최신 회차의 당첨번호 확률 재계산 중...");
                    MainForm.LogToHistory(programName, "최신 회차의 당첨번호 확률 재계산 중...");
                    await _dbService.CalculateAndSaveRoundWinnerRateAsync(latestRound);

                    // 3. 번호별 등장 간격 재계산
                    Console.WriteLine("?? 번호별 등장 간격 재계산 중...");
                    MainForm.LogToHistory(programName, "번호별 등장 간격 재계산 중...");
                    await _dbService.CalculateAndSaveNumberFrequencyAsync();

                    // 4. 가중치 적용 확률 재계산
                    Console.WriteLine("?? 가중치 적용 확률 재계산 중...");
                    MainForm.LogToHistory(programName, "가중치 적용 확률 재계산 중...");
                    await _dbService.CalculateAndSaveWeightedProbabilitiesAsync();

                    // 5. 추천번호 재계산
                    Console.WriteLine("?? 추천번호 재계산 중...");
                    MainForm.LogToHistory(programName, "추천번호 재계산 중...");
                    var nextRecommendRound = latestRound + 1;
                    await _dbService.CalculateAndSaveRecommendNumbersAsync(nextRecommendRound);

                    var completeMsg = "? 모든 관련 테이블 재계산 완료!";
                    Console.WriteLine(completeMsg);
                    MainForm.LogToHistory(programName, completeMsg);
                }
                else
                {
                    var noUpdateMsg = "?? 업데이트된 데이터가 없어 재계산을 생략합니다.";
                    Console.WriteLine(noUpdateMsg);
                    MainForm.LogToHistory(programName, noUpdateMsg);
                }
            }
            catch (Exception ex)
            {
                var exceptionMsg = $"최신 당첨이력 재조회 오류: {ex.Message}";
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {exceptionMsg}");
                MainForm.LogToHistory(programName, $"? {exceptionMsg}");
                throw; // 예외를 다시 던져서 상위에서 처리하도록 함
            }
        }

        /// <summary>
        /// 특정 회차의 winner_history 데이터를 삭제합니다
        /// </summary>
        private async Task<bool> DeleteWinnerDataAsync(int round)
        {
            try
            {
                using var connection = new MySqlConnector.MySqlConnection("Server=223.130.128.14;Port=3306;Database=lottoapp;Uid=root;Pwd=minwook0409;CharSet=utf8mb4;");
                await connection.OpenAsync();

                var query = "DELETE FROM winner_history WHERE round = @round";
                using var command = new MySqlConnector.MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@round", round);
                
                var deletedRows = await command.ExecuteNonQueryAsync();
                Console.WriteLine($"??? {round}회차 기존 데이터 삭제: {deletedRows}행");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? {round}회차 데이터 삭제 실패: {ex.Message}");
                return false;
            }
        }
    }
}