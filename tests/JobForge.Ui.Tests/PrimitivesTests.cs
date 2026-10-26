using JobForge.Ui.Components;
using JobForge.Ui.Components.Ui;

namespace JobForge.Ui.Tests;

public sealed class PrimitivesTests : UiTestContext
{
    [Theory]
    [InlineData("succeeded", "success")]
    [InlineData("failed", "warning")]
    [InlineData("dead", "error")]
    [InlineData("running", "info")]
    [InlineData("pending", "neutral")]
    [InlineData("weird", "neutral")]
    public void Status_badge_maps_status_to_variant(string status, string variant)
    {
        var cut = Render<StatusBadge>(p => p.Add(x => x.Status, status));
        var badge = cut.Find("[data-testid=status-badge]");

        Assert.Equal(status, badge.GetAttribute("data-status"));
        Assert.Contains($"data-variant=\"{variant}\"", cut.Markup);
    }

    [Fact]
    public void Button_renders_variant_and_handles_click()
    {
        var clicked = 0;
        var cut = Render<Button>(p => p
            .Add(x => x.Variant, "secondary")
            .Add(x => x.OnClick, () => clicked++));

        cut.Find("button").Click();
        Assert.Equal(1, clicked);
        Assert.Contains("bg-[var(--secondary)]", cut.Markup);
    }

    [Fact]
    public void Modal_only_renders_when_open()
    {
        var closed = Render<Modal>(p => p.Add(x => x.IsOpen, false));
        Assert.Empty(closed.FindAll("[data-testid=modal]"));

        var open = Render<Modal>(p => p
            .Add(x => x.IsOpen, true)
            .Add(x => x.Title, "Disable job"));
        Assert.NotNull(open.Find("[data-testid=modal]"));
        Assert.Contains("Disable job", open.Markup);
    }
}
