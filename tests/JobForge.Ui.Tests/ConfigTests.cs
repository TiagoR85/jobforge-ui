using System.Text.Json;

namespace JobForge.Ui.Tests;

public sealed class ConfigTests
{
    private static JsonElement LoadAppSettings(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "JobForge.Ui", fileName);
            if (File.Exists(candidate))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(candidate));
                return doc.RootElement.Clone();
            }
            dir = dir.Parent;
        }
        throw new FileNotFoundException(fileName + " not found walking up from " + AppContext.BaseDirectory);
    }

    [Fact]
    public void Base_url_points_at_orchestrator_rest_port()
    {
        var baseUrl = LoadAppSettings("appsettings.json")
            .GetProperty("JobForge").GetProperty("BaseUrl").GetString();

        Assert.Equal("http://localhost:8094", baseUrl);
    }

    [Fact]
    public void Client_is_the_confidential_console_client_with_both_scopes()
    {
        var jobforge = LoadAppSettings("appsettings.json").GetProperty("JobForge");

        Assert.Equal("jobforge-console", jobforge.GetProperty("ClientId").GetString());
        Assert.Equal("jobs:read jobs:write", jobforge.GetProperty("Scope").GetString());
    }

    [Fact]
    public void Committed_appsettings_contains_no_secret()
    {
        var secret = LoadAppSettings("appsettings.json")
            .GetProperty("JobForge").GetProperty("ClientSecret").GetString();

        Assert.True(string.IsNullOrWhiteSpace(secret),
            "appsettings.json must keep ClientSecret empty; the real value comes from OIDC_CLIENT_SECRET.");
    }

    [Fact]
    public void Development_secret_is_provided_locally()
    {
        var secret = LoadAppSettings("appsettings.Development.json")
            .GetProperty("JobForge").GetProperty("ClientSecret").GetString();

        Assert.Equal("jobforge-dev-secret-change-me", secret);
    }

    [Fact]
    public void Auto_refresh_defaults_to_five_seconds()
    {
        Assert.Equal(5, LoadAppSettings("appsettings.json")
            .GetProperty("Ui").GetProperty("RefreshSeconds").GetInt32());
    }

    [Fact]
    public void Nlog_defaults_service_name_to_jobforge_ui()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "JobForge.Ui", "nlog.config");
            if (File.Exists(candidate))
            {
                var xml = File.ReadAllText(candidate);
                Assert.Contains("default=jobforge-ui", xml);
                Assert.Contains("JsonLayout", xml);
                return;
            }
            dir = dir.Parent;
        }
        throw new FileNotFoundException("src/JobForge.Ui/nlog.config not found walking up from " + AppContext.BaseDirectory);
    }
}
