namespace JobForge.Ui.Services;

public sealed record StatusCounts(int Pending, int Running, int Succeeded, int Failed, int Dead);

public sealed record HourBucket(string Label, int Count);

public sealed record OverviewMetrics(
    int TotalJobs,
    int ActiveJobs,
    double SuccessRate,
    int DeadLetters,
    TimeSpan? AverageDuration,
    IReadOnlyList<HourBucket> Hourly,
    StatusCounts ByStatus)
{
    public bool HasData => ByStatus.Pending + ByStatus.Running + ByStatus.Succeeded
                           + ByStatus.Failed + ByStatus.Dead > 0;

    public static OverviewMetrics From(
        IReadOnlyList<JobDto> jobs,
        IReadOnlyList<ExecutionDto> window,
        int deadTotal,
        DateTime nowUtc)
    {
        var counts = new StatusCounts(
            window.Count(e => e.Status == "pending"),
            window.Count(e => e.Status == "running"),
            window.Count(e => e.Status == "succeeded"),
            window.Count(e => e.Status == "failed"),
            window.Count(e => e.Status == "dead"));

        var completed = counts.Succeeded + counts.Failed + counts.Dead;
        var successRate = completed == 0 ? 0 : (double)counts.Succeeded / completed;

        var durations = window
            .Where(e => e.StartedAt is not null && e.FinishedAt is not null)
            .Select(e => e.FinishedAt!.Value - e.StartedAt!.Value)
            .ToList();
        var average = durations.Count == 0
            ? (TimeSpan?)null
            : TimeSpan.FromTicks((long)durations.Average(d => d.Ticks));

        var end = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, nowUtc.Hour, 0, 0, DateTimeKind.Utc);
        var start = end.AddHours(-23);
        var hourly = Enumerable.Range(0, 24)
            .Select(i =>
            {
                var hour = start.AddHours(i);
                var count = window.Count(e =>
                {
                    var scheduled = e.ScheduledAt.Kind == DateTimeKind.Utc
                        ? e.ScheduledAt
                        : e.ScheduledAt.ToUniversalTime();
                    return scheduled >= hour && scheduled < hour.AddHours(1);
                });
                return new HourBucket(hour.ToString("HH:00"), count);
            })
            .ToList();

        return new OverviewMetrics(
            jobs.Count,
            jobs.Count(j => j.Enabled),
            successRate,
            deadTotal,
            average,
            hourly,
            counts);
    }
}
