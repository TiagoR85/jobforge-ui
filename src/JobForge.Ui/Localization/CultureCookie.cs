using Microsoft.AspNetCore.Localization;

namespace JobForge.Ui.Localization;

public static class CultureCookie
{
    public static string CookieName => CookieRequestCultureProvider.DefaultCookieName;

    public static bool IsSupported(string? culture)
        => culture is "pt-BR" or "en";

    public static string BuildValue(string culture)
        => CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));

    /// <summary>Only same-site absolute paths; blocks open redirects (//host).</summary>
    public static string SafeRedirect(string? redirect)
        => !string.IsNullOrWhiteSpace(redirect)
           && redirect.StartsWith('/')
           && !redirect.StartsWith("//")
            ? redirect
            : "/";

    public static void Set(HttpResponse response, string culture, bool secure)
        => response.Cookies.Append(CookieName, BuildValue(culture), new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = secure
        });
}
