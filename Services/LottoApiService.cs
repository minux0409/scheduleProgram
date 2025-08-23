using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using scheduleProgram.Services;

namespace scheduleProgram.Services
{
    /// <summary>
    /// 로또 API 호출 서비스
    /// </summary>
    public class LottoApiService
    {
        private readonly HttpClient _httpClient;
        private const string API_BASE_URL = "https://www.dhlottery.co.kr/common.do?method=getLottoNumber&drwNo=";

        public LottoApiService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            // User-Agent 설정 (일부 서버에서 요구할 수 있음)
            _httpClient.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        }

        /// <summary>
        /// 특정 회차의 로또 당첨번호 조회
        /// </summary>
        public async Task<LottoResult?> GetLottoNumberAsync(int round)
        {
            try
            {
                Console.WriteLine($"로또 API 호출 시작 - {round}회차");
                
                var url = $"{API_BASE_URL}{round}";
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"? HTTP 호출 실패 - {round}회차: {response.StatusCode}");
                    return null; // HTTP 오류는 null 반환
                }

                // 문자 인코딩 문제 해결: 바이트로 읽어서 UTF-8로 강제 디코딩
                string jsonContent;
                try
                {
                    var responseBytes = await response.Content.ReadAsByteArrayAsync();
                    
                    // UTF-8로 강제 디코딩 시도
                    jsonContent = Encoding.UTF8.GetString(responseBytes);
                    
                    // UTF-8 디코딩이 실패한 경우 EUC-KR 시도
                    if (string.IsNullOrEmpty(jsonContent) || jsonContent.Contains("?"))
                    {
                        // EUC-KR 인코딩 시도 (한국 웹사이트에서 자주 사용)
                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                        var euckrEncoding = Encoding.GetEncoding("EUC-KR");
                        jsonContent = euckrEncoding.GetString(responseBytes);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"? 응답 디코딩 실패 - {round}회차: {ex.Message}");
                    return null;
                }

                Console.WriteLine($"API 응답 받음 - {round}회차: {jsonContent}");

                LottoApiResponse? lottoData;
                try
                {
                    lottoData = JsonSerializer.Deserialize<LottoApiResponse>(jsonContent);
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"? JSON 파싱 실패 - {round}회차: {ex.Message}");
                    Console.WriteLine($"응답 내용: {jsonContent}");
                    return null; // JSON 파싱 오류는 null 반환
                }
                
                if (lottoData == null)
                {
                    Console.WriteLine($"? JSON 데이터가 null - {round}회차");
                    return null;
                }

                // returnValue 확인
                if (lottoData.returnValue == "fail")
                {
                    Console.WriteLine($"?? API 응답: 데이터 없음 - {round}회차 (returnValue: fail)");
                    return new LottoResult { Round = round, IsDataNotFound = true }; // 데이터 없음을 나타내는 특별한 객체
                }
                else if (lottoData.returnValue != "success")
                {
                    Console.WriteLine($"? API 응답 오류 - {round}회차: returnValue = {lottoData.returnValue}");
                    return null; // API 오류는 null 반환
                }

                // 성공 응답 처리
                return new LottoResult
                {
                    Round = lottoData.drwNo,
                    DrawDate = DateTime.ParseExact(lottoData.drwNoDate, "yyyy-MM-dd", null),
                    IsDataNotFound = false,
                    TotalPrice = lottoData.firstAccumamnt,
                    Winner = lottoData.firstPrzwnerCo,
                    WinnerPrice = lottoData.firstWinamnt,
                    Numbers = new List<WinnerNumber>
                    {
                        new WinnerNumber { Seq = 1, Number = lottoData.drwtNo1, IsBonusNumber = false },
                        new WinnerNumber { Seq = 2, Number = lottoData.drwtNo2, IsBonusNumber = false },
                        new WinnerNumber { Seq = 3, Number = lottoData.drwtNo3, IsBonusNumber = false },
                        new WinnerNumber { Seq = 4, Number = lottoData.drwtNo4, IsBonusNumber = false },
                        new WinnerNumber { Seq = 5, Number = lottoData.drwtNo5, IsBonusNumber = false },
                        new WinnerNumber { Seq = 6, Number = lottoData.drwtNo6, IsBonusNumber = false },
                        new WinnerNumber { Seq = 7, Number = lottoData.bnusNo, IsBonusNumber = true } // 보너스 번호 추가
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? API 호출 예외 - {round}회차: {ex.Message}");
                return null; // 예외는 null 반환
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// 로또 API 응답 모델
    /// </summary>
    public class LottoApiResponse
    {
        public string returnValue { get; set; } = string.Empty;
        public int drwNo { get; set; }
        public string drwNoDate { get; set; } = string.Empty;
        public int drwtNo1 { get; set; }
        public int drwtNo2 { get; set; }
        public int drwtNo3 { get; set; }
        public int drwtNo4 { get; set; }
        public int drwtNo5 { get; set; }
        public int drwtNo6 { get; set; }
        public int bnusNo { get; set; }
        public long totSellamnt { get; set; }
        public long firstAccumamnt { get; set; }
        public int firstPrzwnerCo { get; set; }
        public long firstWinamnt { get; set; }
    }

    /// <summary>
    /// 로또 결과 모델
    /// </summary>
    public class LottoResult
    {
        public int Round { get; set; }
        public DateTime DrawDate { get; set; }
        public bool IsDataNotFound { get; set; } = false; // API에서 데이터가 없음을 나타냄 (returnValue: "fail")
        public long TotalPrice { get; set; } // 총 상금 (firstAccumamnt)
        public int Winner { get; set; } // 1등 당첨자 수 (firstPrzwnerCo)
        public long WinnerPrice { get; set; } // 1등 당첨금 (firstWinamnt)
        public List<WinnerNumber> Numbers { get; set; } = new List<WinnerNumber>();
    }
}