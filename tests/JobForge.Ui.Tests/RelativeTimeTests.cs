using JobForge.Ui.Services;

namespace JobForge.Ui.Tests;

public sealed class RelativeTimeTests
{
    private static readonly DateTime Now = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0, "time_now", null)]
    [InlineData(10, "time_seconds_ago", 10)]
    [InlineData(120, "time_minutes_ago", 2)]
    [InlineData(7200, "time_hours_ago", 2)]
    [InlineData(172800, "time_days_ago", 2)]
    public void Formats_by_threshold(int secondsAgo, string key, int? value)
    {
        var result = RelativeTime.Format(Now.AddSeconds(-secondsAgo), Now);
        Assert.Equal(key, result.Key);
        Assert.Equal(value, result.Value);
    }
}
