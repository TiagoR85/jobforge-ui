namespace JobForge.Ui.Services;

public sealed class JobForgeOptions
{
    public string BaseUrl { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string Scope { get; set; } = "";

    public string TokenUri => new Uri(new Uri(BaseUrl), "/connect/token").ToString();
}
