using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using scheduleProgram.Services;

namespace scheduleProgram
{
    /// <summary>
    /// 프로그램 관리 메인 폼
    /// </summary>
    public partial class MainForm : Form
    {
        #region 필드 및 속성
        private ProgramManager programManager;
        private SchedulerService schedulerService;
        private DataGridView programsGrid;
        private Button refreshButton;
        private Button schedulerToggleButton;
        private Panel topPanel;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private List<ProgramInfo> currentPrograms;
        
        // 실행 이력 관련 컨트롤
        private Panel historyPanel;
        private Label historyTitleLabel;
        private ListBox historyListBox;
        private List<ExecutionHistory> executionHistory;
        private DateTime lastHistoryClearDate; // 마지막 이력 초기화 날짜
        private const int MaxHistoryCount = 200; // 최대 이력 개수 증가
        #endregion

        #region 생성자
        public MainForm()
        {
            // 스레드 간 호출 검사 비활성화 (개발 단계에서만 사용)
            Control.CheckForIllegalCrossThreadCalls = false;
            
            InitializeComponent();
            programManager = new ProgramManager();
            schedulerService = new SchedulerService(programManager);
            currentPrograms = new List<ProgramInfo>();
            executionHistory = new List<ExecutionHistory>();
            lastHistoryClearDate = DateTime.Today; // 오늘 날짜로 초기화
            
            // 스케줄러 이벤트 연결
            schedulerService.ProgramExecuted += OnProgramExecuted;
            schedulerService.StatusUpdated += OnSchedulerStatusUpdated;
            
            LoadInitialData();
        }
        #endregion

        #region 초기화
        /// <summary>
        /// 초기 데이터를 로드합니다
        /// </summary>
        private async void LoadInitialData()
        {
            UpdateStatus("프로그램 관리자가 시작되었습니다.");
            
            // DB 연결 테스트
            try
            {
                var dbConnected = await programManager.TestDatabaseConnectionAsync();
                if (dbConnected)
                {
                    UpdateStatus("데이터베이스 연결 성공");
                }
                else
                {
                    UpdateStatus("데이터베이스 연결 실패 - 실행 이력이 DB에 저장되지 않을 수 있습니다");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"데이터베이스 연결 테스트 오류: {ex.Message}");
            }
            
            RefreshProgramList();
            
            // 스케줄러 자동 시작
            schedulerService.Start();
        }
        #endregion

        #region 이벤트 핸들러
        /// <summary>
        /// 새로고침 버튼 클릭 이벤트
        /// </summary>
        private void RefreshButton_Click(object sender, EventArgs e)
        {
            RefreshProgramList();
            schedulerService.RefreshPrograms();
            UpdateStatus("프로그램 목록이 새로고침되었습니다.");
        }

        /// <summary>
        /// 스케줄러 토글 버튼 클릭 이벤트
        /// </summary>
        private void SchedulerToggleButton_Click(object sender, EventArgs e)
        {
            if (schedulerService.IsRunning)
            {
                schedulerService.Stop();
                schedulerToggleButton.Text = "스케줄러 시작";
                schedulerToggleButton.BackColor = UITheme.AccentColor;
            }
            else
            {
                schedulerService.Start();
                schedulerToggleButton.Text = "스케줄러 중지";
                schedulerToggleButton.BackColor = UITheme.ErrorColor;
            }
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
            
            // 스케줄이 활성화된 프로그램은 약간 다른 색상으로 표시
            if (programInfo.IsScheduleEnabled)
            {
                backColor = System.Drawing.Color.FromArgb(
                    Math.Max(0, backColor.R - 20),
                    Math.Min(255, backColor.G + 20),
                    Math.Max(0, backColor.B - 10)
                );
            }
            
            programsGrid.Rows[e.RowIndex].DefaultCellStyle.BackColor = backColor;
        }

