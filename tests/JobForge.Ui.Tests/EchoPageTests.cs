using JobForge.Ui.Components.Pages;
using JobForge.Ui.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JobForge.Ui.Tests;

public sealed class EchoPageTests : UiTestContext
{
    [Fact]
    public void Shows_the_payload_and_user_agent_of_received_requests()
    {
        var store = new EchoStore();
        store.Add(new EchoEntry(DateTimeOffset.UtcNow, "application/json", "jobforge-runner", "{\"hello\":\"world\"}"));
        Services.AddSingleton(store);

        var cut = Render<Echo>();

        var entry = cut.Find("[data-testid=echo-entry]");
        Assert.Contains("{\"hello\":\"world\"}", entry.TextContent);
        Assert.Contains("jobforge-runner", entry.TextContent);
    }

    [Fact]
    public void Shows_an_empty_state_when_nothing_arrived()
    {
        Services.AddSingleton(new EchoStore());

        var cut = Render<Echo>();

        Assert.NotEmpty(cut.FindAll("[data-testid=echo-empty]"));
        Assert.Empty(cut.FindAll("[data-testid=echo-entry]"));
    }
}
