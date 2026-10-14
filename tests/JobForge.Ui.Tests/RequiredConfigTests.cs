using JobForge.Ui.Configuration;
using Microsoft.Extensions.Configuration;

namespace JobForge.Ui.Tests;

public sealed class RequiredConfigTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(
            values.ToDictionary(v => v.Key, v => v.Value)).Build();

    [Fact]
    public void Missing_key_throws_with_actionable_message()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => RequiredConfig.Ensure(Config(), "JobForge:BaseUrl"));

        Assert.Contains("Missing required configuration: JobForge:BaseUrl", ex.Message);
        Assert.Contains("See .env.example.", ex.Message);
    }

    [Fact]
    public void Whitespace_value_counts_as_missing()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => RequiredConfig.Ensure(Config(("JobForge:ClientSecret", "   ")), "JobForge:ClientSecret"));

        Assert.Contains("JobForge:ClientSecret", ex.Message);
    }

    [Fact]
    public void Present_keys_do_not_throw()
    {
        RequiredConfig.Ensure(Config(
            ("JobForge:BaseUrl", "http://localhost:8094"),
            ("JobForge:ClientSecret", "s")), "JobForge:BaseUrl", "JobForge:ClientSecret");
    }
}
