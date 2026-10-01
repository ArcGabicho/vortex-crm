using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Vortex.Domain.Comun;
using Vortex.Domain.Contactos;
using Vortex.Domain.Ventas;
using Vortex.Infrastructure.Contactos;
using Vortex.Infrastructure.Ventas;
using Vortex.Shared.Pages.Ventas;
using Xunit;

namespace Vortex.Shared.Tests;

public class PipelineTests : BunitContext
{
    private readonly RepositorioContactosEnMemoria contactos = new();
    private readonly RepositorioOportunidadesEnMemoria oportunidades = new();
    private readonly IRenderedComponent<MudPopoverProvider> popovers;
    private readonly CancellationToken ct = Xunit.TestContext.Current.CancellationToken;

    public PipelineTests()
    {
        Services.AddMudServices();
        Services.AddSingleton<IRepositorioContactos>(contactos);
        Services.AddSingleton<IRepositorioOportunidades>(oportunidades);
        JSInterop.Mode = JSRuntimeMode.Loose;
        popovers = Render<MudPopoverProvider>();
    }

    private async Task<Oportunidad> CrearAsync(string titulo, EtapaOportunidad etapa, decimal monto, Guid clienteId)
    {
        var oportunidad = new Oportunidad { Titulo = titulo, Monto = monto, ContactoId = clienteId };
        oportunidad.CambiarEtapa(etapa, DateTimeOffset.UtcNow);
        await oportunidades.GuardarAsync(oportunidad, ct);
        return oportunidad;
    }

    [Fact]
    public async Task CadaOportunidadApareceEnSuColumnaConLosTotales()
    {
        var rosa = new Contacto { Nombres = "Rosa", Apellidos = "Quispe" };
        await contactos.GuardarContactoAsync(rosa, ct);
        await CrearAsync("50 polos bordados", EtapaOportunidad.Prospecto, 1500, rosa.Id);
        await CrearAsync("Uniformes escolares", EtapaOportunidad.Prospecto, 2000, rosa.Id);
        await CrearAsync("Gorras", EtapaOportunidad.Ganado, 800, rosa.Id);

        var pagina = Render<Pipeline>();

        var prospecto = pagina.Find("[data-etapa=Prospecto]");
        Assert.Contains("50 polos bordados", prospecto.TextContent);
        Assert.Contains("Rosa Quispe", prospecto.TextContent);
        Assert.Contains("(2)", prospecto.TextContent);
        Assert.Contains(FormatoPeru.Soles(3500), prospecto.TextContent);
        Assert.Contains("Gorras", pagina.Find("[data-etapa=Ganado]").TextContent);

        Assert.Equal(FormatoPeru.Soles(3500), pagina.Find("[data-resumen=en-curso]").TextContent.Trim());
        Assert.Equal(FormatoPeru.Soles(800), pagina.Find("[data-resumen=ganado]").TextContent.Trim());
        Assert.Equal("100%", pagina.Find("[data-resumen=tasa]").TextContent.Trim());
    }

    [Fact]
    public async Task ElMenuMueveLaOportunidadDeColumnaYLaGuarda()
    {
        var oportunidad = await CrearAsync("50 polos bordados", EtapaOportunidad.Negociacion, 1500, Guid.NewGuid());
        var pagina = Render<Pipeline>();

        pagina.Find("[data-etapa=Negociacion] button[aria-label='Mover oportunidad']").Click();
        popovers.WaitForAssertion(() => Assert.Contains("Mover a Ganado", popovers.Markup));
        popovers.FindAll(".mud-menu-item").First(i => i.TextContent.Contains("Mover a Ganado")).Click();

        pagina.WaitForAssertion(() =>
            Assert.Contains("50 polos bordados", pagina.Find("[data-etapa=Ganado]").TextContent));
        Assert.DoesNotContain("50 polos bordados", pagina.Find("[data-etapa=Negociacion]").TextContent);
        Assert.Equal(EtapaOportunidad.Ganado, (await oportunidades.ObtenerAsync(oportunidad.Id, ct))!.Etapa);
        Assert.Equal(FormatoPeru.Soles(1500), pagina.Find("[data-resumen=ganado]").TextContent.Trim());
    }

    [Fact]
    public void SinOportunidadesMuestraUnMensajeParaEmpezar()
    {
        var pagina = Render<Pipeline>();

        Assert.Contains("Todavía no tienes oportunidades", pagina.Markup);
        Assert.Equal("—", pagina.Find("[data-resumen=tasa]").TextContent.Trim());
    }
}
