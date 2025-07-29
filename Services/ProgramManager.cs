using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;
using scheduleProgram.Services;

namespace scheduleProgram
{
    /// <summary>
    /// 프로그램 폴더를 관리하고 CS 파일을 실행하는 서비스
    /// </summary>
    public class ProgramManager
    {
        private readonly string _baseProgramsPath;
        private readonly string _scheduleConfigPath;
        private readonly DatabaseService _databaseService;
        private List<ProgramInfo> _programs;

        public ProgramManager()
        {
            // 프로젝트 소스 루트의 ProgramList 폴더를 찾기 위한 경로 설정
            _baseProgramsPath = GetProjectSourceProgramListPath();
            _scheduleConfigPath = Path.Combine(Path.GetDirectoryName(_baseProgramsPath), "schedule_config.json");
            _databaseService = new DatabaseService();
            _programs = new List<ProgramInfo>();
            EnsureDirectoryExists();
        }

        /// <summary>
        /// 프로젝트 소스 디렉토리의 ProgramList 경로를 찾습니다
        /// </summary>
        private string GetProjectSourceProgramListPath()
        {
            // 현재 어셈블리의 위치에서 시작하여 프로젝트 루트를 찾습니다
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var currentDir = new DirectoryInfo(Path.GetDirectoryName(assemblyLocation));

            // bin 폴더에서 프로젝트 루트까지 올라갑니다
            while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, "scheduleProgram.csproj")))
            {
                currentDir = currentDir.Parent;
            }

            if (currentDir == null)
            {
                // 프로젝트 파일을 찾지 못한 경우 현재 디렉토리 사용
                return Path.Combine(Directory.GetCurrentDirectory(), "ProgramList");
            }

            return Path.Combine(currentDir.FullName, "ProgramList");
        }

        /// <summary>
        /// ProgramList 디렉토리가 존재하는지 확인하고 없으면 생성
        /// </summary>
        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_baseProgramsPath))
            {
                Directory.CreateDirectory(_baseProgramsPath);
            }
        }

        /// <summary>
        /// ProgramList 폴더의 모든 프로그램 폴더에서 CS 파일들을 스캔
        /// </summary>
        public List<ProgramInfo> ScanAllPrograms()
        {
            _programs.Clear();

            if (!Directory.Exists(_baseProgramsPath))
            {
                // 디버깅을 위해 실제 경로 표시
                MessageBox.Show($"ProgramList 폴더를 찾을 수 없습니다.\n경로: {_baseProgramsPath}", 
                    "디버그 정보", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return _programs;
            }

            var programFolders = Directory.GetDirectories(_baseProgramsPath);

            foreach (var folder in programFolders)
            {
                var programName = Path.GetFileName(folder);
                var csFiles = Directory.GetFiles(folder, "*.cs");

                foreach (var csFile in csFiles)
                {
                    var programInfo = CreateProgramInfo(programName, csFile);
                    if (programInfo != null)
                    {
                        _programs.Add(programInfo);
                    }
                }
            }

            // 스케줄 설정 로드
            LoadScheduleConfig();

            return _programs.OrderBy(p => p.ProgramName).ThenBy(p => p.CsFileName).ToList();
        }

        /// <summary>
        /// CS 파일에서 프로그램 정보 생성
        /// </summary>
        private ProgramInfo? CreateProgramInfo(string programName, string filePath)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                var memo = ExtractMemoFromContent(content);
                var scheduleInfo = ExtractScheduleFromContent(content);

                var programInfo = new ProgramInfo
                {
                    ProgramName = programName,
                    CsFileName = Path.GetFileName(filePath),
                    FilePath = filePath,
                    Memo = memo,
                    LastModified = File.GetLastWriteTime(filePath),
                    IsValid = true
                };

                // CS 파일에서 스케줄 정보 추출
                ApplyScheduleInfo(programInfo, scheduleInfo);

                return programInfo;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"파일 읽기 오류: {filePath}\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
        }

        /// <summary>
        /// CS 파일 내용에서 memo 변수 값 추출
        /// </summary>
        private string ExtractMemoFromContent(string content)
        {
            try
            {
                // memo 변수를 찾는 정규식 패턴
                var patterns = new[]
                {
                    @"string\s+memo\s*=\s*""([^""]*)"";",
                    @"private\s+string\s+memo\s*=\s*""([^""]*)"";",
                    @"public\s+string\s+memo\s*=\s*""([^""]*)"";",
                    @"readonly\s+string\s+memo\s*=\s*""([^""]*)"";",
                    @"string\s+Memo\s*=\s*""([^""]*)"";",
                    @"public\s+string\s+Memo\s*{\s*get;\s*}\s*=\s*""([^""]*)"";",
                };

                foreach (var pattern in patterns)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(content, pattern);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        return match.Groups[1].Value;
                    }
                }

                return "메모 없음";
            }
            catch
            {
                return "메모 읽기 실패";
            }
        }

        /// <summary>
        /// CS 파일 내용에서 스케줄 정보 추출
        /// </summary>
        private Dictionary<string, string> ExtractScheduleFromContent(string content)
        {
            var scheduleInfo = new Dictionary<string, string>();

            try
            {
                // 스케줄 관련 주석을 찾는 패턴들
                var patterns = new Dictionary<string, string>
                {
                    { "schedule", @"//\s*schedule\s*:\s*(.+)" },
                    { "interval", @"//\s*interval\s*:\s*(\d+)" },
                    { "time", @"//\s*time\s*:\s*([\d:]+)" },
                    { "enabled", @"//\s*enabled\s*:\s*(true|false)" }
                };

                foreach (var kvp in patterns)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(content, kvp.Value, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (match.Success && match.Groups.Count > 1)
                    {
                        scheduleInfo[kvp.Key] = match.Groups[1].Value.Trim();
                    }
                }
            }
            catch
            {
                // 스케줄 정보 추출 실패 시 무시
            }

            return scheduleInfo;
        }

        /// <summary>
        /// 추출된 스케줄 정보를 ProgramInfo에 적용
        /// </summary>
        private void ApplyScheduleInfo(ProgramInfo programInfo, Dictionary<string, string> scheduleInfo)
        {
            if (scheduleInfo.TryGetValue("enabled", out var enabledStr) && bool.TryParse(enabledStr, out var enabled))
            {
                programInfo.IsScheduleEnabled = enabled;
            }

            if (scheduleInfo.TryGetValue("schedule", out var scheduleType))
            {
                programInfo.ScheduleType = scheduleType.ToLower() switch
                {
                    "once" => ScheduleType.Once,
                    "daily" => ScheduleType.Daily,
                    "weekly" => ScheduleType.Weekly,
                    "monthly" => ScheduleType.Monthly,
                    "interval" => ScheduleType.Interval,
                    _ => ScheduleType.None
                };
            }

            if (scheduleInfo.TryGetValue("time", out var timeStr) && TimeSpan.TryParse(timeStr, out var time))
            {
                programInfo.ScheduledTime = DateTime.Today.Add(time);
            }

            if (scheduleInfo.TryGetValue("interval", out var intervalStr) && int.TryParse(intervalStr, out var interval))
            {
                programInfo.IntervalMinutes = interval;
            }

            // 다음 실행 시간 계산
            programInfo.CalculateNextExecution();
        }

        /// <summary>
        /// 스케줄 설정을 JSON 파일에서 로드
        /// </summary>
        private void LoadScheduleConfig()
        {
            try
            {
                if (File.Exists(_scheduleConfigPath))
                {
                    var json = File.ReadAllText(_scheduleConfigPath);
                    var config = JsonSerializer.Deserialize<Dictionary<string, ScheduleConfig>>(json);
                    
                    if (config != null)
                    {
                        foreach (var program in _programs)
                        {
                            var key = $"{program.ProgramName}_{program.CsFileName}";
                            if (config.TryGetValue(key, out var scheduleConfig))
                            {
                                program.IsScheduleEnabled = scheduleConfig.IsEnabled;
                                program.ScheduleType = scheduleConfig.ScheduleType;
                                program.ScheduledTime = scheduleConfig.ScheduledTime;
                                program.IntervalMinutes = scheduleConfig.IntervalMinutes;
                                program.LastExecuted = scheduleConfig.LastExecuted;
                                program.SelectedDayOfWeek = scheduleConfig.SelectedDayOfWeek;
                                program.SelectedDayOfMonth = scheduleConfig.SelectedDayOfMonth;
                                program.IsLastDayOfMonth = scheduleConfig.IsLastDayOfMonth;
                                program.CalculateNextExecution();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"스케줄 설정 로드 오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// 스케줄 설정을 JSON 파일에 저장
        /// </summary>
        public void SaveScheduleConfig()
        {
            try
            {
                var config = new Dictionary<string, ScheduleConfig>();
                
                foreach (var program in _programs)
                {
                    var key = $"{program.ProgramName}_{program.CsFileName}";
                    config[key] = new ScheduleConfig
                    {
                        IsEnabled = program.IsScheduleEnabled,
                        ScheduleType = program.ScheduleType,
                        ScheduledTime = program.ScheduledTime,
                        IntervalMinutes = program.IntervalMinutes,
                        LastExecuted = program.LastExecuted,
                        SelectedDayOfWeek = program.SelectedDayOfWeek,
                        SelectedDayOfMonth = program.SelectedDayOfMonth,
                        IsLastDayOfMonth = program.IsLastDayOfMonth
                    };
                }

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_scheduleConfigPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"스케줄 설정 저장 오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// 실행 예정인 프로그램들을 반환
        /// </summary>
        public List<ProgramInfo> GetProgramsToExecute()
        {
            return _programs.Where(p => p.ShouldExecuteNow()).ToList();
        }

        /// <summary>
        /// 프로그램 실행 (수동 실행 - 메시지박스 표시)
        /// </summary>
        public void ExecuteProgram(ProgramInfo programInfo)
        {
            ExecuteProgramInternal(programInfo, isScheduled: false);
        }

        /// <summary>
        /// 프로그램 실행 (스케줄 실행 - 조용한 실행)
        /// </summary>
        public void ExecuteProgramSilently(ProgramInfo programInfo)
        {
            ExecuteProgramInternal(programInfo, isScheduled: true);
        }

        /// <summary>
        /// 프로그램 실행 내부 로직
        /// </summary>
        private void ExecuteProgramInternal(ProgramInfo programInfo, bool isScheduled)
        {
            var startTime = DateTime.Now;
            var success = false;
            string? errorMessage = null;

            Console.WriteLine($"ProgramManager.ExecuteProgramInternal 시작: {programInfo.ProgramName}.{programInfo.DisplayName}, 스케줄: {isScheduled}");

            // 실행 시작 로그 DB 저장
            SaveExecutionStartToDatabase(programInfo);

            try
            {
                var content = File.ReadAllText(programInfo.FilePath);
                
                // CS 스크립트 실행
                ExecuteCsScript(content, programInfo, isScheduled);
                
                success = true;
                Console.WriteLine($"ProgramManager.ExecuteProgramInternal 성공: {programInfo.ProgramName}.{programInfo.DisplayName}");

                // 실행 시간 업데이트
                programInfo.LastExecuted = DateTime.Now;
                programInfo.CalculateNextExecution();
                SaveScheduleConfig();
            }
            catch (Exception ex)
            {
                success = false;
                errorMessage = ex.Message;
                Console.WriteLine($"ProgramManager.ExecuteProgramInternal 실패: {programInfo.ProgramName}.{programInfo.DisplayName}, 오류: {ex.Message}");

                // 수동 실행인 경우에만 에러 메시지박스 표시
                if (!isScheduled)
                {
                    MessageBox.Show($"프로그램 실행 오류: {programInfo.CsFileName}\n{ex.Message}", 
                        "실행 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                // 실행 종료 로그 DB 저장
                Console.WriteLine($"ProgramManager.ExecuteProgramInternal 종료 DB 저장 호출: {programInfo.ProgramName}.{programInfo.DisplayName}");
                SaveExecutionEndToDatabase(programInfo, success, errorMessage);
            }
        }

        /// <summary>
        /// 실행 시작 이력을 데이터베이스에 저장
        /// </summary>
        private void SaveExecutionStartToDatabase(ProgramInfo programInfo)
        {
            try
            {
                var programName = programInfo.ProgramName;
                var executeFile = Path.GetFileNameWithoutExtension(programInfo.CsFileName); // 확장자 제거
                var runTime = DateTime.Today.ToString("yyyy-MM-dd");

                Console.WriteLine($"DB 시작 로그 저장: {programName}.{executeFile}, 날짜: {runTime}");

                // DB에 시작 로그 저장 (endFlag=1, status="start")
                _databaseService.SaveExecutionHistory(programName, executeFile, 1, runTime, null, "start");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 시작 로그 저장 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 실행 종료 이력을 데이터베이스에 저장
        /// </summary>
        private void SaveExecutionEndToDatabase(ProgramInfo programInfo, bool success, string? errorMessage)
        {
            try
            {
                var programName = programInfo.ProgramName;
                var executeFile = Path.GetFileNameWithoutExtension(programInfo.CsFileName); // 확장자 제거
                var endFlag = success ? 1 : 0;
                var runTime = DateTime.Today.ToString("yyyy-MM-dd");

                Console.WriteLine($"DB 종료 로그 저장: {programName}.{executeFile}, 결과: {(success ? "성공" : "실패")}, 날짜: {runTime}" +
                    (!success && !string.IsNullOrEmpty(errorMessage) ? $", 오류: {errorMessage}" : ""));

                // DB에 종료 로그 저장 (endFlag=성공여부, status="end")
                _databaseService.SaveExecutionHistory(programName, executeFile, endFlag, runTime, errorMessage, "end");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 종료 로그 저장 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// CS 스크립트 컴파일 및 실행
        /// </summary>
        private void ExecuteCsScript(string code, ProgramInfo programInfo, bool isScheduled)
        {
            try
            {
                // 실제 CS 클래스를 인스턴스화하고 Execute 메서드 호출
                ExecuteActualCsClass(programInfo, isScheduled);
                
                // 스케줄 실행인 경우 콘솔에만 로그 출력
                if (isScheduled)
                {
                    Console.WriteLine($"[스케줄 실행] {programInfo.ProgramName}.{programInfo.DisplayName} - {DateTime.Now:HH:mm:ss}");
                }
                else
                {
                    Console.WriteLine($"[수동 실행] {programInfo.ProgramName}.{programInfo.DisplayName} - {DateTime.Now:HH:mm:ss}");
                }
            }
            catch (Exception ex)
            {
                // 수동 실행인 경우에만 에러 메시지박스 표시
                if (!isScheduled)
                {
                    MessageBox.Show($"실행 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                throw; // 상위에서 에러 처리하도록 예외 재던지기
            }
        }

        /// <summary>
        /// 실제 CS 클래스를 인스턴스화하고 Execute 메서드를 호출
        /// </summary>
        private void ExecuteActualCsClass(ProgramInfo programInfo, bool isScheduled)
        {
            try
            {
                // 파일명에서 확장자 제거하여 클래스명 생성
                var className = Path.GetFileNameWithoutExtension(programInfo.CsFileName);
                var fullClassName = $"scheduleProgram.ProgramList.{programInfo.ProgramName}.{className}";

                Console.WriteLine($"클래스 실행 시도: {fullClassName}");

                // 현재 어셈블리에서 타입 찾기
                var currentAssembly = Assembly.GetExecutingAssembly();
                var classType = currentAssembly.GetType(fullClassName);

                if (classType == null)
                {
                    throw new Exception($"클래스를 찾을 수 없습니다: {fullClassName}");
                }

                // 클래스 인스턴스 생성
                var instance = Activator.CreateInstance(classType);
                if (instance == null)
                {
                    throw new Exception($"클래스 인스턴스 생성 실패: {fullClassName}");
                }

                // Execute 메서드 찾기
                var executeMethod = classType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Instance);
                if (executeMethod == null)
                {
                    throw new Exception($"Execute 메서드를 찾을 수 없습니다: {fullClassName}");
                }

                Console.WriteLine($"Execute 메서드 호출: {fullClassName}.Execute()");

                // Execute 메서드 호출
                executeMethod.Invoke(instance, null);

                Console.WriteLine($"? 실행 완료: {fullClassName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? 실행 실패: {programInfo.ProgramName}.{programInfo.CsFileName} - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 데이터베이스 연결 테스트
        /// </summary>
        public async Task<bool> TestDatabaseConnectionAsync()
        {
            return await _databaseService.TestConnectionAsync();
        }
    }

    /// <summary>
    /// 스케줄 설정 저장용 클래스
    /// </summary>
    public class ScheduleConfig
    {
        public bool IsEnabled { get; set; }
        public ScheduleType ScheduleType { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public int IntervalMinutes { get; set; }
        public DateTime? LastExecuted { get; set; }
        public DayOfWeek SelectedDayOfWeek { get; set; } = DayOfWeek.Monday;
        public int SelectedDayOfMonth { get; set; } = 1;
        public bool IsLastDayOfMonth { get; set; } = false;
    }
}