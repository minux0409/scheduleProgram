using MySqlConnector;
using System;
using System.Threading.Tasks;

namespace scheduleProgram.Services
{
    /// <summary>
    /// MySQL 데이터베이스 연결 및 실행 이력 저장 서비스
    /// </summary>
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            // MySQL 연결 문자열
            _connectionString = "Server=223.130.128.14;Port=3306;Database=history;Uid=root;Pwd=minwook0409;CharSet=utf8mb4;";
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
                Console.WriteLine($"DB 연결 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 실행 이력을 데이터베이스에 저장
        /// </summary>
        /// <param name="programName">프로그램명</param>
        /// <param name="executeFile">실행파일명</param>
        /// <param name="endFlag">실행결과 (성공: 1, 실패: 0)</param>
        /// <param name="runTime">실행날짜 (yyyy-MM-dd 형식)</param>
        /// <param name="errorMsg">에러 메시지 (실패 시)</param>
        /// <param name="status">실행 상태 (start/end)</param>
        public async Task SaveExecutionHistoryAsync(string programName, string executeFile, int endFlag, string runTime, string? errorMsg = null, string status = "end")
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    INSERT INTO interface_history (program, executeFile, endFlag, executeTime, runDate, errorMsg, status) 
                    VALUES (@program, @executeFile, @endFlag, NOW(), @runDate, @errorMsg, @status)";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@program", programName);
                command.Parameters.AddWithValue("@executeFile", executeFile);
                command.Parameters.AddWithValue("@endFlag", endFlag);
                command.Parameters.AddWithValue("@runDate", runTime);
                command.Parameters.AddWithValue("@errorMsg", (object?)errorMsg ?? DBNull.Value);
                command.Parameters.AddWithValue("@status", status);

                await command.ExecuteNonQueryAsync();
                Console.WriteLine($"DB 저장 성공: {programName}.{executeFile} - {(endFlag == 1 ? "성공" : "실패")} (status: {status})" + 
                    (endFlag == 0 && !string.IsNullOrEmpty(errorMsg) ? $" (오류: {errorMsg})" : ""));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 저장 실패: {ex.Message}");
                // DB 저장 실패해도 프로그램은 계속 동작하도록 예외를 던지지 않음
            }
        }

        /// <summary>
        /// 실행 이력을 데이터베이스에 저장 (동기 버전)
        /// </summary>
        public void SaveExecutionHistory(string programName, string executeFile, int endFlag, string runTime, string? errorMsg = null, string status = "end")
        {
            Task.Run(async () => 
            {
                try
                {
                    Console.WriteLine($"DB 저장 시작: {programName}.{executeFile} - {(endFlag == 1 ? "성공" : "실패")} (status: {status})" +
                        (endFlag == 0 && !string.IsNullOrEmpty(errorMsg) ? $" (오류: {errorMsg})" : ""));
                    await SaveExecutionHistoryAsync(programName, executeFile, endFlag, runTime, errorMsg, status);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DB 저장 Task 실패: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 특정 날짜의 실행 이력 조회
        /// </summary>
        public async Task<List<DatabaseExecutionHistory>> GetExecutionHistoryAsync(string runTime)
        {
            var histories = new List<DatabaseExecutionHistory>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT program, executeFile, endFlag, executeTime, runTime, errorMsg, status 
                    FROM interface_history 
                    WHERE runTime = @runTime 
                    ORDER BY executeTime DESC";

                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@runTime", runTime);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string? errorMsg = null;
                    string status = "end";
                    
                    // errorMsg 컬럼이 있는지 확인하고 값 가져오기
                    try
                    {
                        var errorMsgOrdinal = reader.GetOrdinal("errorMsg");
                        errorMsg = reader.IsDBNull(errorMsgOrdinal) ? null : reader.GetString(errorMsgOrdinal);
                    }
                    catch
                    {
                        // errorMsg 컬럼이 없는 경우 무시
                        errorMsg = null;
                    }
                    
                    // status 컬럼이 있는지 확인하고 값 가져오기
                    try
                    {
                        var statusOrdinal = reader.GetOrdinal("status");
                        status = reader.IsDBNull(statusOrdinal) ? "end" : reader.GetString(statusOrdinal);
                    }
                    catch
                    {
                        // status 컬럼이 없는 경우 기본값 사용
                        status = "end";
                    }
                    
                    histories.Add(new DatabaseExecutionHistory
                    {
                        Program = reader.GetString("program"),
                        ExecuteFile = reader.GetString("executeFile"),
                        EndFlag = reader.GetInt32("endFlag"),
                        ExecuteTime = reader.GetDateTime("executeTime"),
                        RunTime = reader.GetString("runTime"),
                        ErrorMsg = errorMsg,
                        Status = status
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 조회 실패: {ex.Message}");
            }

            return histories;
        }
    }

    /// <summary>
    /// 데이터베이스 실행 이력 모델
    /// </summary>
    public class DatabaseExecutionHistory
    {
        public string Program { get; set; } = string.Empty;
        public string ExecuteFile { get; set; } = string.Empty;
        public int EndFlag { get; set; }
        public DateTime ExecuteTime { get; set; }
        public string RunTime { get; set; } = string.Empty;
        public string? ErrorMsg { get; set; }
        public string Status { get; set; } = "end"; // 실행 상태 (start/end)
        
        public bool IsSuccess => EndFlag == 1;
        public string StatusText => IsSuccess ? "성공" : "실패";
        public string DisplayText => 
            $"[{ExecuteTime:HH:mm:ss}] {Program}.{ExecuteFile} - {StatusText} ({Status})" +
            (!IsSuccess && !string.IsNullOrEmpty(ErrorMsg) ? $" ({ErrorMsg})" : "");
    }
}