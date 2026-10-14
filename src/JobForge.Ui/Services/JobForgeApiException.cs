namespace JobForge.Ui.Services;

public sealed class JobForgeApiException(int statusCode, string? message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
