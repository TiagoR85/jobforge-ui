using JobForge.Ui.Services;

namespace JobForge.Ui.Tests;

public sealed class OverviewMetricsTests
{
    private static readonly DateTime Now = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid JobId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static JobDto Job(bool enabled) => new(
        JobId, "job", "https://example.test/hook", null, "POST", null, 5, 30, enabled, null, null);

    private static ExecutionDto Execution(string status, int minuteOffset, int? durationSeconds = null)
    {
        var scheduled = Now.AddMinutes(-minuteOffset);
        return new ExecutionDto(Guid.NewGuid(), JobId, status, 1, 200, null, scheduled,
            durationSeconds is null ? null : scheduled,
            durationSeconds is null ? null : scheduled.AddSeconds(durationSeconds.Value),
            "trace");
    }

    [Fact]
    public void Empty_window_zeroes_everything()
    {
        var metrics = OverviewMetrics.From([Job(true)], [], 0, Now);

        Assert.Equal(1, metrics.ActiveJobs);
        Assert.Equal(0, metrics.SuccessRate);
        Assert.Null(metrics.AverageDuration);
        Assert.False(metrics.HasData);
        Assert.Equal(24, metrics.Hourly.Count);
    }

    [Fact]
    public void Success_rate_uses_completed_executions()
    {
        ExecutionDto[] window =
        [
            Execution("succeeded", 1), Execution("succeeded", 2), Execution("succeeded", 3),
            Execution("failed", 4), Execution("dead", 5)
        ];

        var metrics = OverviewMetrics.From([Job(true)], window, 5, Now);

        Assert.Equal(0.6, metrics.SuccessRate, 3);
        Assert.Equal(3, metrics.ByStatus.Succeeded);
        Assert.Equal(1, metrics.ByStatus.Failed);
        Assert.Equal(1, metrics.ByStatus.Dead);
        Assert.True(metrics.HasData);
    }

    [Fact]
    public void Average_duration_ignores_unfinished_executions()
    {
        ExecutionDto[] window =
        [
            Execution("succeeded", 1, 10),
            Execution("succeeded", 2, 20),
            Execution("running", 3)
        ];

        var metrics = OverviewMetrics.From([], window, 0, Now);

        Assert.Equal(TimeSpan.FromSeconds(15), metrics.AverageDuration);
    }

    [Fact]
    public void Hourly_buckets_cover_24_hours_ending_now()
    {
        var metrics = OverviewMetrics.From([], [Execution("succeeded", 0)], 0, Now);

        Assert.Equal(24, metrics.Hourly.Count);
        Assert.Equal("12:00", metrics.Hourly[^1].Label);
        Assert.Equal(1, metrics.Hourly[^1].Count);
        Assert.Equal(1, metrics.Hourly.Sum(b => b.Count));
    }

    [Fact]
    public void Dead_letters_uses_the_exact_total()
    {
        var metrics = OverviewMetrics.From([], [Execution("dead", 1)], 42, Now);
        Assert.Equal(42, metrics.DeadLetters);
    }

    [Fact]
    public async Task Loader_queries_jobs_window_and_dead_total()
    {
        var fake = new FakeJobForgeClient
        {
            Jobs = [Job(true)],
            Executions = new ExecutionPage(1, 100, 1, [Execution("succeeded", 1)]),
            ExecutionsByStatus = { ["dead"] = new ExecutionPage(1, 1, 7, []) }
        };

        var metrics = await OverviewData.LoadAsync(fake, TimeProvider.System, CancellationToken.None);

        Assert.Equal(1, metrics.ActiveJobs);
        Assert.Equal(7, metrics.DeadLetters);
        Assert.Equal(new[] { 100, 1 }, fake.RequestedSizes);
    }
}
