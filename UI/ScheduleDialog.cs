using System;
using System.Drawing;
using System.Windows.Forms;

namespace scheduleProgram
{
    /// <summary>
    /// 스케줄 설정 대화상자
    /// </summary>
    public partial class ScheduleDialog : Form
    {
        public ScheduleType SelectedScheduleType { get; private set; }
        public DateTime? SelectedTime { get; private set; }
        public int IntervalMinutes { get; private set; }
        public bool IsEnabled { get; private set; }
        public DayOfWeek SelectedDayOfWeek { get; private set; }
        public int SelectedDayOfMonth { get; private set; }
        public bool IsLastDayOfMonth { get; private set; }

        private readonly ProgramInfo _programInfo;
        private CheckBox enabledCheckBox;
        private ComboBox scheduleTypeComboBox;
        private DateTimePicker datePicker;
        private DateTimePicker timePicker;
        private NumericUpDown intervalNumeric;
        private Label timeLabel;
        private Label intervalLabel;
        
        // 매주 스케줄을 위한 컨트롤들
        private Label dayOfWeekLabel;
        private ComboBox dayOfWeekComboBox;
        
        // 매월 스케줄을 위한 컨트롤들
        private Label dayOfMonthLabel;
        private ComboBox dayOfMonthComboBox;

        public ScheduleDialog(ProgramInfo programInfo)
        {
            _programInfo = programInfo;
            InitializeDialog();
            LoadCurrentSettings();
        }

        private void InitializeDialog()
        {
            this.Text = $"스케줄 설정 - {_programInfo.DisplayName}";
            this.Size = new Size(480, 380);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 프로그램 정보 라벨
            var programLabel = new Label()
            {
                Text = $"프로그램: {_programInfo.ProgramName} - {_programInfo.DisplayName}",
                Location = new Point(12, 15),
                Size = new Size(450, 20),
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold)
            };

            // 스케줄 활성화 체크박스
            enabledCheckBox = new CheckBox()
            {
                Text = "스케줄 활성화",
                Location = new Point(12, 45),
                Size = new Size(120, 25),
                Checked = _programInfo.IsScheduleEnabled
            };
            enabledCheckBox.CheckedChanged += EnabledCheckBox_CheckedChanged;

            // 스케줄 타입 라벨
            var scheduleTypeLabel = new Label()
            {
                Text = "스케줄 타입:",
                Location = new Point(12, 80),
                Size = new Size(80, 23)
            };

