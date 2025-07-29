using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace scheduleProgram.Services
{
    /// <summary>
    /// 프로그램 스케줄 실행을 관리하는 서비스
    /// </summary>
    public class SchedulerService
    {
        private readonly ProgramManager _programManager;
        private readonly System.Timers.Timer _timer;
        private List<ProgramInfo> _programs;

        public event EventHandler<ProgramExecutedEventArgs>? ProgramExecuted;
        public event EventHandler<string>? StatusUpdated;

        public bool IsRunning { get; private set; }

        public SchedulerService(ProgramManager programManager)
        {
            _programManager = programManager;
            _programs = new List<ProgramInfo>();
            
            // 1분마다 스케줄 확인
            _timer = new System.Timers.Timer(60000); // 60초
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
        }

        /// <summary>
        /// 스케줄러 시작
        /// </summary>
        public void Start()
        {
            if (!IsRunning)
            {
                RefreshPrograms();
                _timer.Start();
                IsRunning = true;
                OnStatusUpdated("스케줄러가 시작되었습니다.");
            }
        }

        /// <summary>
        /// 스케줄러 중지
        /// </summary>
        public void Stop()
        {
            if (IsRunning)
            {
                _timer.Stop();
                IsRunning = false;
                OnStatusUpdated("스케줄러가 중지되었습니다.");
            }
        }

        /// <summary>
        /// 프로그램 목록 새로고침
        /// </summary>
        public void RefreshPrograms()
        {
            _programs = _programManager.ScanAllPrograms();
            OnStatusUpdated($"프로그램 {_programs.Count}개가 로드되었습니다. 스케줄 활성화: {_programs.Count(p => p.IsScheduleEnabled)}개");
        }

        /// <summary>
        /// 타이머 이벤트 - 스케줄된 프로그램 확인 및 실행
        /// </summary>
        private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                var programsToExecute = _programManager.GetProgramsToExecute();
                
                foreach (var program in programsToExecute)
                {
                    ExecuteScheduledProgram(program);
                }

                if (programsToExecute.Any())
                {
                    OnStatusUpdated($"{programsToExecute.Count}개 프로그램이 스케줄에 따라 실행되었습니다.");
                }
            }
            catch (Exception ex)
            {
                OnStatusUpdated($"스케줄 실행 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 스케줄된 프로그램 실행
        /// </summary>
        private void ExecuteScheduledProgram(ProgramInfo program)
        {
            try
            {
                // UI 스레드에서 조용한 실행 (메시지박스 없음)
                if (Application.OpenForms.Count > 0)
                {
                    var mainForm = Application.OpenForms[0];
                    mainForm.Invoke(() =>
                    {
                        _programManager.ExecuteProgramSilently(program); // 조용한 실행
                        OnProgramExecuted(new ProgramExecutedEventArgs(program, DateTime.Now, true));
                    });
                }
                else
                {
                    _programManager.ExecuteProgramSilently(program); // 조용한 실행
                    OnProgramExecuted(new ProgramExecutedEventArgs(program, DateTime.Now, true));
                }
            }
            catch (Exception ex)
            {
                OnProgramExecuted(new ProgramExecutedEventArgs(program, DateTime.Now, false, ex.Message));
            }
        }

        /// <summary>
        /// 다음 실행 예정 프로그램들을 반환
        /// </summary>
        public List<(ProgramInfo Program, DateTime? NextExecution)> GetUpcomingExecutions(int hours = 24)
        {
            var upcoming = new List<(ProgramInfo, DateTime?)>();
            var cutoffTime = DateTime.Now.AddHours(hours);

            foreach (var program in _programs.Where(p => p.IsScheduleEnabled && p.NextExecution.HasValue))
            {
                if (program.NextExecution <= cutoffTime)
                {
                    upcoming.Add((program, program.NextExecution));
                }
            }

            return upcoming.OrderBy(x => x.Item2).ToList();
        }

        /// <summary>
        /// 특정 프로그램의 스케줄 설정 업데이트
        /// </summary>
        public void UpdateProgramSchedule(ProgramInfo program, ScheduleType scheduleType, DateTime? scheduledTime = null, int intervalMinutes = 60, bool isEnabled = true, DayOfWeek? dayOfWeek = null, int? dayOfMonth = null, bool? isLastDayOfMonth = null)
        {
            program.ScheduleType = scheduleType;
            program.ScheduledTime = scheduledTime;
            program.IntervalMinutes = intervalMinutes;
            program.IsScheduleEnabled = isEnabled;
            
            // 매주 스케줄 설정
            if (dayOfWeek.HasValue)
            {
                program.SelectedDayOfWeek = dayOfWeek.Value;
            }
            
            // 매월 스케줄 설정
            if (dayOfMonth.HasValue)
            {
                program.SelectedDayOfMonth = dayOfMonth.Value;
            }
            
            if (isLastDayOfMonth.HasValue)
            {
                program.IsLastDayOfMonth = isLastDayOfMonth.Value;
            }
            
            program.CalculateNextExecution();
            
            _programManager.SaveScheduleConfig();
            OnStatusUpdated($"{program.DisplayName}의 스케줄이 업데이트되었습니다.");
        }

        protected virtual void OnProgramExecuted(ProgramExecutedEventArgs e)
        {
            ProgramExecuted?.Invoke(this, e);
        }

        protected virtual void OnStatusUpdated(string message)
        {
            StatusUpdated?.Invoke(this, message);
        }

        public void Dispose()
        {
            Stop();
            _timer?.Dispose();
        }
    }

    /// <summary>
    /// 프로그램 실행 이벤트 인수
    /// </summary>
    public class ProgramExecutedEventArgs : EventArgs
    {
        public ProgramInfo Program { get; }
        public DateTime ExecutionTime { get; }
        public bool Success { get; }
        public string? ErrorMessage { get; }

        public ProgramExecutedEventArgs(ProgramInfo program, DateTime executionTime, bool success, string? errorMessage = null)
        {
            Program = program;
            ExecutionTime = executionTime;
            Success = success;
            ErrorMessage = errorMessage;
        }
    }
}