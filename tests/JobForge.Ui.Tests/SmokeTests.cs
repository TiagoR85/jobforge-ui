using JobForge.Ui.Components;
using JobForge.Ui.Components.Pages;
using JobForge.Ui.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JobForge.Ui.Tests;

public sealed class SmokeTests : BunitContext
{
    [Fact]
    public void App_renders()
    {
        Services.AddSingleton<IJobForgeClient>(new FakeJobForgeClient());
        var cut = Render<Routes>();
        Assert.Contains("jobforge", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Error_page_renders()
    {
        var cut = Render<Error>();
        Assert.Contains("An error occurred", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NotFound_page_renders()
    {
        var cut = Render<NotFound>();
        Assert.Contains("does not exist", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Nav_menu_links_to_the_family_pages()
    {
        Services.AddSingleton<IJobForgeClient>(new FakeJobForgeClient());
        var cut = Render<Routes>();
        var hrefs = cut.FindAll("a").Select(a => a.GetAttribute("href")).ToList();

        Assert.Contains("", hrefs);
        Assert.Contains("jobs/new", hrefs);
        Assert.Contains("executions", hrefs);
        Assert.Contains("executions?status=dead", hrefs);
    }
}