        /// <summary>
        /// 그리드 편집 시작 이벤트 - 편집을 방지합니다
        /// </summary>
        private void ProgramsGrid_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            // 모든 편집 시도를 취소
            e.Cancel = true;
        }

        /// <summary>
        /// 그리드 더블클릭 이벤트 - 편집 모드 진입을 방지합니다
        /// </summary>
        private void ProgramsGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            // 실행 버튼이 아닌 셀을 더블클릭한 경우 편집 모드로 들어가지 않도록 함
            if (programsGrid.Columns[e.ColumnIndex].Name != "ExecuteColumn")
            {
                // 스케줄 설정 대화상자 표시
                var programInfo = GetProgramInfoFromRow(e.RowIndex);
                if (programInfo != null)
                {
                    ShowScheduleDialog(programInfo);
                }
            }
        }

        /// <summary>
        /// 키 입력 이벤트 - F2, Enter 등으로 편집 모드 진입을 방지합니다
        /// </summary>
        private void ProgramsGrid_KeyDown(object sender, KeyEventArgs e)
        {
            // F2, Enter, 기타 편집 관련 키 입력을 무시
            if (e.KeyCode == Keys.F2 || e.KeyCode == Keys.Enter || 
                e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// 스케줄러에서 프로그램이 실행되었을 때 이벤트
        /// </summary>
        private void OnProgramExecuted(object? sender, ProgramExecutedEventArgs e)
        {
            try
            {
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.Invoke(() =>
                    {
                        var status = e.Success ? "성공" : $"실패 ({e.ErrorMessage})";
                        UpdateStatus($"스케줄 실행: {e.Program.DisplayName} - {status}");
                        
                        // 실행 이력 추가
                        AddExecutionHistory(e.Program, e.ExecutionTime, e.Success, e.ErrorMessage);
                        
                        // 그리드 새로고침 (다음 실행 시간 업데이트)
                        RefreshProgramList();
                    });
                }
            }
            catch (ObjectDisposedException)
            {
                // 폼이 해제된 경우 무시
            }
            catch (InvalidOperationException)
            {
                // 스레드 간 호출 문제 발생 시 무시
            }
        }

        /// <summary>
        /// 스케줄러 상태 업데이트 이벤트
        /// </summary>
        private void OnSchedulerStatusUpdated(object? sender, string message)
        {
            try
            {
                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.Invoke(() => UpdateStatus($"스케줄러: {message}"));
                }
            }
            catch (ObjectDisposedException)
            {
                // 폼이 해제된 경우 무시
            }
            catch (InvalidOperationException)
            {
                // 스레드 간 호출 문제 발생 시 무시
            }
        }

        /// <summary>
        /// 폼 크기 변경 이벤트
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            AdjustControlSizes();
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
                var scheduledCount = currentPrograms.Count(p => p.IsScheduleEnabled);
                UpdateStatus($"총 {currentPrograms.Count}개의 프로그램이 발견되었습니다. (스케줄: {scheduledCount}개)");
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
            Console.WriteLine($"MainForm.ExecuteProgram 호출: {programInfo.ProgramName}.{programInfo.DisplayName}");
            
            // 시작 로그 추가
            AddExecutionStartHistory(programInfo, DateTime.Now);
            
            try
            {
                // ProgramManager에서 실행 (DB 저장 포함)
                programManager.ExecuteProgram(programInfo);
                
                // 종료 로그 추가 (성공)
                AddExecutionEndHistory(programInfo, DateTime.Now, true, null);
                
                UpdateStatus($"프로그램 실행: {programInfo.ProgramName} - {programInfo.DisplayName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MainForm.ExecuteProgram 예외: {ex.Message}");
                
                // 종료 로그 추가 (실패)
                AddExecutionEndHistory(programInfo, DateTime.Now, false, ex.Message);
                
                ShowError($"프로그램 실행 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 실행 시작 이력을 추가합니다
        /// </summary>
        private void AddExecutionStartHistory(ProgramInfo program, DateTime executionTime)
        {
            // 날짜가 바뀌었으면 이력 초기화
            CheckAndClearDailyHistory();

            var startHistory = new ExecutionHistory
            {
                ProgramName = program.ProgramName,
                DisplayName = program.DisplayName,
                ExecutionTime = executionTime,
                Success = true, // 시작은 항상 성공으로 간주
                ErrorMessage = null,
                Status = "start"
            };

            // 최신 이력을 맨 앞에 추가
            executionHistory.Insert(0, startHistory);

            // 최대 개수 초과시 오래된 이력 제거
            if (executionHistory.Count > MaxHistoryCount)
            {
                executionHistory.RemoveAt(executionHistory.Count - 1);
            }

            UpdateHistoryDisplay();
        }

        /// <summary>
        /// 실행 종료 이력을 추가합니다
        /// </summary>
        private void AddExecutionEndHistory(ProgramInfo program, DateTime executionTime, bool success, string? errorMessage)
        {
            var endHistory = new ExecutionHistory
            {
                ProgramName = program.ProgramName,
                DisplayName = program.DisplayName,
                ExecutionTime = executionTime,
                Success = success,
                ErrorMessage = errorMessage,
                Status = "end"
            };

            // 최신 이력을 맨 앞에 추가
            executionHistory.Insert(0, endHistory);

            // 최대 개수 초과시 오래된 이력 제거
            if (executionHistory.Count > MaxHistoryCount)
            {
                executionHistory.RemoveAt(executionHistory.Count - 1);
            }

            UpdateHistoryDisplay();
        }

        /// <summary>
        /// 실행 이력을 추가합니다 (스케줄러용)
        /// </summary>
        private void AddExecutionHistory(ProgramInfo program, DateTime executionTime, bool success, string? errorMessage)
        {
            // 스케줄러 실행은 시작/종료를 별도로 추가하지 않고 결과만 추가
            CheckAndClearDailyHistory();

            var history = new ExecutionHistory
            {
                ProgramName = program.ProgramName,
                DisplayName = program.DisplayName,
                ExecutionTime = executionTime,
                Success = success,
                ErrorMessage = errorMessage,
                Status = "end" // 스케줄러는 종료 결과만 표시
            };

            // 최신 이력을 맨 앞에 추가
            executionHistory.Insert(0, history);

            // 최대 개수 초과시 오래된 이력 제거
            if (executionHistory.Count > MaxHistoryCount)
            {
                executionHistory.RemoveAt(executionHistory.Count - 1);
            }

            UpdateHistoryDisplay();
        }

        /// <summary>
        /// 일일 이력 초기화 체크
        /// </summary>
        private void CheckAndClearDailyHistory()
        {
            var today = DateTime.Today;
            if (today > lastHistoryClearDate)
            {
                executionHistory.Clear();
                lastHistoryClearDate = today;
                UpdateStatus($"새로운 날({today:yyyy-MM-dd})이 시작되어 실행 이력이 초기화되었습니다.");
            }
        }

        /// <summary>
        /// 스케줄 설정 대화상자를 표시합니다
        /// </summary>
        private void ShowScheduleDialog(ProgramInfo programInfo)
        {
            var dialog = new ScheduleDialog(programInfo);
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                schedulerService.UpdateProgramSchedule(
                    programInfo,
                    dialog.SelectedScheduleType,
                    dialog.SelectedTime,
                    dialog.IntervalMinutes,
                    dialog.IsEnabled,
                    dialog.SelectedDayOfWeek,
                    dialog.SelectedDayOfMonth,
                    dialog.IsLastDayOfMonth
                );
                RefreshProgramList();
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
                var nextExecution = program.NextExecution?.ToString("MM-dd HH:mm") ?? "-";
                var rowIndex = programsGrid.Rows.Add(
                    program.ProgramName,
                    program.DisplayName,
                    program.Memo,
                    program.ScheduleDescription,
                    nextExecution,
                    "실행"
                );

                // 마지막 수정일시를 툴팁으로 추가
                programsGrid.Rows[rowIndex].Cells[0].ToolTipText =
                    $"마지막 수정: {program.LastModified:yyyy-MM-dd HH:mm:ss}\n더블클릭하여 스케줄 설정";
            }
        }

        /// <summary>
        /// 실행 이력 표시를 업데이트합니다
        /// </summary>
        private void UpdateHistoryDisplay()
        {
            historyListBox.Items.Clear();
            
            foreach (var history in executionHistory)
            {
                historyListBox.Items.Add(history.DisplayText);
            }

            // 제목에 오늘 날짜와 이력 개수 표시
            historyTitleLabel.Text = $"실행 이력 {DateTime.Today:MM/dd} ({executionHistory.Count})";
        }

        /// <summary>
        /// 컨트롤 크기를 동적으로 조정합니다
        /// </summary>
        private void AdjustControlSizes()
        {
            if (this.WindowState == FormWindowState.Minimized)
                return;

            var clientWidth = this.ClientSize.Width;
            var clientHeight = this.ClientSize.Height;

            // 상단 패널 크기 조정
            if (topPanel != null)
            {
                topPanel.Width = clientWidth - (UITheme.DefaultPadding * 2);
                
                // 상단 패널 내의 버튼들을 오른쪽 끝에 위치
                AdjustTopPanelButtons();
            }

            // 그리드와 이력 패널 크기 조정
            if (programsGrid != null && historyPanel != null)
            {
                var historyPanelWidth = 350; // 이력 패널 너비
                var availableWidth = clientWidth - (UITheme.DefaultPadding * 3) - historyPanelWidth;
                var availableHeight = clientHeight - 160; // 상태바 공간 추가 (140→160)
                
                // 이력 패널을 그리드와 같은 높이에서 시작
                var historyStartY = 90; // 그리드와 동일한 시작 높이

                // 프로그램 그리드 크기 조정 (최소 너비 증가)
                programsGrid.Width = Math.Max(800, availableWidth); // 최소 너비: 600→800
                programsGrid.Height = availableHeight;

                // 이력 패널 위치와 크기 조정 (그리드와 같은 높이에서 시작)
                historyPanel.Location = new System.Drawing.Point(programsGrid.Width + UITheme.DefaultPadding * 2, historyStartY);
                historyPanel.Width = historyPanelWidth;
                historyPanel.Height = availableHeight; // 그리드와 같은 높이
                
                // 이력 리스트박스 크기도 함께 조정
                if (historyListBox != null)
                {
                    historyListBox.Width = historyPanelWidth - 20;
                    historyListBox.Height = historyPanel.Height - 60; // 제목 라벨 공간 확보
                }
            }
        }

        /// <summary>
        /// 상단 패널의 버튼들을 오른쪽 끝에 배치
        /// </summary>
        private void AdjustTopPanelButtons()
        {
            if (topPanel == null || refreshButton == null || schedulerToggleButton == null)
                return;

            var panelWidth = topPanel.Width;
            var buttonSpacing = 12; // 버튼 간격 조금 증가
            var rightMargin = UITheme.DefaultPadding; // 오른쪽 여백

            // 스케줄러 토글 버튼을 가장 오른쪽 끝에 위치
            schedulerToggleButton.Location = new System.Drawing.Point(
                panelWidth - schedulerToggleButton.Width - rightMargin,
                12
            );

            // 새로고침 버튼을 스케줄러 버튼 왼쪽에 위치
            refreshButton.Location = new System.Drawing.Point(
                schedulerToggleButton.Location.X - refreshButton.Width - buttonSpacing,
                12
            );

            // 버튼들이 상단 패널 앞쪽으로 오도록 Z-order 조정
            schedulerToggleButton.BringToFront();
            refreshButton.BringToFront();
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

        #region 폼 이벤트
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            schedulerService.Stop();
            schedulerService.Dispose();
            base.OnFormClosing(e);
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
