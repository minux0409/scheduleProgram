using System;
using System.IO;

namespace scheduleProgram
{
    /// <summary>
    /// 스케줄 실행 타입 열거형
    /// </summary>
    public enum ScheduleType
    {
        None,       // 스케줄 없음
        Once,       // 한 번만 실행
        Daily,      // 매일
        Weekly,     // 매주
        Monthly,    // 매월
        Interval    // 주기적 (분 단위)
    }

    /// <summary>
    /// 실행 가능한 프로그램 정보를 담는 클래스
    /// </summary>
    public class ProgramInfo
    {
        public string ProgramName { get; set; } = string.Empty;
        public string CsFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Memo { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public bool IsValid { get; set; } = true;

        // 스케줄링 관련 속성
        public ScheduleType ScheduleType { get; set; } = ScheduleType.None;
        public DateTime? ScheduledTime { get; set; }  // 예약 실행 시간
        public int IntervalMinutes { get; set; } = 60;  // 주기 실행 간격 (분)
        public DateTime? LastExecuted { get; set; }  // 마지막 실행 시간
        public DateTime? NextExecution { get; set; }  // 다음 실행 예정 시간
        public bool IsScheduleEnabled { get; set; } = false;  // 스케줄 활성화 여부

        // 매주 스케줄 속성
        public DayOfWeek SelectedDayOfWeek { get; set; } = DayOfWeek.Monday;  // 선택된 요일

        // 매월 스케줄 속성
        public int SelectedDayOfMonth { get; set; } = 1;  // 선택된 일자 (1-31)
        public bool IsLastDayOfMonth { get; set; } = false;  // 말일 여부

        public string DisplayName => Path.GetFileNameWithoutExtension(CsFileName);
        
        /// <summary>
        /// 스케줄 정보를 포함한 표시 문자열
        /// </summary>
        public string ScheduleDescription
        {
            get
            {
                if (!IsScheduleEnabled || ScheduleType == ScheduleType.None)
                    return "스케줄 없음";

                return ScheduleType switch
                {
                    ScheduleType.Once => $"한 번 실행: {ScheduledTime:yyyy-MM-dd HH:mm}",
                    ScheduleType.Daily => $"매일 {ScheduledTime:HH:mm}",
                    ScheduleType.Weekly => $"매주 {GetDayOfWeekKorean(SelectedDayOfWeek)} {ScheduledTime:HH:mm}",
                    ScheduleType.Monthly => IsLastDayOfMonth 
                        ? $"매월 말일 {ScheduledTime:HH:mm}"
                        : $"매월 {SelectedDayOfMonth}일 {ScheduledTime:HH:mm}",
                    ScheduleType.Interval => $"{IntervalMinutes}분 간격",
                    _ => "스케줄 없음"
                };
            }
        }

        /// <summary>
        /// 영어 요일을 한국어로 변환
        /// </summary>
        private string GetDayOfWeekKorean(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Sunday => "일요일",
                DayOfWeek.Monday => "월요일",
                DayOfWeek.Tuesday => "화요일",
                DayOfWeek.Wednesday => "수요일",
                DayOfWeek.Thursday => "목요일",
                DayOfWeek.Friday => "금요일",
                DayOfWeek.Saturday => "토요일",
                _ => "월요일"
            };
        }

        /// <summary>
        /// 다음 실행 시간을 계산합니다
        /// </summary>
        public void CalculateNextExecution()
        {
            if (!IsScheduleEnabled || ScheduleType == ScheduleType.None)
            {
                NextExecution = null;
                return;
            }

            var now = DateTime.Now;

            NextExecution = ScheduleType switch
            {
                ScheduleType.Once => ScheduledTime > now ? ScheduledTime : null,
                ScheduleType.Daily => GetNextDaily(now),
                ScheduleType.Weekly => GetNextWeekly(now),
                ScheduleType.Monthly => GetNextMonthly(now),
                ScheduleType.Interval => LastExecuted?.AddMinutes(IntervalMinutes) ?? now,
                _ => null
            };
        }

        private DateTime GetNextDaily(DateTime now)
        {
            if (!ScheduledTime.HasValue) return now.AddDays(1);
            
            var today = now.Date.Add(ScheduledTime.Value.TimeOfDay);
            return today > now ? today : today.AddDays(1);
        }

        private DateTime GetNextWeekly(DateTime now)
        {
            if (!ScheduledTime.HasValue) return now.AddDays(7);
            
            var targetTime = ScheduledTime.Value.TimeOfDay;
            var daysUntilTarget = ((int)SelectedDayOfWeek - (int)now.DayOfWeek + 7) % 7;
            
            if (daysUntilTarget == 0 && now.TimeOfDay > targetTime)
                daysUntilTarget = 7;
            
            return now.Date.AddDays(daysUntilTarget).Add(targetTime);
        }

        private DateTime GetNextMonthly(DateTime now)
        {
            if (!ScheduledTime.HasValue) return now.AddMonths(1);
            
            var targetTime = ScheduledTime.Value.TimeOfDay;
            DateTime targetDate;

            if (IsLastDayOfMonth)
            {
                // 말일 계산
                var lastDayThisMonth = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
                targetDate = lastDayThisMonth.Add(targetTime);
                
                if (targetDate <= now)
                {
                    var nextMonth = now.AddMonths(1);
                    var lastDayNextMonth = new DateTime(nextMonth.Year, nextMonth.Month, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
                    targetDate = lastDayNextMonth.Add(targetTime);
                }
            }
            else
            {
                // 특정 일자 계산
                var targetDay = Math.Min(SelectedDayOfMonth, DateTime.DaysInMonth(now.Year, now.Month));
                targetDate = new DateTime(now.Year, now.Month, targetDay).Add(targetTime);
                
                if (targetDate <= now)
                {
                    var nextMonth = now.AddMonths(1);
                    targetDay = Math.Min(SelectedDayOfMonth, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
                    targetDate = new DateTime(nextMonth.Year, nextMonth.Month, targetDay).Add(targetTime);
                }
            }
            
            return targetDate;
        }

        /// <summary>
        /// 현재 시간이 실행 시간인지 확인합니다
        /// </summary>
        public bool ShouldExecuteNow()
        {
            if (!IsScheduleEnabled || !NextExecution.HasValue)
                return false;

            return DateTime.Now >= NextExecution.Value;
        }

        public override string ToString()
        {
            return $"{ProgramName} - {DisplayName}";
        }
    }

    /// <summary>
    /// 프로그램 실행 인터페이스
    /// </summary>
    public interface IExecutableProgram
    {
        string Memo { get; }
        void Execute();
    }
}