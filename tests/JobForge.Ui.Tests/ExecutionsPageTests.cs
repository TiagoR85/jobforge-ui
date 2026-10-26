using JobForge.Ui.Components;
using JobForge.Ui.Components.Pages;
using JobForge.Ui.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JobForge.Ui.Tests;

public sealed class ExecutionsPageTests : UiTestContext
{
    private FakeJobForgeClient Register(FakeJobForgeClient? fake = null, double refreshSeconds = 5)
    {
        fake ??= new FakeJobForgeClient();
        Services.AddSingleton<IJobForgeClient>(fake);
        Services.AddSingleton(Options.Create(new UiOptions { RefreshSeconds = refreshSeconds }));
        Services.AddSingleton(TimeProvider.System);
        return fake;
    }

    [Fact]
    public async Task Renders_rows_and_page_info()
    {
        var fake = Register(new FakeJobForgeClient
        {
            Executions = new ExecutionPage(1, 20, 45,
            [
                new ExecutionDto(Guid.NewGuid(), Guid.NewGuid(), "succeeded", 1, 200, null,
                    new DateTime(2026, 10, 14, 10, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 10, 14, 10, 0, 1, DateTimeKind.Utc),
                    new DateTime(2026, 10, 14, 10, 0, 2, DateTimeKind.Utc), "tr-1"),
                new ExecutionDto(Guid.NewGuid(), Guid.NewGuid(), "failed", 3, 502, "bad gateway",
                    new DateTime(2026, 10, 14, 11, 0, 0, DateTimeKind.Utc), null, null, "tr-2")
            ])
        });

        var cut = Render<Executions>();
        await Task.Yield();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[data-testid=execution-row]").Count));

        Assert.Contains("Page 1 of 3", cut.Find("[data-testid=page-info]").TextContent);
        Assert.Contains("45 total", cut.Find("[data-testid=page-info]").TextContent);
        Assert.Contains(cut.FindAll("[data-testid=execution-row]"), row =>
            row.TextContent.Contains("bad gateway") || row.TextContent.Contains("failed"));
    }

    [Fact]
    public async Task Empty_state_renders()
    {
        Register();
        var cut = Render<Executions>();
        await Task.Yield();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-testid=executions-empty]")));
    }

    [Fact]
    public async Task Filter_change_syncs_status_query_string()
    {
        var fake = Register();
        var cut = Render<Executions>();
        await Task.Yield();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-testid=status-filter]")));

        cut.Find("[data-testid=status-filter]").Change("dead");

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.Contains("/executions?status=dead", nav.Uri);
        Assert.Contains("status=dead", nav.Uri);
        Assert.Equal(1, fake.GetExecutionsCalls);
    }

    [Fact]
    public async Task Status_from_query_string_is_passed_to_the_api()
    {
        var fake = Register();
        Services.GetRequiredService<NavigationManager>().NavigateTo("/executions?status=dead");

        var cut = Render<Executions>();
        await Task.Yield();
        cut.WaitForAssertion(() => Assert.Equal("dead", fake.LastStatus));

        Assert.Equal(1, fake.LastPage);
        Assert.Equal(20, fake.LastSize);
    }

    [Fact]
    public async Task Next_and_prev_change_page_and_prev_starts_disabled()
    {
        var fake = Register(new FakeJobForgeClient
        {
            Executions = new ExecutionPage(1, 20, 45,
            [
                new ExecutionDto(Guid.NewGuid(), Guid.NewGuid(), "succeeded", 1, 200, null,
                    new DateTime(2026, 10, 14, 10, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 10, 14, 10, 0, 1, DateTimeKind.Utc),
                    new DateTime(2026, 10, 14, 10, 0, 2, DateTimeKind.Utc), "tr-1")
            ])
        });
        var cut = Render<Executions>();
        await Task.Yield();
        await Task.Delay(50);
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-testid=page-info]")));

        Assert.True(cut.Find("[data-testid=pagination-prev]").HasAttribute("disabled"));

        cut.Find("[data-testid=pagination-next]").Click();
        await Task.Yield();
        Assert.Equal(2, fake.LastPage);

        cut.Find("[data-testid=pagination-prev]").Click();
        await Task.Yield();
        Assert.Equal(1, fake.LastPage);
    }

    [Fact]
    public async Task Next_is_disabled_on_the_last_page()
    {
        Register(new FakeJobForgeClient
        {
            Executions = new ExecutionPage(1, 20, 20,
            [
                new ExecutionDto(Guid.NewGuid(), Guid.NewGuid(), "succeeded", 1, 200, null,
                    new DateTime(2026, 10, 14, 10, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 10, 14, 10, 0, 1, DateTimeKind.Utc),
                    new DateTime(2026, 10, 14, 10, 0, 2, DateTimeKind.Utc), "tr-1")
            ])
        });
        var cut = Render<Executions>();
        await Task.Yield();
        await Task.Delay(50);
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-testid=page-info]")));

        Assert.True(cut.Find("[data-testid=pagination-next]").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Auto_refresh_polls_the_api()
    {
        var fake = Register(refreshSeconds: 0.1);
        var cut = Render<Executions>();
        await Task.Yield();
        await Task.Delay(300);

        Assert.True(fake.GetExecutionsCalls >= 2,
            $"expected a refresh poll, saw {fake.GetExecutionsCalls} calls");
    }

    [Fact]
    public async Task Rows_expose_relative_time_and_link_to_detail()
    {
        var fake = new FakeJobForgeClient
        {
            Executions = new ExecutionPage(1, 20, 1,
            [
                new ExecutionDto(Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    "succeeded", 1, 200, null,
                    DateTime.UtcNow.AddMinutes(-2), null, null, "trace")
            ])
        };
        Services.AddSingleton<IJobForgeClient>(fake);

        var cut = Render<Executions>();
        await Task.Yield();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid=execution-row]")));
        Assert.NotEmpty(cut.FindAll("[data-testid=execution-age]"));
        var link = cut.Find("[data-testid=execution-link]");
        Assert.Contains("/executions/33333333", link.GetAttribute("href"));
    }

    [Fact]
    public async Task Api_failure_shows_error_alert()
    {
        Register(new FakeJobForgeClient
        {
            ExecutionsError = new JobForgeApiException(500, "storage down")
        });
        var cut = Render<Executions>();
        await Task.Yield();
        cut.WaitForAssertion(() => Assert.Contains("storage down", cut.Markup));

        Assert.NotNull(cut.Find("[data-testid=error-alert]"));
    }
}