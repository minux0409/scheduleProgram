using System;
using System.Windows.Forms;

namespace scheduleProgram
{
    internal static class Program
    {
        /// <summary>
        /// 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // .NET Core/.NET 5+ 스타일 초기화
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
