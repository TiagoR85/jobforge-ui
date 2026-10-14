using JobForge.Ui.Services;

namespace JobForge.Ui.Tests;

public sealed class FakeJobForgeClient : IJobForgeClient
{
    public List<JobDto> Jobs { get; set; } = [];
    public Exception? JobsError { get; set; }
    public int GetJobsCalls { get; private set; }

    public ExecutionDto RunResult { get; set; } = new(
        Guid.Parse("77777777-7777-7777-7777-777777777777"),
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        "pending", 0, null, null, DateTime.UtcNow, null, null, "trace-1");
    public Exception? RunError { get; set; }
    public Guid? LastRunJobId { get; private set; }

    public Exception? DisableError { get; set; }
    public Guid? LastDisableJobId { get; private set; }

    public JobDto CreateResult { get; set; } = new(
        Guid.Parse("88888888-8888-8888-8888-888888888888"),
        "created", "http://localhost", null, "GET", null, 3, 30, true, null, null);
    public Exception? CreateError { get; set; }
    public TaskCompletionSource? CreateGate { get; set; }
    public CreateJobRequest? LastCreateRequest { get; private set; }
    public string? LastIdempotencyKey { get; private set; }

    public ExecutionPage Executions { get; set; } = new(1, 20, 0, []);
    public Exception? ExecutionsError { get; set; }
    public int GetExecutionsCalls { get; private set; }
    public string? LastStatus { get; private set; }
    public int LastPage { get; private set; }
    public int LastSize { get; private set; }

    public ExecutionDto? Execution { get; set; }
    public Exception? ExecutionError { get; set; }
    public int GetExecutionCalls { get; private set; }

    public Task<IReadOnlyList<JobDto>> GetJobsAsync(CancellationToken ct = default)
    {
        GetJobsCalls++;
        if (JobsError is not null) return Task.FromException<IReadOnlyList<JobDto>>(JobsError);
        return Task.FromResult<IReadOnlyList<JobDto>>(Jobs);
    }

    public async Task<JobDto> CreateJobAsync(CreateJobRequest request, string idempotencyKey, CancellationToken ct = default)
    {
        LastCreateRequest = request;
        LastIdempotencyKey = idempotencyKey;
        if (CreateGate is not null) await CreateGate.Task;
        if (CreateError is not null) throw CreateError;
        return CreateResult;
    }

    public Task<ExecutionDto> RunJobAsync(Guid jobId, CancellationToken ct = default)
    {
        LastRunJobId = jobId;
        if (RunError is not null) return Task.FromException<ExecutionDto>(RunError);
        return Task.FromResult(RunResult);
    }

    public Task DisableJobAsync(Guid jobId, CancellationToken ct = default)
    {
        LastDisableJobId = jobId;
        if (DisableError is not null) return Task.FromException(DisableError);
        return Task.CompletedTask;
    }

    public Task<ExecutionPage> GetExecutionsAsync(string? status, int page, int size, CancellationToken ct = default)
    {
        GetExecutionsCalls++;
        LastStatus = status;
        LastPage = page;
        LastSize = size;
        if (ExecutionsError is not null) return Task.FromException<ExecutionPage>(ExecutionsError);
        return Task.FromResult(Executions);
    }

    public Task<ExecutionDto?> GetExecutionAsync(Guid id, CancellationToken ct = default)
    {
        GetExecutionCalls++;
        if (ExecutionError is not null) return Task.FromException<ExecutionDto?>(ExecutionError);
        return Task.FromResult(Execution);
    }
}