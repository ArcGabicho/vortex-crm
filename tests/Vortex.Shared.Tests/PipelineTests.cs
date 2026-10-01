using Bunit;
using Vortex.Domain.Comun;
using Vortex.Domain.Contactos;
using Vortex.Domain.Ventas;
using Vortex.Shared.Pages.Ventas;
using Xunit;

namespace Vortex.Shared.Tests;

public class PipelineTests : PruebaUi
{
    private IRepositorioContactos Contactos => Servicio<IRepositorioContactos>();

    private IRepositorioOportunidades Oportunidades => Servicio<IRepositorioOportunidades>();

    private async Task<Oportunidad> CrearAsync(string titulo, EtapaOportunidad etapa, decimal monto, Guid clienteId)
    {
        var oportunidad = new Oportunidad { Titulo = titulo, Monto = monto, ContactoId = clienteId };
        oportunidad.CambiarEtapa(etapa, DateTimeOffset.UtcNow);
        await Oportunidades.GuardarAsync(oportunidad, Ct);
        return oportunidad;
    }

    [Fact]
    public async Task CadaOportunidadApareceEnSuColumnaConLosTotales()
    {
        var rosa = new Contacto { Nombres = "Rosa", Apellidos = "Quispe" };
        await Contactos.GuardarContactoAsync(rosa, Ct);
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
        Popovers.WaitForAssertion(() => Assert.Contains("Mover a Ganado", Popovers.Markup));
        Popovers.FindAll(".mud-menu-item").First(i => i.TextContent.Contains("Mover a Ganado")).Click();

        pagina.WaitForAssertion(() =>
            Assert.Contains("50 polos bordados", pagina.Find("[data-etapa=Ganado]").TextContent));
        Assert.DoesNotContain("50 polos bordados", pagina.Find("[data-etapa=Negociacion]").TextContent);
        Assert.Equal(EtapaOportunidad.Ganado, (await Oportunidades.ObtenerAsync(oportunidad.Id, Ct))!.Etapa);
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
