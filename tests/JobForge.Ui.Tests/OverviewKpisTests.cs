using JobForge.Ui.Components.Pages;
using JobForge.Ui.Services;

namespace JobForge.Ui.Tests;

public sealed class OverviewKpisTests : UiTestContext
{
    private static OverviewMetrics Metrics(int jobs, double rate, int dead, TimeSpan? avg) => new(
        jobs + 1, jobs, rate, dead, avg,
        Enumerable.Range(0, 24).Select(i => new HourBucket($"{i:00}:00", 0)).ToList(),
        new StatusCounts(0, 0, 3, 1, 1));

    [Fact]
    public void Renders_kpis_with_culture_aware_percentage()
    {
        using var _ = new CultureScope("pt-BR");
        var cut = Render<OverviewKpis>(p => p.Add(x => x.Metrics, Metrics(4, 0.75, 7, TimeSpan.FromSeconds(2.4))));

        Assert.Equal("4", cut.Find("[data-testid=kpi-active-jobs]").TextContent.Trim());
        Assert.Contains("75", cut.Find("[data-testid=kpi-success-rate]").TextContent);
        Assert.Equal("7", cut.Find("[data-testid=kpi-dead-letters]").TextContent.Trim());
        Assert.Contains("2,4", cut.Find("[data-testid=kpi-avg-duration]").TextContent);
    }

    [Fact]
    public void Empty_window_shows_note_instead_of_dashboards()
    {
        var empty = new OverviewMetrics(0, 0, 0, 0, null,
            Enumerable.Range(0, 24).Select(i => new HourBucket($"{i:00}:00", 0)).ToList(),
            new StatusCounts(0, 0, 0, 0, 0));

        var cut = Render<OverviewKpis>(p => p.Add(x => x.Metrics, empty));

        Assert.NotNull(cut.Find("[data-testid=overview-empty]"));
        Assert.Equal("0", cut.Find("[data-testid=kpi-active-jobs]").TextContent.Trim());
    }

    [Fact]
    public void Average_duration_is_dash_when_unknown()
    {
        var cut = Render<OverviewKpis>(p => p.Add(x => x.Metrics, Metrics(1, 0, 0, null)));
        Assert.Equal("—", cut.Find("[data-testid=kpi-avg-duration]").TextContent.Trim());
    }
}
