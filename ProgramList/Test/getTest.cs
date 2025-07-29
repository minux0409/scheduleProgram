using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace scheduleProgram.ProgramList.Test
{
    // schedule: daily
    // time: 09:00
    // enabled: true
    internal class getTest
    {
        private string memo = "테스트 프로그램 - 매일 오전 9시 실행";

        public void Execute()
        {
            MessageBox.Show($"테스트 프로그램이 실행되었습니다!\n실행 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", 
                "getTest", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
