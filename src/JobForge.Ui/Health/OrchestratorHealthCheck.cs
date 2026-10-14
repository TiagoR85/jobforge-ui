using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JobForge.Ui.Health;

public sealed class OrchestratorHealthCheck(IHttpClientFactory factory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = factory.CreateClient("JobForgeApi");
            using var response = await client.GetAsync("health/live", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("orchestrator is reachable")
                : HealthCheckResult.Unhealthy($"orchestrator returned {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("orchestrator is unreachable", ex);
        }
    }
}