            // 스케줄 타입 콤보박스
            scheduleTypeComboBox = new ComboBox()
            {
                Location = new Point(100, 78),
                Size = new Size(120, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            scheduleTypeComboBox.Items.AddRange(new[] { "없음", "한 번만", "매일", "매주", "매월", "주기적" });
            scheduleTypeComboBox.SelectedIndexChanged += ScheduleTypeComboBox_SelectedIndexChanged;

            // 날짜 선택 (한 번만 실행용)
            var dateLabel = new Label()
            {
                Text = "날짜:",
                Location = new Point(12, 115),
                Size = new Size(50, 23)
            };

            datePicker = new DateTimePicker()
            {
                Location = new Point(70, 113),
                Size = new Size(150, 23),
                Format = DateTimePickerFormat.Short
            };

            // 요일 선택 (매주용)
            dayOfWeekLabel = new Label()
            {
                Text = "요일:",
                Location = new Point(12, 115),
                Size = new Size(50, 23)
            };

            dayOfWeekComboBox = new ComboBox()
            {
                Location = new Point(70, 113),
                Size = new Size(100, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            dayOfWeekComboBox.Items.AddRange(new[] { "일요일", "월요일", "화요일", "수요일", "목요일", "금요일", "토요일" });

            // 일자 선택 (매월용)
            dayOfMonthLabel = new Label()
            {
                Text = "일자:",
                Location = new Point(12, 115),
                Size = new Size(50, 23)
            };

            dayOfMonthComboBox = new ComboBox()
            {
                Location = new Point(70, 113),
                Size = new Size(100, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            
            // 1일부터 31일까지 + 말일 옵션 추가
            for (int i = 1; i <= 31; i++)
            {
                dayOfMonthComboBox.Items.Add($"{i}일");
            }
            dayOfMonthComboBox.Items.Add("말일");

            // 시간 라벨
            timeLabel = new Label()
            {
                Text = "시간:",
                Location = new Point(240, 115),
                Size = new Size(40, 23)
            };

            // 시간 선택
            timePicker = new DateTimePicker()
            {
                Location = new Point(285, 113),
                Size = new Size(120, 23),
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true
            };

            // 간격 라벨
            intervalLabel = new Label()
            {
                Text = "실행 간격 (분):",
                Location = new Point(12, 150),
                Size = new Size(100, 23)
            };

            // 간격 입력
            intervalNumeric = new NumericUpDown()
            {
                Location = new Point(120, 148),
                Size = new Size(80, 23),
                Minimum = 1,
                Maximum = 1440, // 24시간
                Value = _programInfo.IntervalMinutes
            };

            // 현재 설정 표시
            var currentLabel = new Label()
            {
                Text = $"현재 설정: {_programInfo.ScheduleDescription}",
                Location = new Point(12, 185),
                Size = new Size(450, 60),
                ForeColor = Color.DarkBlue,
                Font = new Font("맑은 고딕", 8F)
            };

            // 도움말 라벨
            var helpLabel = new Label()
            {
                Text = "? 매주: 특정 요일에 실행\n? 매월: 특정 일자나 말일에 실행\n? 주기적: 지정된 시간 간격으로 실행",
                Location = new Point(12, 250),
                Size = new Size(450, 50),
                ForeColor = Color.Gray,
                Font = new Font("맑은 고딕", 7.5F)
            };

            // 확인 버튼
            var okButton = new Button()
            {
                Text = "확인",
                DialogResult = DialogResult.OK,
                Location = new Point(300, 310),
                Size = new Size(75, 30)
            };
            okButton.Click += OkButton_Click;

            // 취소 버튼
            var cancelButton = new Button()
            {
                Text = "취소",
                DialogResult = DialogResult.Cancel,
                Location = new Point(385, 310),
                Size = new Size(75, 30)
            };

            this.Controls.AddRange(new Control[] {
                programLabel, enabledCheckBox, scheduleTypeLabel, scheduleTypeComboBox,
                dateLabel, datePicker, dayOfWeekLabel, dayOfWeekComboBox, 
                dayOfMonthLabel, dayOfMonthComboBox, timeLabel, timePicker,
                intervalLabel, intervalNumeric, currentLabel, helpLabel,
                okButton, cancelButton
            });

            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        private void LoadCurrentSettings()
        {
            enabledCheckBox.Checked = _programInfo.IsScheduleEnabled;
            scheduleTypeComboBox.SelectedIndex = (int)_programInfo.ScheduleType;
            
            // 요일 설정
            dayOfWeekComboBox.SelectedIndex = (int)_programInfo.SelectedDayOfWeek;
            
            // 일자 설정
            if (_programInfo.IsLastDayOfMonth)
            {
                dayOfMonthComboBox.SelectedIndex = 31; // "말일" 선택
            }
            else
            {
                dayOfMonthComboBox.SelectedIndex = _programInfo.SelectedDayOfMonth - 1; // 0-based index
            }
            
            if (_programInfo.ScheduledTime.HasValue)
            {
                datePicker.Value = _programInfo.ScheduledTime.Value.Date;
                timePicker.Value = DateTime.Today.Add(_programInfo.ScheduledTime.Value.TimeOfDay);
            }
            else
            {
                datePicker.Value = DateTime.Today;
                timePicker.Value = DateTime.Today.AddHours(9); // 기본 오전 9시
            }

            intervalNumeric.Value = _programInfo.IntervalMinutes;
            UpdateControlsVisibility();
        }

        private void EnabledCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            UpdateControlsVisibility();
        }

        private void ScheduleTypeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateControlsVisibility();
        }

        private void UpdateControlsVisibility()
        {
            var enabled = enabledCheckBox.Checked;
            var scheduleType = (ScheduleType)scheduleTypeComboBox.SelectedIndex;

            scheduleTypeComboBox.Enabled = enabled;
            
            // 기본적으로 모든 컨트롤 숨기기
            datePicker.Visible = false;
            dayOfWeekLabel.Visible = false;
            dayOfWeekComboBox.Visible = false;
            dayOfMonthLabel.Visible = false;
            dayOfMonthComboBox.Visible = false;
            timePicker.Visible = false;
            timeLabel.Visible = false;
            intervalNumeric.Visible = false;
            intervalLabel.Visible = false;

            if (!enabled || scheduleType == ScheduleType.None)
                return;

            // 스케줄 타입에 따라 필요한 컨트롤만 표시
            switch (scheduleType)
            {
                case ScheduleType.Once:
                    datePicker.Visible = true;
                    timePicker.Visible = true;
                    timeLabel.Visible = true;
                    break;

                case ScheduleType.Daily:
                    timePicker.Visible = true;
                    timeLabel.Visible = true;
                    break;

                case ScheduleType.Weekly:
                    dayOfWeekLabel.Visible = true;
                    dayOfWeekComboBox.Visible = true;
                    timePicker.Visible = true;
                    timeLabel.Visible = true;
                    break;

                case ScheduleType.Monthly:
                    dayOfMonthLabel.Visible = true;
                    dayOfMonthComboBox.Visible = true;
                    timePicker.Visible = true;
                    timeLabel.Visible = true;
                    break;

                case ScheduleType.Interval:
                    intervalLabel.Visible = true;
                    intervalNumeric.Visible = true;
                    break;
            }
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            IsEnabled = enabledCheckBox.Checked;
            SelectedScheduleType = (ScheduleType)scheduleTypeComboBox.SelectedIndex;
            IntervalMinutes = (int)intervalNumeric.Value;
            
            // 요일 설정
            SelectedDayOfWeek = (DayOfWeek)dayOfWeekComboBox.SelectedIndex;
            
            // 일자 설정
            if (dayOfMonthComboBox.SelectedIndex == 31) // "말일" 선택
            {
                IsLastDayOfMonth = true;
                SelectedDayOfMonth = 1; // 기본값
            }
            else
            {
                IsLastDayOfMonth = false;
                SelectedDayOfMonth = dayOfMonthComboBox.SelectedIndex + 1; // 1-based
            }

            if (IsEnabled && SelectedScheduleType != ScheduleType.None && SelectedScheduleType != ScheduleType.Interval)
            {
                SelectedTime = SelectedScheduleType == ScheduleType.Once 
                    ? datePicker.Value.Date.Add(timePicker.Value.TimeOfDay)
                    : DateTime.Today.Add(timePicker.Value.TimeOfDay);
            }
            else
            {
                SelectedTime = null;
            }
        }
    }
}