using JobForge.Ui.Components.Ui;
using JobForge.Ui.Services;
using Microsoft.Extensions.DependencyInjection;

namespace JobForge.Ui.Tests;

public sealed class ToastTests : UiTestContext
{
    [Fact]
    public void Show_success_renders_message()
    {
        var toasts = new ToastService();
        Services.AddSingleton(toasts);

        var cut = Render<ToastContainer>();
        toasts.ShowSuccess("Job created.");

        cut.WaitForAssertion(() => Assert.Contains("Job created.", cut.Markup));
    }

    [Fact]
    public void Service_raises_typed_event()
    {
        var toasts = new ToastService();
        string? message = null;
        string? type = null;
        toasts.OnShow += (m, t) => { message = m; type = t; };

        toasts.ShowError("boom");

        Assert.Equal("boom", message);
        Assert.Equal("error", type);
    }
}
