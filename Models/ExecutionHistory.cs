using System;

namespace scheduleProgram
{
    /// <summary>
    /// 프로그램 실행 이력 정보
    /// </summary>
    public class ExecutionHistory
    {
        public string ProgramName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public DateTime ExecutionTime { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string Status { get; set; } = "end"; // "start" 또는 "end"

        public string StatusText => Status == "start" ? "시작" : (Success ? "종료" : $"실패: {ErrorMessage}");
        
        public string DisplayText => $"[{ExecutionTime:HH:mm:ss}] {ProgramName}.{DisplayName} - {StatusText}";
    }
}