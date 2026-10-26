namespace JobForge.Ui.Components.Shell;

/// <summary>"/" matches only the root; every other href matches by prefix.</summary>
public static class NavRoute
{
    public static bool IsActive(string currentPath, string href)
        => href == "/"
            ? string.Equals(currentPath, "/", StringComparison.OrdinalIgnoreCase)
            : currentPath.StartsWith(href, StringComparison.OrdinalIgnoreCase);
}
