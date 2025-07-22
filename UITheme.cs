using System.Drawing;

namespace scheduleProgram
{
    /// <summary>
    /// 애플리케이션의 색상 테마와 폰트를 관리하는 클래스
    /// </summary>
    public static class UITheme
    {
        // 색상 팔레트
        public static readonly Color PrimaryColor = Color.FromArgb(41, 128, 185);      // 블루
        public static readonly Color SecondaryColor = Color.FromArgb(52, 152, 219);    // 라이트 블루
        public static readonly Color AccentColor = Color.FromArgb(230, 126, 34);       // 오렌지
        public static readonly Color BackgroundColor = Color.FromArgb(236, 240, 241);  // 라이트 그레이
        public static readonly Color DarkColor = Color.FromArgb(44, 62, 80);           // 다크 그레이
        public static readonly Color LightColor = Color.White;
        public static readonly Color ErrorColor = Color.FromArgb(231, 76, 60);         // 레드
        public static readonly Color BorderColor = Color.FromArgb(220, 220, 220);
        public static readonly Color TextColor = Color.FromArgb(44, 62, 80);
        public static readonly Color PlaceholderColor = Color.FromArgb(149, 165, 166);

        // 폰트
        public static readonly Font DefaultFont = new Font("맑은 고딕", 9F, FontStyle.Regular);
        public static readonly Font BoldFont = new Font("맑은 고딕", 10F, FontStyle.Bold);
        public static readonly Font LargeFont = new Font("맑은 고딕", 12F, FontStyle.Bold);
        public static readonly Font SmallFont = new Font("맑은 고딕", 8F, FontStyle.Regular);

        // 크기 및 여백
        public static readonly int DefaultPadding = 15;
        public static readonly int SmallPadding = 10;
        public static readonly int LargePadding = 20;
        public static readonly int ButtonHeight = 40;
        public static readonly int InputHeight = 30;
        public static readonly int BorderRadius = 8;
        public static readonly int PanelRadius = 10;

        // 상태별 색상
        public static Color GetDateStatusColor(DateTime date)
        {
            if (date < DateTime.Now.Date)
                return Color.Gray;
            else if (date == DateTime.Now.Date)
                return AccentColor;
            else
                return DarkColor;
        }

        // 호버 효과 색상
        public static Color GetHoverColor(Color baseColor) => ControlPaint.Light(baseColor, 0.2f);
        public static Color GetPressedColor(Color baseColor) => ControlPaint.Dark(baseColor, 0.1f);
    }
}