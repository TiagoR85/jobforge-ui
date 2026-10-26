using JobForge.Ui.Components;
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
            Jobs = [new JobDto(JobId, "send-report", "https://example.test/hook", null,
                "GET", "*/5 * * * *", 3, 30, true,
                Guid.Parse("22222222-2222-2222-2222-222222222222"), "succeeded")]
        };
        Services.AddSingleton<IJobForgeClient>(fake);
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
        var row = cut.Find("[data-testid=job-row]");
        var badge = row.QuerySelector("[data-testid=status-badge]")!;
        Assert.Equal("succeeded", badge.GetAttribute("data-status"));
        Assert.NotEmpty(cut.FindAll("[data-testid=job-run-button]"));
        Assert.NotEmpty(cut.FindAll("[data-testid=job-disable-button]"));
        Assert.NotEmpty(cut.FindAll("[data-testid=new-job-link]"));
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
    public async Task Api_failure_shows_error_alert()
    {
        Register(new FakeJobForgeClient
        {
            JobsError = new JobForgeApiException(500, "orchestrator exploded")
        });
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
        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith($"/executions/{fake.RunResult.Id}", nav.Uri);
    }

    [Fact]
    public async Task Disable_calls_api_and_reloads_the_list()
    {
        var fake = Register();
        var cut = Render<Jobs>();
        await Task.Yield();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid=job-disable-button]")));

        cut.Find("[data-testid=job-disable-button]").Click();
        await Task.Yield();

        Assert.Equal(JobId, fake.LastDisableJobId);
        Assert.Equal(2, fake.GetJobsCalls);
    }
}