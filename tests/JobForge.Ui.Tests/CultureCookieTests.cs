using JobForge.Ui.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace JobForge.Ui.Tests;

public sealed class CultureCookieTests
{
    [Theory]
    [InlineData("pt-BR", true)]
    [InlineData("en", true)]
    [InlineData("fr", false)]
    [InlineData(null, false)]
    public void Only_supported_cultures_are_accepted(string? culture, bool expected)
        => Assert.Equal(expected, CultureCookie.IsSupported(culture));

    [Theory]
    [InlineData("/jobs", "/jobs")]
    [InlineData("/executions?status=dead", "/executions?status=dead")]
    [InlineData("https://evil.test", "/")]
    [InlineData("//evil.test", "/")]
    [InlineData("", "/")]
    [InlineData(null, "/")]
    public void Redirect_is_restricted_to_local_paths(string? input, string expected)
        => Assert.Equal(expected, CultureCookie.SafeRedirect(input));

    [Fact]
    public void Cookie_value_round_trips()
    {
        var value = CultureCookie.BuildValue("pt-BR");
        var parsed = CookieRequestCultureProvider.ParseCookieValue(value);

        Assert.NotNull(parsed);
        Assert.Equal("pt-BR", parsed!.Cultures.First().Value);
    }
}
