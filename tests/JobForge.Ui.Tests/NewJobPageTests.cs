using JobForge.Ui.Components.Pages;
using JobForge.Ui.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace JobForge.Ui.Tests;

public sealed class NewJobPageTests : UiTestContext
{
    private FakeJobForgeClient Register(FakeJobForgeClient? fake = null)
    {
        fake ??= new FakeJobForgeClient();
        Services.AddSingleton<IJobForgeClient>(fake);
        Services.AddSingleton(new ToastService());
        return fake;
    }

    private static void FillValidForm(IRenderedComponent<NewJob> cut)
    {
        cut.Find("[data-testid=job-name-input]").Change("demo");
        cut.Find("[data-testid=job-url-input]").Change("https://example.test/hook");
    }

    [Fact]
    public void Cron_preview_returns_next_three_occurrences_utc()
    {
        var next = CronPreview.Next("0 9 * * *", new DateTime(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc), 3);

        Assert.Equal(3, next.Count);
        Assert.Equal(new DateTime(2026, 10, 26, 9, 0, 0, DateTimeKind.Utc), next[0]);
        Assert.Equal(new DateTime(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc), next[1]);
        Assert.Equal(new DateTime(2026, 10, 28, 9, 0, 0, DateTimeKind.Utc), next[2]);
    }

    [Fact]
    public void Cron_preview_returns_empty_for_invalid_expression()
        => Assert.Empty(CronPreview.Next("not a cron", DateTime.UtcNow, 3));

    [Fact]
    public void Renders_three_preview_rows_for_daily_cron()
    {
        Register();
        var cut = Render<NewJob>();

        cut.Find("[data-testid=job-cron-input]").Change("0 9 * * *");

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("[data-testid=cron-preview]").Count));
    }

    [Fact]
    public void Preset_fills_the_cron_input()
    {
        Register();
        var cut = Render<NewJob>();

        cut.FindAll("[data-testid=preset-cron]")[0].Click();

        Assert.Equal("*/5 * * * *", cut.Find("[data-testid=job-cron-input]").GetAttribute("value"));
    }

    [Fact]
    public async Task Empty_submit_shows_field_errors_without_calling_api()
    {
        var fake = Register();
        var cut = Render<NewJob>();
        await Task.Yield();

        cut.Find("[data-testid=submit-button]").Click();
        await Task.Yield();

        Assert.NotNull(cut.Find("[data-testid=name-error]"));
        Assert.NotNull(cut.Find("[data-testid=url-error]"));
        Assert.Null(fake.LastCreateRequest);
    }

    [Fact]
    public async Task Invalid_cron_and_payload_are_rejected_client_side()
    {
        var fake = Register();
        var cut = Render<NewJob>();
        await Task.Yield();
        FillValidForm(cut);

        cut.Find("[data-testid=job-cron-input]").Change("not-a-cron");
        cut.Find("[data-testid=job-payload-input]").Change("{oops");
        cut.Find("[data-testid=submit-button]").Click();
        await Task.Yield();

        Assert.NotNull(cut.Find("[data-testid=cron-error]"));
        Assert.NotNull(cut.Find("[data-testid=payload-error]"));
        Assert.Null(fake.LastCreateRequest);
    }

    [Fact]
    public async Task Valid_submit_creates_job_with_fresh_idempotency_key_and_navigates()
    {
        var fake = Register();
        var cut = Render<NewJob>();
        await Task.Yield();
        FillValidForm(cut);
        cut.Find("[data-testid=job-cron-input]").Change("*/5 * * * *");

        cut.Find("[data-testid=submit-button]").Click();
        await Task.Yield();

        Assert.NotNull(fake.LastCreateRequest);
        Assert.Equal("demo", fake.LastCreateRequest!.Name);
        Assert.Equal("https://example.test/hook", fake.LastCreateRequest.WebhookUrl);
        Assert.Equal("*/5 * * * *", fake.LastCreateRequest.Cron);
        Assert.Matches("^[0-9a-f]{32}$", fake.LastIdempotencyKey);
        Assert.EndsWith("/jobs", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public async Task Api_failure_shows_form_error_and_keeps_form()
    {
        var fake = Register();
        fake.CreateError = new JobForgeApiException(400, "cron invalid");
        var cut = Render<NewJob>();
        await Task.Yield();
        FillValidForm(cut);

        cut.Find("[data-testid=submit-button]").Click();
        await Task.Yield();

        Assert.Contains("cron invalid", cut.Find("[data-testid=form-error]").TextContent);
        Assert.Empty(cut.FindAll("[data-testid=name-error]"));
    }

    [Fact]
    public async Task Submit_button_is_disabled_while_creating()
    {
        var fake = Register();
        fake.CreateGate = new TaskCompletionSource();
        var cut = Render<NewJob>();
        await Task.Yield();
        FillValidForm(cut);

        cut.Find("[data-testid=submit-button]").Click();
        await Task.Yield();

        Assert.True(cut.Find("[data-testid=submit-button]").HasAttribute("disabled"));

        fake.CreateGate.SetResult();
        await Task.Yield();
        Assert.NotNull(fake.LastCreateRequest);
    }
}
