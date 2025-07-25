using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace scheduleProgram
{
    /// <summary>
    /// 프로그램 관리 메인 폼
    /// </summary>
    public partial class MainForm : Form
    {
        #region 필드 및 속성
        private ProgramManager programManager;
        private DataGridView programsGrid;
        private Button refreshButton;
        private Button addFolderButton;
        private Button openDesignerButton;
        private Panel topPanel;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private List<ProgramInfo> currentPrograms;
        #endregion

        #region 생성자
        public MainForm()
        {
            InitializeComponent();
            programManager = new ProgramManager();
            currentPrograms = new List<ProgramInfo>();
            LoadInitialData();
        }
        #endregion

        #region 초기화
        /// <summary>
        /// 초기 데이터를 로드합니다
        /// </summary>
        private void LoadInitialData()
        {
            UpdateStatus("프로그램 관리자가 시작되었습니다.");
            RefreshProgramList();
        }
        #endregion

        #region 이벤트 핸들러
        /// <summary>
        /// 새로고침 버튼 클릭 이벤트
        /// </summary>
        private void RefreshButton_Click(object sender, EventArgs e)
        {
            RefreshProgramList();
            UpdateStatus("프로그램 목록이 새로고침되었습니다.");
        }

        /// <summary>
        /// 폴더 추가 버튼 클릭 이벤트
        /// </summary>
        private void AddFolderButton_Click(object sender, EventArgs e)
        {
            ShowAddFolderDialog();
        }

        /// <summary>
        /// 비주얼 디자이너 열기 버튼 클릭 이벤트
        /// </summary>
        private void OpenDesignerButton_Click(object sender, EventArgs e)
        {
            OpenVisualDesigner();
        }

        /// <summary>
        /// 그리드 셀 클릭 이벤트 (실행 버튼)
        /// </summary>
        private void ProgramsGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            // 실행 버튼 컬럼인지 확인
            if (programsGrid.Columns[e.ColumnIndex].Name == "ExecuteColumn")
            {
                var programInfo = GetProgramInfoFromRow(e.RowIndex);
                if (programInfo != null)
                {
                    ExecuteProgram(programInfo);
                }
            }
        }

        /// <summary>
        /// 그리드 셀 포맷팅 이벤트
        /// </summary>
        private void ProgramsGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var programInfo = GetProgramInfoFromRow(e.RowIndex);
            if (programInfo == null) return;

            // 프로그램별 색상 구분
            var backColor = GetProgramColor(programInfo.ProgramName);
            programsGrid.Rows[e.RowIndex].DefaultCellStyle.BackColor = backColor;
        }
        #endregion

        #region 비즈니스 로직
        /// <summary>
        /// 프로그램 목록을 새로고침합니다
        /// </summary>
        private void RefreshProgramList()
        {
            try
            {
                currentPrograms = programManager.ScanAllPrograms();
                UpdateProgramsGrid();
                UpdateStatus($"총 {currentPrograms.Count}개의 프로그램이 발견되었습니다.");
            }
            catch (Exception ex)
            {
                ShowError($"프로그램 목록 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 프로그램을 실행합니다
        /// </summary>
        private void ExecuteProgram(ProgramInfo programInfo)
        {
            try
            {
                programManager.ExecuteProgram(programInfo);
                UpdateStatus($"프로그램 실행: {programInfo.ProgramName} - {programInfo.DisplayName}");
            }
            catch (Exception ex)
            {
                ShowError($"프로그램 실행 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 새 폴더 추가 대화상자를 표시합니다
        /// </summary>
        private void ShowAddFolderDialog()
        {
            var inputDialog = new SimpleInputDialog("새 프로그램 폴더 이름을 입력하세요:", "폴더 추가", "newProgram");

            if (inputDialog.ShowDialog() == DialogResult.OK)
            {
                var folderName = inputDialog.InputText;
                if (!string.IsNullOrWhiteSpace(folderName))
                {
                    if (programManager.CreateProgramFolder(folderName))
                    {
                        RefreshProgramList();
                        UpdateStatus($"새 폴더 '{folderName}'이(가) 생성되었습니다.");
                    }
                    else
                    {
                        ShowError("폴더 생성에 실패했습니다. 이미 존재하거나 잘못된 이름일 수 있습니다.");
                    }
                }
            }
        }

        /// <summary>
        /// 비주얼 디자이너를 엽니다
        /// </summary>
        private void OpenVisualDesigner()
        {
            try
            {
                MessageBox.Show("비주얼 폼 디자이너는 개발 중입니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateStatus("비주얼 폼 디자이너 기능은 개발 중입니다.");
            }
            catch (Exception ex)
            {
                ShowError($"디자이너 열기 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 행 인덱스로부터 프로그램 정보를 가져옵니다
        /// </summary>
        private ProgramInfo? GetProgramInfoFromRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= currentPrograms.Count)
                return null;

            return currentPrograms[rowIndex];
        }

        /// <summary>
        /// 프로그램명에 따른 색상을 반환합니다
        /// </summary>
        private System.Drawing.Color GetProgramColor(string programName)
        {
            var hash = programName.GetHashCode();
            var colors = new[]
            {
                UITheme.LightColor,
                System.Drawing.Color.FromArgb(240, 248, 255), // AliceBlue
                System.Drawing.Color.FromArgb(250, 240, 230), // Linen
                System.Drawing.Color.FromArgb(245, 255, 250), // MintCream
                System.Drawing.Color.FromArgb(255, 250, 240), // FloralWhite
            };

            return colors[Math.Abs(hash) % colors.Length];
        }
        #endregion

        #region UI 업데이트
        /// <summary>
        /// 프로그램 그리드를 업데이트합니다
        /// </summary>
        private void UpdateProgramsGrid()
        {
            programsGrid.Rows.Clear();

            foreach (var program in currentPrograms)
            {
                var rowIndex = programsGrid.Rows.Add(
                    program.ProgramName,
                    program.DisplayName,
                    program.Memo,
                    "실행"
                );

                // 마지막 수정일시를 툴팁으로 추가
                programsGrid.Rows[rowIndex].Cells[0].ToolTipText =
                    $"마지막 수정: {program.LastModified:yyyy-MM-dd HH:mm:ss}";
            }
        }

        /// <summary>
        /// 상태바 메시지를 업데이트합니다
        /// </summary>
        private void UpdateStatus(string message)
        {
            statusLabel.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
        }

        /// <summary>
        /// 오류 메시지를 표시합니다
        /// </summary>
        private void ShowError(string message)
        {
            MessageBox.Show(message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            UpdateStatus($"오류: {message}");
        }
        #endregion
    }

    /// <summary>
    /// 간단한 입력 대화상자
    /// </summary>
    public partial class SimpleInputDialog : Form
    {
        public string InputText { get; private set; } = string.Empty;

        private TextBox textBox;

        public SimpleInputDialog(string prompt, string title, string defaultValue = "")
        {
            InitializeDialog(prompt, title, defaultValue);
        }

        private void InitializeDialog(string prompt, string title, string defaultValue)
        {
            this.Text = title;
            this.Size = new System.Drawing.Size(400, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var label = new Label()
            {
                Text = prompt,
                Location = new System.Drawing.Point(12, 15),
                Size = new System.Drawing.Size(360, 23),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            textBox = new TextBox()
            {
                Text = defaultValue,
                Location = new System.Drawing.Point(12, 45),
                Size = new System.Drawing.Size(360, 23)
            };

            var okButton = new Button()
            {
                Text = "확인",
                DialogResult = DialogResult.OK,
                Location = new System.Drawing.Point(217, 75),
                Size = new System.Drawing.Size(75, 23)
            };

            var cancelButton = new Button()
            {
                Text = "취소",
                DialogResult = DialogResult.Cancel,
                Location = new System.Drawing.Point(297, 75),
                Size = new System.Drawing.Size(75, 23)
            };

            okButton.Click += (s, e) => { InputText = textBox.Text; };

            this.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }
    }
}
