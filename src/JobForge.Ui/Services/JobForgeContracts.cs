namespace JobForge.Ui.Services;

public sealed record CreateJobRequest(
    string Name,
    string WebhookUrl,
    string? Payload,
    string? Method,
    string? Cron,
    int? MaxRetries,
    int? TimeoutSeconds);

public sealed record JobDto(
    Guid Id, string Name, string WebhookUrl, string? Payload, string Method,
    string? Cron, int MaxRetries, int TimeoutSeconds, bool Enabled,
    Guid? ExecutionId, string? ExecutionStatus);

public sealed record ExecutionDto(
    Guid Id, Guid JobId, string Status, int Attempt, int? HttpStatusCode, string? Error,
    DateTime ScheduledAt, DateTime? StartedAt, DateTime? FinishedAt, string TraceId);

public sealed record ExecutionPage(int Page, int Size, int Total, IReadOnlyList<ExecutionDto> Items);
