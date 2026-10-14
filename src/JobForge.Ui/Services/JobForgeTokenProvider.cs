using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace JobForge.Ui.Services;

public sealed class JobForgeTokenProvider(HttpClient http, JobForgeOptions options, TimeProvider time)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public async Task<string> GetTokenAsync(CancellationToken ct = default)
    {
        if (IsFresh()) return _token!;

        await _gate.WaitAsync(ct);
        try
        {
            if (IsFresh()) return _token!;

            using var response = await http.PostAsync(options.TokenUri,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = options.ClientId,
                    ["client_secret"] = options.ClientSecret,
                    ["scope"] = options.Scope
                }), ct);

            if (!response.IsSuccessStatusCode)
            {
                throw new JobForgeApiException((int)response.StatusCode,
                    $"token endpoint returned {(int)response.StatusCode}");
            }

            var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(ct)
                ?? throw new JobForgeApiException((int)response.StatusCode, "empty token response");
            _token = payload.AccessToken;
            _expiresAt = time.GetUtcNow().AddSeconds(Math.Max(payload.ExpiresIn - 60, 30));
            return _token;
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool IsFresh() => _token is not null && time.GetUtcNow() < _expiresAt;

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
