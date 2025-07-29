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
        public string Status { get; set; } = "end"; // "start", "end", "log"

        public string StatusText => Status switch
        {
            "start" => "시작",
            "log" => ErrorMessage ?? "로그",
            _ => Success ? "성공" : $"실패: {ErrorMessage}"
        };
        
        public string DisplayText => Status == "log" 
            ? $"[{ExecutionTime:HH:mm:ss}] {ProgramName} - {ErrorMessage}"
            : $"[{ExecutionTime:HH:mm:ss}] {ProgramName}.{DisplayName} - {StatusText}";
    }
}