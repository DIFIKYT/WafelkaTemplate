namespace WafelkaTemplate
{
    public class DailyMidnightTaskScheduler
    {
        private Timer? _timer;

        public event Func<Task>? TaskExecuted;

        public DailyMidnightTaskScheduler()
        {
            SetTimer();
        }

        private void SetTimer(TimeSpan? interval = null)
        {
            TimeZoneInfo moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
            DateTime nowInMoscow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, moscowTimeZone);

            DateTime nextExecutionTime;

            if (interval != null)
            {
                nextExecutionTime = nowInMoscow.Add(interval.Value);
            }
            else
            {
                nextExecutionTime = new DateTime(nowInMoscow.Year, nowInMoscow.Month, nowInMoscow.Day, 0, 0, 0);

                if (nowInMoscow >= nextExecutionTime)
                {
                    nextExecutionTime = nextExecutionTime.AddDays(1);
                }
            }

            TimeSpan timeUntilNextExecution = nextExecutionTime - nowInMoscow;

            if (timeUntilNextExecution.TotalMilliseconds <= 0)
            {
                throw new InvalidOperationException("Time until next execution is non-positive. This should not happen.");
            }

            _timer = new Timer(
                ExecuteTask!,
                null,
                timeUntilNextExecution,
                Timeout.InfiniteTimeSpan
            );
        }

        private async void ExecuteTask(object state)
        {
            if (TaskExecuted != null)
            {
                await TaskExecuted();
            }

            SetTimer();
        }
    }
}