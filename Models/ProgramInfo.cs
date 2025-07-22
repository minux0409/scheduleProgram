using System;
using System.IO;

namespace scheduleProgram
{
    /// <summary>
    /// 실행 가능한 프로그램 정보를 담는 클래스
    /// </summary>
    public class ProgramInfo
    {
        public string ProgramName { get; set; } = string.Empty;
        public string CsFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Memo { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public bool IsValid { get; set; } = true;

        public string DisplayName => Path.GetFileNameWithoutExtension(CsFileName);
        
        public override string ToString()
        {
            return $"{ProgramName} - {DisplayName}";
        }
    }

    /// <summary>
    /// 프로그램 실행 인터페이스
    /// </summary>
    public interface IExecutableProgram
    {
        string Memo { get; }
        void Execute();
    }
}