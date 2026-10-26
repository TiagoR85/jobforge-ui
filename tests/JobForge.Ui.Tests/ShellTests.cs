using JobForge.Ui.Components.Shell;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace JobForge.Ui.Tests;

public sealed class ShellTests : UiTestContext
{
    [Theory]
    [InlineData("/", "/", true)]
    [InlineData("/jobs", "/", false)]
    [InlineData("/jobs/abc", "/jobs", true)]
    [InlineData("/executions", "/executions", true)]
    [InlineData("/executions?status=dead", "/executions", true)]
    public void Nav_route_matches_exact_root_and_prefix(string path, string href, bool expected)
        => Assert.Equal(expected, NavRoute.IsActive(path, href));

    [Fact]
    public void Sidebar_renders_all_navigation_items()
    {
        var cut = Render<Sidebar>(p => p.Add(x => x.IsCollapsed, false));

        Assert.Equal(6, cut.FindAll("[data-testid^=nav-]").Count);
        Assert.NotEmpty(cut.FindAll("[data-testid=nav-overview]"));
        Assert.NotEmpty(cut.FindAll("[data-testid=nav-jobs]"));
        Assert.NotEmpty(cut.FindAll("[data-testid=nav-new_job]"));
        Assert.NotEmpty(cut.FindAll("[data-testid=nav-executions]"));
        Assert.NotEmpty(cut.FindAll("[data-testid=nav-dead_letters]"));
        Assert.NotEmpty(cut.FindAll("[data-testid=nav-operations]"));
    }

    [Fact]
    public void Sidebar_marks_current_route_as_active()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/jobs");
        var cut = Render<Sidebar>(p => p.Add(x => x.IsCollapsed, false));

        Assert.Contains("bg-[var(--sidebar-accent)]", cut.Find("[data-testid=nav-jobs]").ClassList);
    }

    [Fact]
    public void Header_has_language_switcher_and_actions()
    {
        var cut = Render<Header>();

        Assert.NotEmpty(cut.FindAll("[data-testid=culture-select]"));
        Assert.NotEmpty(cut.FindAll("[data-testid=new-job-link]"));
        Assert.NotEmpty(cut.FindAll("[data-testid=theme-toggle]"));
        Assert.NotEmpty(cut.FindAll("[data-testid=command-palette-button]"));
    }

    [Fact]
    public void Main_layout_renders_shell_and_body()
    {
        Services.AddSingleton(new JobForge.Ui.Services.ToastService());

        var cut = Render<JobForge.Ui.Components.Layout.MainLayout>(p => p
            .Add(x => x.Body, builder => builder.AddContent(0, "page-content")));

        Assert.NotEmpty(cut.FindAll("aside"));
        Assert.NotEmpty(cut.FindAll("header"));
        Assert.Contains("page-content", cut.Markup);
    }
}
