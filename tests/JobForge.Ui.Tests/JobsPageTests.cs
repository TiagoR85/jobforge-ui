using JobForge.Ui.Components.Pages;
using JobForge.Ui.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace JobForge.Ui.Tests;

public sealed class JobsPageTests : UiTestContext
{
    private static readonly Guid JobId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private FakeJobForgeClient Register(FakeJobForgeClient? fake = null)
    {
        fake ??= new FakeJobForgeClient
        {
            Jobs =
            [
                new JobDto(JobId, "send-report", "https://example.test/hook", null,
                    "GET", "*/5 * * * *", 3, 30, true,
                    Guid.Parse("22222222-2222-2222-2222-222222222222"), "succeeded")
            ]
        };
        Services.AddSingleton<IJobForgeClient>(fake);
        Services.AddSingleton(new ToastService());
        return fake;
    }

    [Fact]
    public async Task Renders_job_row_with_last_status_badge()
    {
        Register();
        var cut = Render<Jobs>();
        await Task.Yield();
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid=job-row]")));

        Assert.Contains("send-report", cut.Markup);
        Assert.Contains("*/5 * * * *", cut.Markup);
        var badge = cut.Find("[data-testid=job-row]").QuerySelector("[data-testid=status-badge]")!;
        Assert.Equal("succeeded", badge.GetAttribute("data-status"));
        Assert.NotEmpty(cut.FindAll("[data-testid=job-run-button]"));
        Assert.NotEmpty(cut.FindAll("[data-testid=job-disable-button]"));
    }

    [Fact]
    public async Task Empty_list_shows_empty_state()
    {
        Register(new FakeJobForgeClient { Jobs = [] });
        var cut = Render<Jobs>();
        await Task.Yield();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-testid=jobs-empty]")));
    }

    [Fact]
    public async Task Search_filters_by_name()
    {
        Register(new FakeJobForgeClient
        {
            Jobs =
            [
                new JobDto(JobId, "send-report", "https://a.test", null, "GET", null, 5, 30, true, null, null),
                new JobDto(Guid.NewGuid(), "cleanup", "https://b.test", null, "GET", null, 5, 30, true, null, null)
            ]
        });
        var cut = Render<Jobs>();
        await Task.Yield();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[data-testid=job-row]").Count));

        cut.Find("[data-testid=jobs-search]").Input("clean");

        Assert.Single(cut.FindAll("[data-testid=job-row]"));
        Assert.Contains("cleanup", cut.Markup);
    }

    [Fact]
    public async Task Api_failure_shows_error_alert()
    {
        Register(new FakeJobForgeClient { JobsError = new JobForgeApiException(500, "orchestrator exploded") });
        var cut = Render<Jobs>();
        await Task.Yield();
        cut.WaitForAssertion(() => Assert.Contains("orchestrator exploded", cut.Markup));

        Assert.NotNull(cut.Find("[data-testid=error-alert]"));
    }

    [Fact]
    public async Task Run_now_navigates_to_execution_detail()
    {
        var fake = Register();
        var cut = Render<Jobs>();
        await Task.Yield();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid=job-run-button]")));

        cut.Find("[data-testid=job-run-button]").Click();
        await Task.Yield();

        Assert.Equal(JobId, fake.LastRunJobId);
        Assert.EndsWith($"/executions/{fake.RunResult.Id}",
            Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public async Task Disable_requires_confirmation_then_reloads()
    {
        var fake = Register();
        var cut = Render<Jobs>();
        await Task.Yield();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid=job-disable-button]")));

        cut.Find("[data-testid=job-disable-button]").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid=confirm-disable]")));

        cut.Find("[data-testid=confirm-disable]").Click();
        await Task.Yield();

        Assert.Equal(JobId, fake.LastDisableJobId);
        Assert.Equal(2, fake.GetJobsCalls);
    }
}
