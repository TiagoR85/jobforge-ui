using JobForge.Ui.Components.Pages;
using JobForge.Ui.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JobForge.Ui.Tests;

public sealed class OverviewPageTests : UiTestContext
{
    [Fact]
    public void Empty_window_renders_without_charts()
    {
        var fake = new FakeJobForgeClient
        {
            Jobs = [],
            Executions = new ExecutionPage(1, 100, 0, []),
            ExecutionsByStatus = { ["dead"] = new ExecutionPage(1, 1, 0, []) }
        };
        Services.AddSingleton<IJobForgeClient>(fake);

        var cut = Render<Overview>();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-testid=overview-empty]")));
        Assert.Equal("0", cut.Find("[data-testid=kpi-active-jobs]").TextContent.Trim());
    }
}
