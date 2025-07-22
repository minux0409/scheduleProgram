using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace scheduleProgram
{
    /// <summary>
    /// 스타일링된 UI 컨트롤들을 생성하는 헬퍼 클래스
    /// </summary>
    public static class UIComponents
    {
        /// <summary>
        /// 스타일링된 라벨을 생성합니다
        /// </summary>
        public static Label CreateStyledLabel(string text, Point location, Size? size = null, Font? font = null)
        {
            return new Label()
            {
                Text = text,
                Location = location,
                Size = size ?? new Size(100, 25),
                Font = font ?? UITheme.BoldFont,
                ForeColor = UITheme.DarkColor,
                BackColor = Color.Transparent
            };
        }

        /// <summary>
        /// 스타일링된 텍스트박스를 생성합니다
        /// </summary>
        public static TextBox CreateStyledTextBox(Point location, Size size, bool multiline = false)
        {
            var textBox = new TextBox()
            {
                Location = location,
                Size = size,
                Font = UITheme.DefaultFont,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = UITheme.LightColor,
                ForeColor = UITheme.DarkColor,
                Multiline = multiline
            };

            if (multiline)
            {
                textBox.ScrollBars = ScrollBars.Vertical;
                textBox.AcceptsReturn = true;
            }

            return textBox;
        }

        /// <summary>
        /// 스타일링된 버튼을 생성합니다
        /// </summary>
        public static Button CreateStyledButton(string text, Point location, Color? backColor = null, Size? size = null)
        {
            var buttonColor = backColor ?? UITheme.PrimaryColor;
            var buttonSize = size ?? new Size(100, UITheme.ButtonHeight);

            var button = new Button()
            {
                Text = text,
                Location = location,
                Size = buttonSize,
                BackColor = buttonColor,
                ForeColor = UITheme.LightColor,
                Font = new Font(UITheme.DefaultFont.FontFamily, 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };

            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = UITheme.GetHoverColor(buttonColor);
            button.FlatAppearance.MouseDownBackColor = UITheme.GetPressedColor(buttonColor);

            // 둥근 모서리 효과 적용
            button.Paint += (s, e) => PaintRoundedButton(button, e);

            return button;
        }

        /// <summary>
        /// 스타일링된 패널을 생성합니다
        /// </summary>
        public static Panel CreateStyledPanel(Point location, Size size, Color? backColor = null)
        {
            var panel = new Panel()
            {
                Location = location,
                Size = size,
                BackColor = backColor ?? UITheme.LightColor,
                BorderStyle = BorderStyle.None
            };

            panel.Paint += (s, e) => PaintRoundedPanel(panel, e);
            return panel;
        }

        /// <summary>
        /// 스타일링된 그룹박스를 생성합니다
        /// </summary>
        public static GroupBox CreateStyledGroupBox(string text, Point location, Size size)
        {
            return new GroupBox()
            {
                Text = text,
                Location = location,
                Size = size,
                BackColor = Color.Transparent,
                Font = UITheme.BoldFont,
                ForeColor = UITheme.DarkColor
            };
        }

        /// <summary>
        /// 스타일링된 ListView를 생성합니다
        /// </summary>
        public static ListView CreateStyledListView(Point location, Size size)
        {
            var listView = new ListView()
            {
                Location = location,
                Size = size,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BorderStyle = BorderStyle.None,
                BackColor = UITheme.LightColor,
                Font = UITheme.DefaultFont
            };

            return listView;
        }

        /// <summary>
        /// 스타일링된 DateTimePicker를 생성합니다
        /// </summary>
        public static DateTimePicker CreateStyledDatePicker(Point location, Size size)
        {
            return new DateTimePicker()
            {
                Location = location,
                Size = size,
                Format = DateTimePickerFormat.Long,
                Font = UITheme.DefaultFont,
                CalendarForeColor = UITheme.DarkColor
            };
        }

        /// <summary>
        /// 상태바를 생성합니다
        /// </summary>
        public static (StatusStrip statusStrip, ToolStripStatusLabel statusLabel) CreateStatusBar()
        {
            var statusStrip = new StatusStrip()
            {
                BackColor = UITheme.DarkColor,
                ForeColor = UITheme.LightColor
            };

            var statusLabel = new ToolStripStatusLabel()
            {
                Text = "스케줄을 추가하거나 목록에서 선택하여 편집하세요.",
                Font = UITheme.DefaultFont,
                ForeColor = UITheme.LightColor
            };

            statusStrip.Items.Add(statusLabel);
            return (statusStrip, statusLabel);
        }

        /// <summary>
        /// 둥근 모서리 경로를 생성합니다
        /// </summary>
        public static GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.X + rect.Width - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.X + rect.Width - radius, rect.Y + rect.Height - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Y + rect.Height - radius, radius, radius, 90, 90);
            path.CloseAllFigures();
            return path;
        }

        /// <summary>
        /// 둥근 버튼을 그립니다
        /// </summary>
        private static void PaintRoundedButton(Button button, PaintEventArgs e)
        {
            var rect = button.ClientRectangle;
            using (var path = GetRoundedRectanglePath(rect, UITheme.BorderRadius))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(button.BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }
                var textRect = rect;
                textRect.Y += 2;
                TextRenderer.DrawText(e.Graphics, button.Text, button.Font,
                    textRect, button.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        /// <summary>
        /// 둥근 패널을 그립니다
        /// </summary>
        private static void PaintRoundedPanel(Panel panel, PaintEventArgs e)
        {
            var rect = panel.ClientRectangle;
            using (var path = GetRoundedRectanglePath(rect, UITheme.PanelRadius))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(panel.BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }
                using (var pen = new Pen(UITheme.BorderColor, 1))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }
    }
}