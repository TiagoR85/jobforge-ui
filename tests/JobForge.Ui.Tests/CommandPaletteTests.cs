using JobForge.Ui.Components.Ui;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace JobForge.Ui.Tests;

public sealed class CommandPaletteTests : UiTestContext
{
    [Fact]
    public async Task Open_shows_pages_and_actions()
    {
        var cut = Render<CommandPalette>();
        await cut.InvokeAsync(() => cut.Instance.OpenAsync());

        Assert.NotNull(cut.Find("[data-testid=command-palette]"));
        Assert.True(cut.FindAll("[data-testid=command-item]").Count >= 8);
    }

    [Fact]
    public async Task Search_filters_items()
    {
        var cut = Render<CommandPalette>();
        await cut.InvokeAsync(() => cut.Instance.OpenAsync());

        cut.Find("[data-testid=command-input]").Input("operations");

        var items = cut.FindAll("[data-testid=command-item]");
        Assert.Single(items);
        Assert.Contains("operations", items[0].GetAttribute("data-href"));
    }

    [Fact]
    public async Task Selecting_navigates()
    {
        var cut = Render<CommandPalette>();
        await cut.InvokeAsync(() => cut.Instance.OpenAsync());

        cut.Find("[data-testid=command-input]").Input("jobs");
        cut.Find("[data-testid=command-item]").Click();

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/jobs", nav.Uri);
    }
}
