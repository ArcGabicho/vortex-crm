using Bunit;
using MudBlazor.Services;
using Vortex.Shared.Pages;
using Xunit;

namespace Vortex.Shared.Tests;

public class HomeTests : BunitContext
{
    public HomeTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void MuestraBienvenidaYModulosDeLaFase1()
    {
        var home = Render<Home>();

        Assert.Contains("Bienvenido a Vortex CRM", home.Markup);
        foreach (var modulo in new[] { "Contactos", "Pipeline", "Cotizaciones", "Tareas" })
        {
            Assert.Contains(modulo, home.Markup);
        }
    }
}
