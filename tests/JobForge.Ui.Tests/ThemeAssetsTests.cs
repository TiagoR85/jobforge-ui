namespace JobForge.Ui.Tests;

public sealed class ThemeAssetsTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "JobForge.Ui.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("repo root not found");
    }

    [Fact]
    public void Generated_tailwind_css_contains_design_tokens()
    {
        var path = Path.Combine(RepoRoot(), "src", "JobForge.Ui", "wwwroot", "css", "tailwind.css");
        Assert.True(File.Exists(path), $"missing {path}; run npm run build:css");

        var css = File.ReadAllText(path);
        Assert.True(css.Length > 1000, "tailwind output looks empty");
        Assert.Contains("--tw-", css);
    }

    [Fact]
    public void Theme_tokens_define_light_and_dark_surfaces()
    {
        var path = Path.Combine(RepoRoot(), "src", "JobForge.Ui", "wwwroot", "css", "theme.css");
        Assert.True(File.Exists(path));

        var css = File.ReadAllText(path);
        Assert.Contains("--primary", css);
        Assert.Contains("--sidebar", css);
        Assert.Contains(".dark", css);
    }

    [Fact]
    public void Theme_js_exposes_the_functions_the_shell_uses()
    {
        var path = Path.Combine(RepoRoot(), "src", "JobForge.Ui", "wwwroot", "js", "theme.js");
        Assert.True(File.Exists(path));

        var js = File.ReadAllText(path);
        foreach (var fn in new[]
                 {
                     "function getTheme", "function toggleTheme", "function getAccent", "function setAccent",
                     "function getSidebarCollapsed", "function setSidebarCollapsed", "function getIsMobile",
                     "function addResizeListener", "function removeResizeListener", "function addKeydownListener"
                 })
        {
            Assert.Contains(fn, js);
        }
    }
}
