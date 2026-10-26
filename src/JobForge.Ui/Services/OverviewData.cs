namespace JobForge.Ui.Services;

public static class OverviewData
{
    public const int WindowSize = 100;

    public static async Task<OverviewMetrics> LoadAsync(
        IJobForgeClient client,
        TimeProvider clock,
        CancellationToken ct = default)
    {
        var jobs = await client.GetJobsAsync(ct);
        var window = await client.GetExecutionsAsync(null, 1, WindowSize, ct);
        var dead = await client.GetExecutionsAsync("dead", 1, 1, ct);

        return OverviewMetrics.From(jobs, window.Items, dead.Total, clock.GetUtcNow().UtcDateTime);
    }
}
