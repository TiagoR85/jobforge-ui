using JobForge.Ui.Components;
using JobForge.Ui.Configuration;
using JobForge.Ui.Services;

var builder = WebApplication.CreateBuilder(args);

RequiredConfig.Ensure(builder.Configuration, "JobForge:BaseUrl", "JobForge:ClientSecret");

builder.Services.AddSingleton(
    builder.Configuration.GetSection("JobForge").Get<JobForgeOptions>()
        ?? throw new InvalidOperationException("Missing required configuration: JobForge. See .env.example."));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
