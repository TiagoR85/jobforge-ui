using JobForge.Ui.Components;
using JobForge.Ui.Components.Pages;
using JobForge.Ui.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JobForge.Ui.Tests;

public sealed class ExecutionDetailPageTests : BunitContext
{
    private static readonly Guid ExecId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private FakeJobForgeClient Register(FakeJobForgeClient? fake = null)
    {
        fake ??= new FakeJobForgeClient
        {
            Execution = new ExecutionDto(
                ExecId, Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "failed", 3, 502, "bad gateway",
                new DateTime(2026, 10, 14, 10, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 10, 14, 10, 0, 1, DateTimeKind.Utc),
                new DateTime(2026, 10, 14, 10, 0, 4, DateTimeKind.Utc), "tr-9")
        };
        Services.AddSingleton<IJobForgeClient>(fake);
        return fake;
    }

    private IRenderedComponent<ExecutionDetail> RenderDetail() =>
        Render<ExecutionDetail>(p => p.Add(d => d.Id, ExecId));

    [Fact]
    public async Task Renders_lifecycle_timeline_with_attempt_http_duration_and_error()
    {
        Register();
        var cut = RenderDetail();
        await Task.Yield();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-testid=attempt]")));

        Assert.Equal("3", cut.Find("[data-testid=attempt]").TextContent.Trim());
        Assert.Equal("502", cut.Find("[data-testid=http-status]").TextContent.Trim());
        Assert.Equal("3.0 s", cut.Find("[data-testid=duration]").TextContent.Trim());
        Assert.Contains("bad gateway", cut.Find("[data-testid=error-text]").TextContent);

        var items = cut.FindAll("[data-testid=timeline-item]");
        Assert.Equal(3, items.Count);
        Assert.Contains("scheduled", items[0].TextContent);
        Assert.Contains("started", items[1].TextContent);
        Assert.Contains("finished", items[2].TextContent);

        var statusEl = cut.Find("[data-testid=detail-status]");
        var badge = statusEl.QuerySelector("[data-testid=status-badge]");
        Assert.Equal("failed", badge!.GetAttribute("data-status"));
    }

    [Fact]
    public async Task Missing_execution_renders_not_found_panel()
    {
        Register(new FakeJobForgeClient { Execution = null });
        var cut = RenderDetail();
        await Task.Yield();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-testid=execution-not-found]")));
    }

    [Fact]
    public async Task Server_error_renders_error_alert_not_not_found()
    {
        Register(new FakeJobForgeClient
        {
            ExecutionError = new JobForgeApiException(500, "storage down")
        });
        var cut = RenderDetail();
        await Task.Yield();
        cut.WaitForAssertion(() => Assert.Contains("storage down", cut.Markup));

        Assert.NotNull(cut.Find("[data-testid=error-alert]"));
        Assert.Empty(cut.FindAll("[data-testid=execution-not-found]"));
    }

    [Fact]
    public async Task Refresh_button_reloads_the_execution()
    {
        var fake = Register();
        var cut = RenderDetail();
        await Task.Yield();
        cut.WaitForAssertion(() => Assert.Equal(1, fake.GetExecutionCalls));

        cut.Find("[data-testid=refresh-button]").Click();
        await Task.Yield();

        Assert.Equal(2, fake.GetExecutionCalls);
    }

    [Fact]
    public async Task Pending_execution_shows_only_scheduled_timeline_row()
    {
        Register(new FakeJobForgeClient
        {
            Execution = new ExecutionDto(
                ExecId, Guid.NewGuid(), "pending", 0, null, null,
                new DateTime(2026, 10, 14, 10, 0, 0, DateTimeKind.Utc), null, null, "tr-p")
        });
        var cut = RenderDetail();
        await Task.Yield();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-testid=timeline-item]")));

        Assert.Single(cut.FindAll("[data-testid=timeline-item]"));
        Assert.Empty(cut.FindAll("[data-testid=duration]"));
        Assert.Empty(cut.FindAll("[data-testid=error-text]"));
    }
}