using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using scheduleProgram.Services;

namespace scheduleProgram.ProgramList.Lotto
{
    // schedule: interval
    // interval: 120
    // enabled: true
    internal class GetWinnerHistory
    {
        private string memo = "로또 당첨 이력 조회";
        private readonly LottoApiService _apiService;
        private readonly LottoDatabaseService _dbService;

        public GetWinnerHistory()
        {
            _apiService = new LottoApiService();
            _dbService = new LottoDatabaseService();
        }

        public async void Execute()
        {
            var programName = "Lotto";
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 로또 당첨 이력 조회 시작");
            MainForm.LogToHistory(programName, "로또 당첨 이력 조회 시작");

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
                var nextRound = latestRound + 1;

                var statusMsg = $"DB 최신 회차: {latestRound}, 다음 조회할 회차: {nextRound}";
                Console.WriteLine(statusMsg);
                MainForm.LogToHistory(programName, statusMsg);

                var processedCount = 0;
                var currentRound = nextRound;

                // 1. API에서 데이터가 없을 때까지 반복
                while (true)
                {
                    var queryMsg = $"로또 {currentRound}회차 조회 중...";
                    Console.WriteLine(queryMsg);
                    MainForm.LogToHistory(programName, queryMsg);

                    // 이미 저장된 회차인지 확인
                    var exists = await _dbService.IsRoundExistsAsync(currentRound);
                    if (exists)
                    {
                        var existsMsg = $"{currentRound}회차는 이미 저장되어 있음. 다음 회차로 이동.";
                        Console.WriteLine(existsMsg);
                        MainForm.LogToHistory(programName, existsMsg);
                        currentRound++;
                        continue;
                    }

                    // API 호출
                    var lottoResult = await _apiService.GetLottoNumberAsync(currentRound);
                    
                    if (lottoResult == null)
                    {
                        var apiErrorMsg = $"❌ {currentRound}회차 API 호출 오류 - 작업을 중단합니다.";
                        Console.WriteLine(apiErrorMsg);
                        MainForm.LogToHistory(programName, apiErrorMsg);
                        break; // API 오류 시 중단
                    }
                    
                    if (lottoResult.IsDataNotFound)
                    {
                        var completeMsg = $"✅ {currentRound}회차 데이터가 아직 없음 - 조회 완료.";
                        Console.WriteLine(completeMsg);
                        MainForm.LogToHistory(programName, completeMsg);
                        break; // 데이터가 없으면 정상 종료
                    }

                    // DB에 저장
                    var saved = await _dbService.SaveWinnerNumbersAsync(currentRound, lottoResult.Numbers, lottoResult.DrawDate, lottoResult.TotalPrice, lottoResult.Winner, lottoResult.WinnerPrice);
                    if (saved)
                    {
                        processedCount++;
                        var saveMsg = $"✅ {currentRound}회차 저장 완료 - 추첨일: {lottoResult.DrawDate:yyyy-MM-dd}";
                        Console.WriteLine(saveMsg);
                        MainForm.LogToHistory(programName, saveMsg);
                        
                        // 당첨번호 출력을 위한 메시지 포맷팅
                        var normalNumbers = lottoResult.Numbers.Where(n => !n.IsBonusNumber).Select(n => n.Number).ToArray();
                        var bonusNumber = lottoResult.Numbers.FirstOrDefault(n => n.IsBonusNumber)?.Number ?? 0;
                        var numbersMsg = $"   당첨번호: {string.Join(", ", normalNumbers)} + 보너스: {bonusNumber}";
                        
                        // 당첨번호 출력
                        Console.WriteLine(numbersMsg);
                        MainForm.LogToHistory(programName, numbersMsg);
                    }
                    else
                    {
                        var saveErrorMsg = $"❌ {currentRound}회차 저장 실패";
                        Console.WriteLine(saveErrorMsg);
                        MainForm.LogToHistory(programName, saveErrorMsg);
                    }

                    currentRound++;

                    // API 호출 간격 (과도한 요청 방지)
                    await Task.Delay(1000);
                }

                // 최종 로그 메시지
                var summaryMsg = $"로또 당첨 이력 조회 완료 - 처리된 회차: {processedCount}개";
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {summaryMsg}");
                MainForm.LogToHistory(programName, summaryMsg);

                // 2. while 루프 완료 후 확률 계산 및 업데이트
                if (processedCount > 0)
                {
                    var probMsg = "📊 새로운 당첨 번호가 추가되어 확률을 재계산합니다...";
                    Console.WriteLine(probMsg);
                    MainForm.LogToHistory(programName, probMsg);
                    await _dbService.CalculateAndUpdateProbabilitiesAsync();
                    
                    // 새로 추가된 회차들의 당첨번호 확률 계산
                    var calcMsg = "📊 새로 추가된 회차들의 당첨번호 확률을 계산합니다...";
                    Console.WriteLine(calcMsg);
                    MainForm.LogToHistory(programName, calcMsg);
                    var currentLatestRound = await _dbService.GetLatestRoundAsync();
                    for (int round = currentLatestRound - processedCount + 1; round <= currentLatestRound; round++)
                    {
                        await _dbService.CalculateAndSaveRoundWinnerRateAsync(round);
                    }
                }
                else
                {
                    var updateMsg = "📊 새로운 데이터가 없지만 확률을 업데이트합니다...";
                    Console.WriteLine(updateMsg);
                    MainForm.LogToHistory(programName, updateMsg);
                    await _dbService.CalculateAndUpdateProbabilitiesAsync();
                }

                // 3. 추천번호 계산 및 저장
                var recommendStartMsg = "🎯 추천번호 계산 및 저장을 시작합니다...";
                Console.WriteLine(recommendStartMsg);
                MainForm.LogToHistory(programName, recommendStartMsg);
                
                // 4. 최신 회차를 기준으로 다음 회차의 추천번호 계산
                var latestRoundForRecommend = await _dbService.GetLatestRoundAsync();
                var nextRecommendRound = latestRoundForRecommend + 1;
                
                var recommendCalcMsg = $"📊 {nextRecommendRound}회차 추천번호 계산 중...";
                Console.WriteLine(recommendCalcMsg);
                MainForm.LogToHistory(programName, recommendCalcMsg);
                await _dbService.CalculateAndSaveRecommendNumbersAsync(nextRecommendRound);
                
                var recommendCompleteMsg = "🎯 추천번호 계산 및 저장 완료!";
                Console.WriteLine(recommendCompleteMsg);
                MainForm.LogToHistory(programName, recommendCompleteMsg);

                // 5. 번호별 등장 간격 계산 및 저장
                var frequencyStartMsg = "📊 번호별 등장 간격 계산 및 저장을 시작합니다...";
                Console.WriteLine(frequencyStartMsg);
                MainForm.LogToHistory(programName, frequencyStartMsg);
                
                await _dbService.CalculateAndSaveNumberFrequencyAsync();
                
                var frequencyCompleteMsg = "📊 번호별 등장 간격 계산 및 저장 완료!";
                Console.WriteLine(frequencyCompleteMsg);
                MainForm.LogToHistory(programName, frequencyCompleteMsg);

                // 6. 가중치 적용 확률 계산 및 저장
                var weightedStartMsg = "🎯 가중치 적용 확률 계산 및 저장을 시작합니다...";
                Console.WriteLine(weightedStartMsg);
                MainForm.LogToHistory(programName, weightedStartMsg);
                
                await _dbService.CalculateAndSaveWeightedProbabilitiesAsync();
                
                var weightedCompleteMsg = "🎯 가중치 적용 확률 계산 및 저장 완료!";
                Console.WriteLine(weightedCompleteMsg);
                MainForm.LogToHistory(programName, weightedCompleteMsg);

            }
            catch (Exception ex)
            {
                var exceptionMsg = $"로또 당첨 이력 조회 오류: {ex.Message}";
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {exceptionMsg}");
                MainForm.LogToHistory(programName, $"❌ {exceptionMsg}");
                throw; // 예외를 다시 던져서 상위에서 처리하도록 함
            }
        }
    }
}
