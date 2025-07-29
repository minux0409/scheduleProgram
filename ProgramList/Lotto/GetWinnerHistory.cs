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
        private string memo = "로또 당첨 이력 조회 - 2시간마다 실행";
        private readonly LottoApiService _apiService;
        private readonly LottoDatabaseService _dbService;

        public GetWinnerHistory()
        {
            _apiService = new LottoApiService();
            _dbService = new LottoDatabaseService();
        }

        public async void Execute()
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 로또 당첨 이력 조회 시작");

            try
            {
                // DB 연결 테스트
                var dbConnected = await _dbService.TestConnectionAsync();
                if (!dbConnected)
                {
                    Console.WriteLine("로또 DB 연결 실패 - 작업을 중단합니다.");
                    return;
                }

                // 가장 최신 회차 조회
                var latestRound = await _dbService.GetLatestRoundAsync();
                var nextRound = latestRound + 1;

                Console.WriteLine($"DB 최신 회차: {latestRound}, 다음 조회할 회차: {nextRound}");

                var processedCount = 0;
                var currentRound = nextRound;

                // 1. API에서 데이터가 없을 때까지 반복
                while (true)
                {
                    Console.WriteLine($"로또 {currentRound}회차 조회 중...");

                    // 이미 저장된 회차인지 확인
                    var exists = await _dbService.IsRoundExistsAsync(currentRound);
                    if (exists)
                    {
                        Console.WriteLine($"{currentRound}회차는 이미 저장되어 있음. 다음 회차로 이동.");
                        currentRound++;
                        continue;
                    }

                    // API 호출
                    var lottoResult = await _apiService.GetLottoNumberAsync(currentRound);
                    
                    if (lottoResult == null)
                    {
                        Console.WriteLine($"❌ {currentRound}회차 API 호출 오류 - 작업을 중단합니다.");
                        break; // API 오류 시 중단
                    }
                    
                    if (lottoResult.IsDataNotFound)
                    {
                        Console.WriteLine($"✅ {currentRound}회차 데이터가 아직 없음 - 조회 완료.");
                        break; // 데이터가 없으면 정상 종료
                    }

                    // DB에 저장
                    var saved = await _dbService.SaveWinnerNumbersAsync(currentRound, lottoResult.Numbers, lottoResult.DrawDate);
                    if (saved)
                    {
                        processedCount++;
                        Console.WriteLine($"✅ {currentRound}회차 저장 완료 - 추첨일: {lottoResult.DrawDate:yyyy-MM-dd}");
                        
                        // 당첨번호 출력 (일반 6개 + 보너스 1개)
                        var normalNumbers = lottoResult.Numbers.Where(n => !n.IsBonusNumber).Select(n => n.Number).ToArray();
                        var bonusNumber = lottoResult.Numbers.FirstOrDefault(n => n.IsBonusNumber)?.Number ?? 0;
                        Console.WriteLine($"   당첨번호: {string.Join(", ", normalNumbers)} + 보너스: {bonusNumber}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ {currentRound}회차 저장 실패");
                    }

                    currentRound++;

                    // API 호출 간격 (과도한 요청 방지)
                    await Task.Delay(1000);

                    // 안전장치: 한 번에 너무 많은 회차를 처리하지 않도록 제한
                    //if (processedCount >= 1000)
                    //{
                    //    Console.WriteLine($"한 번에 최대 1000회차까지만 처리. 다음 실행에서 계속 처리됩니다.");
                    //    break;
                    //}
                }

                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 로또 당첨 이력 조회 완료 - 처리된 회차: {processedCount}개");

                //2.  while 루프 완료 후 확률 계산 및 업데이트
                if (processedCount > 0)
                {
                    Console.WriteLine("📊 새로운 당첨 번호가 추가되어 확률을 재계산합니다...");
                    await _dbService.CalculateAndUpdateProbabilitiesAsync();
                    
                    // 새로 추가된 회차들의 당첨번호 확률 계산
                    Console.WriteLine("📊 새로 추가된 회차들의 당첨번호 확률을 계산합니다...");
                    var currentLatestRound = await _dbService.GetLatestRoundAsync();
                    for (int round = currentLatestRound - processedCount + 1; round <= currentLatestRound; round++)
                    {
                        await _dbService.CalculateAndSaveRoundWinnerRateAsync(round);
                    }
                }
                else
                {
                    Console.WriteLine("📊 새로운 데이터가 없지만 확률을 업데이트합니다...");
                    await _dbService.CalculateAndUpdateProbabilitiesAsync();
                }

                // 3. 추천번호 계산 및 저장
                Console.WriteLine("🎯 추천번호 계산 및 저장을 시작합니다...");
                
                // 최신 회차를 기준으로 다음 회차의 추천번호 계산
                var latestRoundForRecommend = await _dbService.GetLatestRoundAsync();
                var nextRecommendRound = latestRoundForRecommend + 1;
                
                Console.WriteLine($"📊 {nextRecommendRound}회차 추천번호 계산 중...");
                await _dbService.CalculateAndSaveRecommendNumbersAsync(nextRecommendRound);
                
                Console.WriteLine("🎯 추천번호 계산 및 저장 완료!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 로또 당첨 이력 조회 오류: {ex.Message}");
                throw; // 예외를 다시 던져서 상위에서 처리하도록 함
            }
        }
    }
}
