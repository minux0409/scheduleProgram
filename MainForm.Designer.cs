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
            this.SuspendLayout();

            // 폼 기본 설정
            InitializeFormProperties();

            // 상단 패널 초기화
            InitializeTopPanel();

            // 메인 그리드 초기화
            InitializeProgramsGrid();

            // 상태바 초기화
            InitializeStatusBar();

            // 컨트롤 계층 구조 설정
            SetupControlHierarchy();

            // 테마 적용
            ApplyModernTheme();

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        /// <summary>
        /// 폼의 기본 속성들을 설정합니다
        /// </summary>
        private void InitializeFormProperties()
        {
            this.Text = "?? 프로그램 관리자 Pro";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
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
                new Size(950, 60)
            );

            // 제목 라벨
            var titleLabel = UIComponents.CreateStyledLabel(
                "?? 등록된 프로그램 목록",
                new Point(UITheme.LargePadding, 15),
                new Size(300, 30),
                UITheme.LargeFont
            );

            // 새로고침 버튼
            refreshButton = UIComponents.CreateStyledButton(
                "?? 새로고침",
                new Point(700, 10),
                UITheme.PrimaryColor,
                new Size(120, 40)
            );
            refreshButton.Click += RefreshButton_Click;

            // 폴더 추가 버튼
            addFolderButton = UIComponents.CreateStyledButton(
                "?? 폴더 추가",
                new Point(830, 10),
                UITheme.AccentColor,
                new Size(120, 40)
            );
            addFolderButton.Click += AddFolderButton_Click;

            topPanel.Controls.AddRange(new Control[] {
                titleLabel, refreshButton, addFolderButton
            });
        }

        /// <summary>
        /// 프로그램 그리드를 초기화합니다
        /// </summary>
        private void InitializeProgramsGrid()
        {
            programsGrid = new DataGridView()
            {
                Location = new Point(UITheme.DefaultPadding, 90),
                Size = new Size(950, 520),
                BackgroundColor = UITheme.LightColor,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                GridColor = UITheme.BorderColor,
                Font = UITheme.DefaultFont,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false
            };

            // 컬럼 설정
            SetupGridColumns();

            // 헤더 스타일 설정
            programsGrid.ColumnHeadersDefaultCellStyle.BackColor = UITheme.PrimaryColor;
            programsGrid.ColumnHeadersDefaultCellStyle.ForeColor = UITheme.LightColor;
            programsGrid.ColumnHeadersDefaultCellStyle.Font = UITheme.BoldFont;
            programsGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            programsGrid.ColumnHeadersHeight = 40;

            // 행 스타일 설정
            programsGrid.DefaultCellStyle.SelectionBackColor = UITheme.SecondaryColor;
            programsGrid.DefaultCellStyle.SelectionForeColor = UITheme.LightColor;
            programsGrid.RowTemplate.Height = 35;

            // 이벤트 연결
            programsGrid.CellClick += ProgramsGrid_CellClick;
            programsGrid.CellFormatting += ProgramsGrid_CellFormatting;
        }

        /// <summary>
        /// 그리드 컬럼을 설정합니다
        /// </summary>
        private void SetupGridColumns()
        {
            // 프로그램명 컬럼
            var programNameColumn = new DataGridViewTextBoxColumn()
            {
                Name = "ProgramNameColumn",
                HeaderText = "프로그램명",
                FillWeight = 20,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };

            // CS파일명 컬럼
            var fileNameColumn = new DataGridViewTextBoxColumn()
            {
                Name = "FileNameColumn",
                HeaderText = "CS파일명",
                FillWeight = 25,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
            };

            // 메모 컬럼
            var memoColumn = new DataGridViewTextBoxColumn()
            {
                Name = "MemoColumn",
                HeaderText = "메모",
                FillWeight = 40,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
            };

            // 즉시실행 버튼 컬럼
            var executeColumn = new DataGridViewButtonColumn()
            {
                Name = "ExecuteColumn",
                HeaderText = "즉시실행",
                FillWeight = 15,
                UseColumnTextForButtonValue = false,
                DefaultCellStyle = { 
                    BackColor = UITheme.AccentColor,
                    ForeColor = UITheme.LightColor,
                    SelectionBackColor = UITheme.GetHoverColor(UITheme.AccentColor),
                    Alignment = DataGridViewContentAlignment.MiddleCenter
                }
            };

            programsGrid.Columns.AddRange(new DataGridViewColumn[] {
                programNameColumn, fileNameColumn, memoColumn, executeColumn
            });
        }

        /// <summary>
        /// 상태바를 초기화합니다
        /// </summary>
        private void InitializeStatusBar()
        {
            (statusStrip, statusLabel) = UIComponents.CreateStatusBar();
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