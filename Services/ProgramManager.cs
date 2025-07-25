using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace scheduleProgram
{
    /// <summary>
    /// 프로그램 폴더를 관리하고 CS 파일을 실행하는 서비스
    /// </summary>
    public class ProgramManager
    {
        private readonly string _baseProgramsPath;
        private List<ProgramInfo> _programs;

        public ProgramManager()
        {
            _baseProgramsPath = Path.Combine(Application.StartupPath, "Programs");
            _programs = new List<ProgramInfo>();
            EnsureDirectoryExists();
        }

        /// <summary>
        /// Programs 디렉토리가 존재하는지 확인하고 없으면 생성
        /// </summary>
        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_baseProgramsPath))
            {
                Directory.CreateDirectory(_baseProgramsPath);
            }
        }

        /// <summary>
        /// 모든 프로그램 폴더에서 CS 파일들을 스캔
        /// </summary>
        public List<ProgramInfo> ScanAllPrograms()
        {
            _programs.Clear();

            if (!Directory.Exists(_baseProgramsPath))
                return _programs;

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

                return new ProgramInfo
                {
                    ProgramName = programName,
                    CsFileName = Path.GetFileName(filePath),
                    FilePath = filePath,
                    Memo = memo,
                    LastModified = File.GetLastWriteTime(filePath),
                    IsValid = true
                };
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
        /// 프로그램 실행
        /// </summary>
        public void ExecuteProgram(ProgramInfo programInfo)
        {
            try
            {
                var content = File.ReadAllText(programInfo.FilePath);
                
                // 간단한 스크립트 실행을 위한 컴파일 및 실행
                ExecuteCsScript(content, programInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"프로그램 실행 오류: {programInfo.CsFileName}\n{ex.Message}", 
                    "실행 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// CS 스크립트 컴파일 및 실행
        /// </summary>
        private void ExecuteCsScript(string code, ProgramInfo programInfo)
        {
            try
            {
                // 메시지박스로 간단히 실행 결과 표시 (실제로는 각 스크립트의 Execute 메서드 호출)
                MessageBox.Show($"[{programInfo.ProgramName}] {programInfo.DisplayName} 실행됨\n\n메모: {programInfo.Memo}", 
                    "프로그램 실행", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // TODO: 실제 컴파일 및 실행 로직 구현
                // 현재는 데모용으로 메시지만 표시
            }
            catch (Exception ex)
            {
                MessageBox.Show($"실행 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 새 프로그램 폴더 생성
        /// </summary>
        public bool CreateProgramFolder(string programName)
        {
            try
            {
                var folderPath = Path.Combine(_baseProgramsPath, programName);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}