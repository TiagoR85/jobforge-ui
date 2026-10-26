using System.Collections;
using System.Globalization;
using System.Resources;
using JobForge.Ui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace JobForge.Ui.Tests;

public sealed class LocalizationTests
{
    private static IStringLocalizer<SharedResources> Localizer()
        => new ServiceCollection()
            .AddLogging()
            .AddLocalization()
            .BuildServiceProvider()
            .GetRequiredService<IStringLocalizer<SharedResources>>();

    [Fact]
    public void Resolves_portuguese_and_english()
    {
        var localizer = Localizer();

        using (new CultureScope("pt-BR"))
        {
            Assert.Equal("Visão geral", localizer["nav_overview"].Value);
        }
        using (new CultureScope("en"))
        {
            Assert.Equal("Overview", localizer["nav_overview"].Value);
        }
    }

    [Fact]
    public void Every_english_key_has_a_portuguese_translation()
    {
        var assembly = typeof(SharedResources).Assembly;
        var neutral = new ResourceManager("JobForge.Ui.SharedResources", assembly)
            .GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true)!;
        var pt = new ResourceManager("JobForge.Ui.SharedResources", assembly)
            .GetResourceSet(new CultureInfo("pt-BR"), createIfNotExists: true, tryParents: true)!;

        foreach (DictionaryEntry entry in neutral)
        {
            Assert.False(string.IsNullOrWhiteSpace(pt.GetString((string)entry.Key)),
                $"missing pt-BR translation for {entry.Key}");
        }
    }
}
