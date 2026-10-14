using System.Net;
using System.Net.Http.Json;
using JobForge.Ui.Services;

namespace JobForge.Ui.Tests;

public sealed class JobForgeTokenProviderTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public string? LastBody { get; private set; }
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public string AccessToken { get; set; } = "tok-1";
        public int ExpiresIn { get; set; } = 3600;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            var response = new HttpResponseMessage(StatusCode);
            if (StatusCode == HttpStatusCode.OK)
            {
                response.Content = JsonContent.Create(new { access_token = AccessToken, expires_in = ExpiresIn });
            }
            return response;
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    private static JobForgeOptions Options => new()
    {
        BaseUrl = "http://localhost:8094",
        ClientId = "jobforge-console",
        ClientSecret = "top-secret",
        Scope = "jobs:read jobs:write"
    };

    private static (JobForgeTokenProvider Provider, StubHandler Handler) Create(TimeProvider? time = null)
    {
        var handler = new StubHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://oidc.test") };
        return (new JobForgeTokenProvider(http, Options, time ?? TimeProvider.System), handler);
    }

    [Fact]
    public async Task Posts_client_credentials_grant_and_returns_token()
    {
        var (provider, handler) = Create();

        var token = await provider.GetTokenAsync();

        Assert.Equal("tok-1", token);
        Assert.Contains("grant_type=client_credentials", handler.LastBody);
        Assert.Contains("client_id=jobforge-console", handler.LastBody);
        Assert.Contains("client_secret=top-secret", handler.LastBody);
        Assert.Contains("scope=jobs", handler.LastBody);
    }

    [Fact]
    public async Task Second_call_is_served_from_cache()
    {
        var (provider, handler) = Create();

        await provider.GetTokenAsync();
        var second = await provider.GetTokenAsync();

        Assert.Equal("tok-1", second);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Token_is_refreshed_after_expiry_window()
    {
        var clock = new FixedTimeProvider();
        var (provider, handler) = Create(clock);

        await provider.GetTokenAsync();
        clock.UtcNow = clock.UtcNow.AddSeconds(3540);      // 3600 - 60 fresh window elapsed
        await provider.GetTokenAsync();

        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task Failure_throws_api_exception_with_status_code()
    {
        var (provider, handler) = Create();
        handler.StatusCode = HttpStatusCode.Unauthorized;

        var ex = await Assert.ThrowsAsync<JobForgeApiException>(() => provider.GetTokenAsync());

        Assert.Equal(401, ex.StatusCode);
        Assert.Contains("401", ex.Message);
    }
}
