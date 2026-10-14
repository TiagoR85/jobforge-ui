using System.Net.Http.Json;

namespace JobForge.Ui.Services;

public sealed class JobForgeClient(HttpClient http, JobForgeTokenProvider token) : IJobForgeClient
{
    public async Task<IReadOnlyList<JobDto>> GetJobsAsync(CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Get, "v1/jobs", null, null, ct);
        return await response.Content.ReadFromJsonAsync<List<JobDto>>(ct) ?? [];
    }

    public async Task<JobDto> CreateJobAsync(CreateJobRequest request, string idempotencyKey, CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Post, "v1/jobs", JsonContent.Create(request), idempotencyKey, ct);
        return await response.Content.ReadFromJsonAsync<JobDto>(ct)
            ?? throw new JobForgeApiException((int)response.StatusCode, "empty create response");
    }

    public async Task<ExecutionDto> RunJobAsync(Guid jobId, CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Post, $"v1/jobs/{jobId}/run", null, null, ct);
        return await response.Content.ReadFromJsonAsync<ExecutionDto>(ct)
            ?? throw new JobForgeApiException((int)response.StatusCode, "empty run response");
    }

    public async Task DisableJobAsync(Guid jobId, CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Delete, $"v1/jobs/{jobId}", null, null, ct);
    }

    public async Task<ExecutionPage> GetExecutionsAsync(string? status, int page, int size, CancellationToken ct = default)
    {
        var query = new List<string> { $"page={page}", $"size={size}" };
        if (!string.IsNullOrWhiteSpace(status))
        {
            query.Insert(0, $"status={Uri.EscapeDataString(status)}");
        }
        using var response = await SendAsync(HttpMethod.Get, "v1/executions?" + string.Join("&", query), null, null, ct);
        return await response.Content.ReadFromJsonAsync<ExecutionPage>(ct)
            ?? throw new JobForgeApiException((int)response.StatusCode, "empty executions response");
    }

    public async Task<ExecutionDto?> GetExecutionAsync(Guid id, CancellationToken ct = default)
    {
        try
        {
            using var response = await SendAsync(HttpMethod.Get, $"v1/executions/{id}", null, null, ct);
            return await response.Content.ReadFromJsonAsync<ExecutionDto>(ct)
                ?? throw new JobForgeApiException((int)response.StatusCode, "empty execution response");
        }
        catch (JobForgeApiException ex) when (ex.StatusCode == 404)
        {
            return null;
        }
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string path, HttpContent? content, string? idempotencyKey, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", await token.GetTokenAsync(ct));
        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        var response = await http.SendAsync(request, ct);
        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var statusCode = (int)response.StatusCode;
        var message = await ReadErrorAsync(response, statusCode, ct);
        response.Dispose();
        throw new JobForgeApiException(statusCode, message);
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, int statusCode, CancellationToken ct)
    {
        try
        {
            var payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(ct);
            if (!string.IsNullOrWhiteSpace(payload?.Error))
            {
                return payload.Error;
            }
        }
        catch (System.Text.Json.JsonException)
        {
        }
        return $"HTTP {statusCode}";
    }

    private sealed record ErrorPayload(string? Error);
}
