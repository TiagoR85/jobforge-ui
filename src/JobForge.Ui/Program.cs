using JobForge.Ui.Components;
using JobForge.Ui.Configuration;
using JobForge.Ui.Health;
using JobForge.Ui.Services;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.UseNLog();

RequiredConfig.Ensure(builder.Configuration, "JobForge:BaseUrl", "JobForge:ClientSecret");

builder.Services.AddSingleton(
    builder.Configuration.GetSection("JobForge").Get<JobForgeOptions>()
        ?? throw new InvalidOperationException("Missing required configuration: JobForge. See .env.example."));
builder.Services.Configure<UiOptions>(builder.Configuration.GetSection("Ui"));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpClient<JobForgeTokenProvider>((sp, client) =>
{
    var options = sp.GetRequiredService<JobForgeOptions>();
    client.BaseAddress = new Uri(options.TokenUri, UriKind.Absolute);
});

builder.Services.AddHttpClient("JobForgeApi", (sp, client) =>
{
    var options = sp.GetRequiredService<JobForgeOptions>();
    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
});
builder.Services.AddSingleton<IJobForgeClient>(sp => new JobForgeClient(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("JobForgeApi"),
    sp.GetRequiredService<JobForgeTokenProvider>()));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHealthChecks()
    .AddCheck("self", static () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<OrchestratorHealthCheck>("orchestrator", tags: ["ready"]);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHealthChecks("/health/live",
    new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = static r => r.Tags.Contains("live")
    });
app.MapHealthChecks("/health/ready",
    new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = static r => r.Tags.Contains("ready"),
        ResponseWriter = async (ctx, report) =>
        {
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.ToDictionary(
                    e => e.Key,
                    e => new { status = e.Value.Status.ToString(), error = e.Value.Description })
            }));
        }
    });

app.Run();