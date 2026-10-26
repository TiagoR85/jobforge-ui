using System.Globalization;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace JobForge.Ui.Tests;

public abstract class UiTestContext : BunitContext
{
    protected UiTestContext()
    {
        // Deterministic UI language for bUnit; tests override with CultureScope("pt-BR").
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");

        Services.AddLocalization();
        Services.AddSingleton(TimeProvider.System);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}

public sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _ui = CultureInfo.CurrentUICulture;
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;

    public CultureScope(string name)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(name);
        CultureInfo.CurrentCulture = new CultureInfo(name);
    }

    public void Dispose()
    {
        CultureInfo.CurrentUICulture = _ui;
        CultureInfo.CurrentCulture = _culture;
    }
}
