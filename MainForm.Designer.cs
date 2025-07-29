using System;
using System.Drawing;
using System.Windows.Forms;

namespace scheduleProgram
{
    partial class MainForm
    {
        /// <summary>
        /// 디자이너에서 필요한 변수들
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 폼의 리소스를 정리합니다
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 정리해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.Text = "프로그램 관리자 Pro";
            this.Size = new Size(1400, 700); // 크기 증가: 1300→1400, 600→700
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1300, 600); // 최소 크기도 증가
            this.BackColor = UITheme.BackgroundColor;

            InitializeTopPanel();
            InitializeProgramsGrid();
            InitializeHistoryPanel(); // 이력 패널 초기화 추가
            InitializeStatusBar();

            // 폼 초기화 후 첫 번째 버튼 위치 조정
            this.Load += (s, e) => AdjustControlSizes();
            
            // 폼 크기 변경 시 컨트롤 크기 조정
            this.Resize += (s, e) => AdjustControlSizes();
        }

        /// <summary>
        /// 폼의 기본 속성들을 설정합니다
        /// </summary>
        private void InitializeFormProperties()
        {
            this.Text = "프로그램 관리자 Pro";
            this.Size = new Size(1400, 700); // 크기 증가: 1300→1400, 600→700
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1300, 600); // 최소 크기도 증가
            this.BackColor = UITheme.BackgroundColor;
            this.Font = UITheme.DefaultFont;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(800, 600);
        }

        /// <summary>
        /// 상단 패널을 초기화합니다
        /// </summary>
        private void InitializeTopPanel()
        {
            topPanel = UIComponents.CreateStyledPanel(
                new Point(UITheme.DefaultPadding, UITheme.DefaultPadding),
                new Size(1350, 60) // 너비 증가: 950→1350
            );

            // 제목 라벨
            var titleLabel = UIComponents.CreateStyledLabel(
                "등록된 프로그램 목록",
                new Point(UITheme.LargePadding, 15),
                new Size(400, 30),
                UITheme.LargeFont
            );

            // 새로고침 버튼 (위치는 AdjustTopPanelButtons에서 동적 조정)
            refreshButton = UIComponents.CreateStyledButton(
                "새로고침",
                new Point(650, 10), // 임시 위치
                UITheme.PrimaryColor,
                new Size(120, 40)
            );
            refreshButton.Click += RefreshButton_Click;

            // 스케줄러 토글 버튼 (위치는 AdjustTopPanelButtons에서 동적 조정)
            schedulerToggleButton = UIComponents.CreateStyledButton(
                "스케줄러 중지",
                new Point(780, 10), // 임시 위치
                UITheme.ErrorColor,
                new Size(140, 40)
            );
            schedulerToggleButton.Click += SchedulerToggleButton_Click;

            topPanel.Controls.AddRange(new Control[] {
                titleLabel, refreshButton, schedulerToggleButton
            });
            
            // ? 중요: 상단 패널을 폼에 추가!
            this.Controls.Add(topPanel);
        }

        /// <summary>
        /// 프로그램 그리드를 초기화합니다
        /// </summary>
        private void InitializeProgramsGrid()
        {
            programsGrid = new DataGridView()
            {
                Location = new Point(UITheme.DefaultPadding, 90),
                Size = new Size(1000, 500), // 크기 증가: 850x400 → 1000x500
                BackgroundColor = UITheme.LightColor,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToOrderColumns = false,
                ReadOnly = false, // 버튼 클릭을 위해 ReadOnly는 false
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 35,
                RowHeadersVisible = false,
                Font = UITheme.DefaultFont
            };

            // 그리드 스타일 설정
            programsGrid.DefaultCellStyle.BackColor = UITheme.LightColor;
            programsGrid.DefaultCellStyle.ForeColor = UITheme.DarkColor;
            programsGrid.DefaultCellStyle.SelectionBackColor = UITheme.GetHoverColor(UITheme.PrimaryColor);
            programsGrid.DefaultCellStyle.SelectionForeColor = UITheme.LightColor;
            programsGrid.ColumnHeadersDefaultCellStyle.BackColor = UITheme.PrimaryColor;
            programsGrid.ColumnHeadersDefaultCellStyle.ForeColor = UITheme.LightColor;
            programsGrid.ColumnHeadersDefaultCellStyle.Font = UITheme.BoldFont;
            
            // 컬럼 헤더 중앙 정렬
            programsGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            SetupGridColumns();

            // 이벤트 연결
            programsGrid.CellClick += ProgramsGrid_CellClick;
            programsGrid.CellFormatting += ProgramsGrid_CellFormatting;
            programsGrid.CellBeginEdit += ProgramsGrid_CellBeginEdit;
            programsGrid.CellDoubleClick += ProgramsGrid_CellDoubleClick;
            programsGrid.KeyDown += ProgramsGrid_KeyDown;

            this.Controls.Add(programsGrid);
        }

        /// <summary>
        /// 그리드 컬럼을 설정합니다
        /// </summary>
        private void SetupGridColumns()
        {
            // 프로그램명 컬럼 (읽기 전용)
            var programNameColumn = new DataGridViewTextBoxColumn()
            {
                Name = "ProgramNameColumn",
                HeaderText = "프로그램명",
                FillWeight = 12,
                ReadOnly = true,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };

            // CS파일명 컬럼 (읽기 전용)
            var fileNameColumn = new DataGridViewTextBoxColumn()
            {
                Name = "FileNameColumn",
                HeaderText = "CS파일명",
                FillWeight = 18,
                ReadOnly = true,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
            };

            // 메모 컬럼 (읽기 전용)
            var memoColumn = new DataGridViewTextBoxColumn()
            {
                Name = "MemoColumn",
                HeaderText = "메모",
                FillWeight = 25,
                ReadOnly = true,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
            };

            // 스케줄 설정 컬럼 (읽기 전용)
            var scheduleColumn = new DataGridViewTextBoxColumn()
            {
                Name = "ScheduleColumn",
                HeaderText = "스케줄 설정",
                FillWeight = 22,
                ReadOnly = true,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
            };

            // 다음 실행 컬럼 (읽기 전용)
            var nextExecutionColumn = new DataGridViewTextBoxColumn()
            {
                Name = "NextExecutionColumn",
                HeaderText = "다음 실행",
                FillWeight = 11,
                ReadOnly = true,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };

            // 즉시실행 버튼 컬럼 (버튼은 기본적으로 편집 불가)
            var executeColumn = new DataGridViewButtonColumn()
            {
                Name = "ExecuteColumn",
                HeaderText = "즉시실행",
                FillWeight = 12, // 크기 증가: 8→12
                UseColumnTextForButtonValue = false,
                DefaultCellStyle = { 
                    BackColor = UITheme.AccentColor,
                    ForeColor = UITheme.LightColor,
                    SelectionBackColor = UITheme.GetHoverColor(UITheme.AccentColor),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            };

            // 모든 컬럼 헤더를 중앙 정렬로 설정
            programNameColumn.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            fileNameColumn.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            memoColumn.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            scheduleColumn.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            nextExecutionColumn.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            executeColumn.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;

            programsGrid.Columns.AddRange(new DataGridViewColumn[] {
                programNameColumn, fileNameColumn, memoColumn, scheduleColumn, nextExecutionColumn, executeColumn
            });
        }

        /// <summary>
        /// 실행 이력 패널을 초기화합니다
        /// </summary>
        private void InitializeHistoryPanel()
        {
            // 이력 패널을 그리드와 같은 높이에서 시작 (90px)
            var historyStartY = 90; // 그리드와 동일한 시작 높이
            
            // 이력 패널 생성
            historyPanel = UIComponents.CreateStyledPanel(
                new Point(1020, historyStartY), // Y 위치: 125→90 (그리드와 같은 높이)
                new Size(350, 500) // 높이 복원: 465→500
            );

            // 이력 제목 라벨
            historyTitleLabel = UIComponents.CreateStyledLabel(
                $"실행 이력 {DateTime.Today:MM/dd} (0)",
                new Point(10, 10),
                new Size(330, 25),
                UITheme.BoldFont
            );

            // 이력 리스트박스 - 스크롤바 없이 크게
            historyListBox = new ListBox()
            {
                Location = new Point(10, 45),
                Size = new Size(330, 445), // 높이 복원: 410→445
                BackColor = UITheme.LightColor,
                ForeColor = UITheme.DarkColor,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new System.Drawing.Font("Consolas", 8.5F), // 폰트 크기 증가
                ScrollAlwaysVisible = false, // 스크롤바 숨김
                HorizontalScrollbar = false, // 가로 스크롤바 제거
                IntegralHeight = false // 부분 항목 표시 허용
            };

            // 컨트롤 추가
            historyPanel.Controls.AddRange(new Control[] {
                historyTitleLabel, historyListBox
            });

            this.Controls.Add(historyPanel);
        }

        /// <summary>
        /// 상태바를 초기화합니다
        /// </summary>
        private void InitializeStatusBar()
        {
            (statusStrip, statusLabel) = UIComponents.CreateStatusBar();
            
            // ? 중요: 상태바를 폼에 추가!
            this.Controls.Add(statusStrip);
        }

        /// <summary>
        /// 컨트롤 계층 구조를 설정합니다
        /// </summary>
        private void SetupControlHierarchy()
        {
            this.Controls.AddRange(new Control[] {
                topPanel, programsGrid, statusStrip
            });
        }

        /// <summary>
        /// 모던 테마를 적용합니다
        /// </summary>
        private void ApplyModernTheme()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.UserPaint |
                         ControlStyles.DoubleBuffer, true);
        }
    }
}