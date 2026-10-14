using JobForge.Ui.Components;

namespace JobForge.Ui.Tests;

public sealed class StatusBadgeTests : BunitContext
{
    [Theory]
    [InlineData("succeeded", "text-bg-success")]
    [InlineData("failed", "text-bg-danger")]
    [InlineData("dead", "text-bg-dark")]
    [InlineData("running", "text-bg-primary")]
    [InlineData("pending", "text-bg-secondary")]
    [InlineData("unexpected", "text-bg-secondary")]
    public void Maps_status_to_bootstrap_color(string status, string cssClass)
    {
        var cut = Render<StatusBadge>(p => p.Add(b => b.Status, status));

        var badge = cut.Find("[data-testid=status-badge]");
        Assert.Contains(cssClass, badge.ClassName);
        Assert.Equal(status, badge.GetAttribute("data-status"));
        Assert.Contains(status, badge.TextContent);
    }
}