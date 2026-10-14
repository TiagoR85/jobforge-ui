namespace JobForge.Ui.Services;

public interface IJobForgeClient
{
    Task<IReadOnlyList<JobDto>> GetJobsAsync(CancellationToken ct = default);
    Task<JobDto> CreateJobAsync(CreateJobRequest request, string idempotencyKey, CancellationToken ct = default);
    Task<ExecutionDto> RunJobAsync(Guid jobId, CancellationToken ct = default);
    Task DisableJobAsync(Guid jobId, CancellationToken ct = default);
    Task<ExecutionPage> GetExecutionsAsync(string? status, int page, int size, CancellationToken ct = default);
    Task<ExecutionDto?> GetExecutionAsync(Guid id, CancellationToken ct = default);
}
