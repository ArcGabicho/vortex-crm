using Bunit;
using Vortex.Domain.Comun;
using Vortex.Domain.Tareas;
using Vortex.Domain.Ventas;
using Vortex.Shared.Pages;
using Xunit;

namespace Vortex.Shared.Tests;

public class HomeTests : PruebaUi
{
    private IRepositorioTareas Tareas => Servicio<IRepositorioTareas>();

    [Fact]
    public void MuestraLosIndicadoresYLasTarjetasDelPanel()
    {
        var home = Render<Home>();

        Assert.Contains("Panel principal", home.Markup);
        foreach (var kpi in new[] { "En curso", "Ganado este mes", "Tasa de cierre", "Oportunidades abiertas", "Cotizaciones enviadas", "Tareas para hoy" })
        {
            Assert.NotNull(home.Find($"[data-kpi='{kpi}']"));
        }

        foreach (var tarjeta in new[] { "data-ventas-mensuales", "data-pipeline", "data-para-hoy", "data-cotizaciones-estado", "data-proximos-cierres", "data-cotizaciones-recientes" })
        {
            Assert.NotNull(home.Find($"[{tarjeta}]"));
        }
    }

    [Fact]
    public async Task ElPipelineYLosIndicadoresReflejanLasOportunidades()
    {
        var oportunidades = Servicio<IRepositorioOportunidades>();
        await oportunidades.GuardarAsync(new Oportunidad { Titulo = "Web", ContactoId = Guid.NewGuid(), Monto = 1500 }, Ct);
        var ganada = new Oportunidad { Titulo = "Logo", ContactoId = Guid.NewGuid(), Monto = 400 };
        ganada.CambiarEtapa(EtapaOportunidad.Ganado, DateTimeOffset.UtcNow);
        await oportunidades.GuardarAsync(ganada, Ct);

        var home = Render<Home>();

        Assert.Contains(1500m.ToString("C0", FormatoPeru.Cultura), home.Find("[data-kpi='En curso']").TextContent);
        Assert.Contains(400m.ToString("C0", FormatoPeru.Cultura), home.Find("[data-kpi='Ganado este mes']").TextContent);
        Assert.Contains("100", home.Find("[data-kpi='Tasa de cierre']").TextContent);
        Assert.Contains("1", home.Find("[data-etapa='Prospecto']").TextContent);
    }

    [Fact]
    public async Task ParaHoyMuestraLasDeHoyYLasVencidasPeroNoLasProximas()
    {
        var hoy = FormatoPeru.Hoy();
        await Tareas.GuardarAsync(new Tarea { Titulo = "Llamar a Rosa", Fecha = hoy }, Ct);
        await Tareas.GuardarAsync(new Tarea { Titulo = "Mandar catálogo", Fecha = hoy.AddDays(-2) }, Ct);
        await Tareas.GuardarAsync(new Tarea { Titulo = "Visitar la feria", Fecha = hoy.AddDays(3) }, Ct);

        var paraHoy = Render<Home>().Find("[data-para-hoy]");

        Assert.NotNull(paraHoy.QuerySelector("[data-tarea='Llamar a Rosa']"));
        Assert.NotNull(paraHoy.QuerySelector("[data-tarea='Mandar catálogo']"));
        Assert.Null(paraHoy.QuerySelector("[data-tarea='Visitar la feria']"));
    }

    [Fact]
    public async Task SinPendientesParaHoyAnunciaLaProxima()
    {
        await Tareas.GuardarAsync(new Tarea { Titulo = "Visitar la feria", Fecha = FormatoPeru.Hoy().AddDays(1) }, Ct);

        var home = Render<Home>();

        Assert.Contains("No tienes tareas pendientes para hoy. La próxima: Visitar la feria (mañana).", home.Find("[data-para-hoy]").TextContent);
    }
}
