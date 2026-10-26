using JobForge.Ui.Components.Pages;
using JobForge.Ui.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace JobForge.Ui.Tests;

public sealed class OperationsPageTests : UiTestContext
{
    private void Register(HealthStatus status, string secret = "super-secret")
    {
        Services.AddHealthChecks()
            .AddCheck("orchestrator", () => new HealthCheckResult(status, "checked"));
        Services.AddSingleton(Options.Create(new JobForgeOptions
        {
            BaseUrl = "http://localhost:8094/",
            ClientId = "jobforge-console",
            ClientSecret = secret,
            Scope = "jobs:read jobs:write"
        }));
        Services.AddSingleton(Options.Create(new UiOptions { RefreshSeconds = 5 }));
    }

    [Fact]
    public void Healthy_orchestrator_is_shown_with_config()
    {
        Register(HealthStatus.Healthy);
        var cut = Render<Operations>();

        cut.WaitForAssertion(() => Assert.Contains("Healthy", cut.Find("[data-testid=ops-status]").TextContent));
        Assert.Contains("http://localhost:8094", cut.Find("[data-testid=ops-base-url]").TextContent);
        Assert.Contains("http://localhost:8094/metrics", cut.Find("[data-testid=ops-metrics-link]").GetAttribute("href"));
    }

    [Fact]
    public void Unhealthy_orchestrator_is_reported()
    {
        Register(HealthStatus.Unhealthy);
        var cut = Render<Operations>();

        cut.WaitForAssertion(() => Assert.Contains("Unhealthy", cut.Find("[data-testid=ops-status]").TextContent));
    }

    [Fact]
    public void Never_renders_the_client_secret()
    {
        Register(HealthStatus.Healthy);
        var cut = Render<Operations>();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid=ops-base-url]")));
        Assert.DoesNotContain("super-secret", cut.Markup);
    }
}
