using JobForge.Ui.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace JobForge.Ui.Tests;

public sealed class OrchestratorHealthCheckTests : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();

    public void Dispose() => _server.Dispose();

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private static OrchestratorHealthCheck CheckFor(HttpClient client) =>
        new(new StubHttpClientFactory(client));

    [Fact]
    public async Task Healthy_when_orchestrator_live_returns_200()
    {
        _server.Given(Request.Create().WithPath("/health/live").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("Healthy"));

        var result = await CheckFor(new HttpClient { BaseAddress = new Uri(_server.Url!) })
            .CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("reachable", result.Description);
    }

    [Fact]
    public async Task Unhealthy_when_orchestrator_returns_503()
    {
        _server.Given(Request.Create().WithPath("/health/live").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(503).WithBody("Degraded"));

        var result = await CheckFor(new HttpClient { BaseAddress = new Uri(_server.Url!) })
            .CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("503", result.Description);
    }

    [Fact]
    public async Task Unhealthy_when_orchestrator_is_unreachable()
    {
        var client = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:1") };

        var result = await CheckFor(client).CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("unreachable", result.Description);
    }
}