using System.Net;
using JobForge.Ui.Services;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace JobForge.Ui.Tests;

public sealed class JobForgeClientTests : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();

    public void Dispose() => _server.Dispose();

    private void StubToken() => _server
        .Given(Request.Create().WithPath("/connect/token").UsingPost())
        .RespondWith(Response.Create().WithStatusCode(200)
            .WithBody("""{"access_token":"test-token","expires_in":3600}""")
            .WithHeader("Content-Type", "application/json"));

    private JobForgeClient Client()
    {
        var options = new JobForgeOptions
        {
            BaseUrl = _server.Url!,
            ClientId = "jobforge-console",
            ClientSecret = "secret",
            Scope = "jobs:read jobs:write"
        };
        var tokens = new JobForgeTokenProvider(
            new HttpClient { BaseAddress = new Uri(_server.Url!) }, options, TimeProvider.System);
        return new JobForgeClient(new HttpClient { BaseAddress = new Uri(_server.Url!) }, tokens);
    }

    [Fact]
    public async Task GetJobs_sends_bearer_and_parses_camel_case()
    {
        StubToken();
        _server.Given(Request.Create().WithPath("/v1/jobs").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new[]
            {
                new
                {
                    id = "11111111-1111-1111-1111-111111111111", name = "demo",
                    webhookUrl = "http://example.test/hook", payload = (string?)null,
                    method = "GET", cron = (string?)null, maxRetries = 3, timeoutSeconds = 30,
                    enabled = true, executionId = (Guid?)null, executionStatus = (string?)null
                }
            }));

        var jobs = await Client().GetJobsAsync();

        var job = Assert.Single(jobs);
        Assert.Equal("demo", job.Name);
        Assert.Equal("GET", job.Method);
        Assert.True(job.Enabled);
        var entry = _server.LogEntries.Single(e => e.RequestMessage!.Path == "/v1/jobs");
        Assert.Equal("Bearer test-token", entry.RequestMessage!.Headers!["Authorization"]?.First());
    }

    [Fact]
    public async Task CreateJob_sends_idempotency_key_and_parses_created_job()
    {
        StubToken();
        _server.Given(Request.Create().WithPath("/v1/jobs").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(201).WithBodyAsJson(new
            {
                id = "22222222-2222-2222-2222-222222222222", name = "demo",
                webhookUrl = "http://example.test/hook", payload = (string?)null,
                method = "GET", cron = (string?)null, maxRetries = 3, timeoutSeconds = 30,
                enabled = true, executionId = (Guid?)null, executionStatus = (string?)null
            }));

        var job = await Client().CreateJobAsync(
            new CreateJobRequest("demo", "http://example.test/hook", null, null, null, null, null),
            "k-1234567890abcdef");

        Assert.Equal("demo", job.Name);
        var entry = _server.LogEntries.Single(e => e.RequestMessage!.Path == "/v1/jobs");
        Assert.Equal("k-1234567890abcdef", entry.RequestMessage!.Headers!["Idempotency-Key"]?.First());
        Assert.Contains("demo", entry.RequestMessage!.Body);
        Assert.Contains("example.test", entry.RequestMessage!.Body);
    }

    [Fact]
    public async Task CreateJob_replayed_200_returns_job_without_error()
    {
        StubToken();
        _server.Given(Request.Create().WithPath("/v1/jobs").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                id = "22222222-2222-2222-2222-222222222222", name = "demo",
                webhookUrl = "http://example.test/hook", payload = (string?)null,
                method = "GET", cron = (string?)null, maxRetries = 3, timeoutSeconds = 30,
                enabled = true, executionId = (Guid?)null, executionStatus = (string?)null
            }));

        var job = await Client().CreateJobAsync(
            new CreateJobRequest("demo", "http://example.test/hook", null, null, null, null, null), "replay");

        Assert.Equal("22222222-2222-2222-2222-222222222222", job.Id.ToString());
    }

    [Fact]
    public async Task CreateJob_400_throws_with_server_error_message()
    {
        StubToken();
        _server.Given(Request.Create().WithPath("/v1/jobs").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(400)
                .WithBodyAsJson(new { error = "Idempotency-Key header is required" }));

        var ex = await Assert.ThrowsAsync<JobForgeApiException>(() => Client().CreateJobAsync(
            new CreateJobRequest("demo", "http://example.test", null, null, null, null, null), "k"));

        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("Idempotency-Key", ex.Message);
    }

    [Fact]
    public async Task RunJob_returns_accepted_execution()
    {
        StubToken();
        var jobId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        _server.Given(Request.Create().WithPath($"/v1/jobs/{jobId}/run").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(202).WithBodyAsJson(new
            {
                id = "44444444-4444-4444-4444-444444444444", jobId, status = "pending",
                attempt = 0, httpStatusCode = (int?)null, error = (string?)null,
                scheduledAt = "2026-10-14T10:00:00Z", startedAt = (string?)null,
                finishedAt = (string?)null, traceId = "tr-1"
            }));

        var execution = await Client().RunJobAsync(jobId);

        Assert.Equal("pending", execution.Status);
        Assert.Equal(jobId, execution.JobId);
    }

    [Fact]
    public async Task DisableJob_204_succeeds_and_404_throws()
    {
        StubToken();
        var gone = Guid.Parse("55555555-5555-5555-5555-555555555555");
        _server.Given(Request.Create().WithPath($"/v1/jobs/{gone}").UsingDelete())
            .RespondWith(Response.Create().WithStatusCode(204));

        await Client().DisableJobAsync(gone);

        var ex = await Assert.ThrowsAsync<JobForgeApiException>(
            () => Client().DisableJobAsync(Guid.NewGuid()));
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task GetExecutions_sends_status_and_paging_query()
    {
        StubToken();
        _server.Given(Request.Create().WithPath("/v1/executions").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                page = 2, size = 20, total = 45, items = Array.Empty<object>()
            }));

        var page = await Client().GetExecutionsAsync("dead", 2, 20);

        Assert.Equal(2, page.Page);
        Assert.Equal(45, page.Total);
        Assert.Empty(page.Items);
        var entry = _server.LogEntries.Single(e => e.RequestMessage!.Path == "/v1/executions");
        Assert.Contains("status=dead", entry.RequestMessage!.Url);
        Assert.Contains("page=2", entry.RequestMessage!.Url);
        Assert.Contains("size=20", entry.RequestMessage!.Url);
    }

    [Fact]
    public async Task GetExecution_returns_null_on_404()
    {
        StubToken();

        var result = await Client().GetExecutionAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetExecution_parses_single_execution()
    {
        StubToken();
        var id = Guid.Parse("66666666-6666-6666-6666-666666666666");
        _server.Given(Request.Create().WithPath($"/v1/executions/{id}").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new
            {
                id, jobId = "33333333-3333-3333-3333-333333333333", status = "failed",
                attempt = 3, httpStatusCode = 502, error = "bad gateway",
                scheduledAt = "2026-10-14T10:00:00Z", startedAt = "2026-10-14T10:00:01Z",
                finishedAt = "2026-10-14T10:00:04Z", traceId = "tr-2"
            }));

        var execution = await Client().GetExecutionAsync(id);

        Assert.NotNull(execution);
        Assert.Equal("failed", execution.Status);
        Assert.Equal(3, execution.Attempt);
        Assert.Equal(502, execution.HttpStatusCode);
        Assert.Equal("bad gateway", execution.Error);
    }
}
