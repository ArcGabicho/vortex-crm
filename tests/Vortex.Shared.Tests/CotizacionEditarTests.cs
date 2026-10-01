using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components;
using Vortex.Domain.Comun;
using Vortex.Domain.Configuracion;
using Vortex.Domain.Contactos;
using Vortex.Domain.Ventas;
using Vortex.Shared.Pages.Ventas;
using Xunit;

namespace Vortex.Shared.Tests;

public class CotizacionEditarTests : PruebaUi
{
    private IRepositorioCotizaciones Cotizaciones => Servicio<IRepositorioCotizaciones>();

    private IRepositorioOportunidades Oportunidades => Servicio<IRepositorioOportunidades>();

    private async Task<(Empresa Empresa, Contacto Contacto, Oportunidad Oportunidad)> PrepararAsync(
        RegimenTributario regimen = RegimenTributario.MypeTributario, decimal montoOportunidad = 1500)
    {
        await Servicio<IRepositorioNegocio>().GuardarAsync(
            new Negocio { RazonSocial = "QUISPE FLORES ROSA", NombreComercial = "Bordados Rosita", Regimen = regimen }, Ct);

        var contactos = Servicio<IRepositorioContactos>();
        var empresa = new Empresa { Ruc = "20131312955", RazonSocial = "TEXTILES ANDINA S.A.C." };
        var contacto = new Contacto { Nombres = "JOSÉ LUIS", Apellidos = "HUAMÁN", Telefono = "987 654 321", EmpresaId = empresa.Id };
        await contactos.GuardarEmpresaAsync(empresa, Ct);
        await contactos.GuardarContactoAsync(contacto, Ct);

        var oportunidad = new Oportunidad
        {
            Titulo = "50 polos bordados",
            Monto = montoOportunidad,
            EmpresaId = empresa.Id,
            ContactoId = contacto.Id,
        };
        await Oportunidades.GuardarAsync(oportunidad, Ct);

        return (empresa, contacto, oportunidad);
    }

    private async Task<Cotizacion> GuardarCotizacionAsync(Oportunidad oportunidad)
    {
        var cotizacion = new Cotizacion
        {
            EmpresaId = oportunidad.EmpresaId,
            ContactoId = oportunidad.ContactoId,
            OportunidadId = oportunidad.Id,
        };
        cotizacion.Lineas.Add(new LineaCotizacion { Descripcion = "Polo bordado", Cantidad = 50, PrecioUnitario = 30 });
        await Cotizaciones.GuardarAsync(cotizacion, Ct);
        return cotizacion;
    }

    /// <summary>Abre la página como desde el pipeline: <c>cotizaciones/nueva?oportunidad={id}</c>.</summary>
    private IRenderedComponent<CotizacionEditar> RenderDesdeOportunidad(Guid oportunidadId)
    {
        Servicio<NavigationManager>().NavigateTo($"cotizaciones/nueva?oportunidad={oportunidadId}");
        return Render<CotizacionEditar>();
    }

    private static string Total(IRenderedComponent<CotizacionEditar> pagina, string cual) =>
        pagina.Find($"[data-total={cual}]").TextContent.Trim();

    [Fact]
    public async Task DesdeUnaOportunidadSeCompletanElClienteYLaPrimeraLineaYAlGuardarRecibeSuNumero()
    {
        var (empresa, contacto, oportunidad) = await PrepararAsync();

        var pagina = RenderDesdeOportunidad(oportunidad.Id);

        // 1,500 con IGV incluido: 1,271.19 + 228.81
        Assert.Equal(FormatoPeru.Soles(1271.19m), Total(pagina, "subtotal"));
        Assert.Equal(FormatoPeru.Soles(228.81m), Total(pagina, "igv"));
        Assert.Equal(FormatoPeru.Soles(1500), Total(pagina, "total"));
        Assert.Contains("MIL QUINIENTOS CON 00/100 SOLES", pagina.Markup);

        pagina.FindAll("button").First(b => b.TextContent.Trim() == "Guardar").Click();

        var guardada = Assert.Single(await Cotizaciones.ListarAsync(Ct));
        Assert.Equal("COT-0001", guardada.Codigo);
        Assert.Equal(empresa.Id, guardada.EmpresaId);
        Assert.Equal(contacto.Id, guardada.ContactoId);
        Assert.Equal(oportunidad.Id, guardada.OportunidadId);
        Assert.Equal("50 polos bordados", guardada.Lineas[0].Descripcion);
        Assert.Equal(1500, guardada.Lineas[0].PrecioUnitario);
        Assert.EndsWith($"cotizaciones/{guardada.Id}", Servicio<NavigationManager>().Uri);
    }

    [Fact]
    public async Task EnElNuevoRusLaCotizacionSaleSinIgv()
    {
        var (_, _, oportunidad) = await PrepararAsync(RegimenTributario.NuevoRus);

        var pagina = RenderDesdeOportunidad(oportunidad.Id);

        Assert.Empty(pagina.FindAll("[data-total=igv]"));
        Assert.Equal(FormatoPeru.Soles(1500), Total(pagina, "total"));
    }

    [Fact]
    public async Task SePuedenAgregarYQuitarLineas()
    {
        await PrepararAsync();
        var pagina = Render<CotizacionEditar>();
        Assert.Single(pagina.FindAll("[data-linea]"));

        pagina.FindAll("button").First(b => b.TextContent.Contains("Agregar producto o servicio")).Click();
        Assert.Equal(2, pagina.FindAll("[data-linea]").Count);

        pagina.Find("[data-linea] button[aria-label='Quitar línea']").Click();
        Assert.Single(pagina.FindAll("[data-linea]"));
    }

    [Fact]
    public async Task EnviarPorWhatsAppAbreElChatDelContactoMarcaEnviadaYAvanzaLaOportunidad()
    {
        var (_, _, oportunidad) = await PrepararAsync(montoOportunidad: 0);
        var cotizacion = await GuardarCotizacionAsync(oportunidad);

        var pagina = Render<CotizacionEditar>(p => p.Add(c => c.Id, cotizacion.Id));

        var enlace = pagina.Find("a[href^='https://wa.me/51987654321?text=']");
        var mensaje = Uri.UnescapeDataString(enlace.GetAttribute("href")!);
        Assert.Contains("Hola José Luis,", mensaje);
        Assert.Contains("cotización COT-0001 de Bordados Rosita", mensaje);
        Assert.Contains("*Total: S/ 1,500.00*", mensaje);

        enlace.Click();

        pagina.WaitForAssertion(() =>
            Assert.Equal(EstadoCotizacion.Enviada, Cotizaciones.ObtenerAsync(cotizacion.Id).GetAwaiter().GetResult()!.Estado));
        var avanzada = (await Oportunidades.ObtenerAsync(oportunidad.Id, Ct))!;
        Assert.Equal(EtapaOportunidad.Cotizado, avanzada.Etapa);
        Assert.Equal(1500, avanzada.Monto);
    }

    [Fact]
    public async Task ElPdfSeEntregaConElNumeroDeLaCotizacion()
    {
        var (_, _, oportunidad) = await PrepararAsync();
        var cotizacion = await GuardarCotizacionAsync(oportunidad);

        var pagina = Render<CotizacionEditar>(p => p.Add(c => c.Id, cotizacion.Id));
        pagina.FindAll("button").First(b => b.TextContent.Contains("Descargar PDF")).Click();

        pagina.WaitForAssertion(() => Assert.Single(Archivos.Entregados), TimeSpan.FromSeconds(10));
        var (nombre, tipo, contenido) = Archivos.Entregados[0];
        Assert.Equal("COT-0001.pdf", nombre);
        Assert.Equal("application/pdf", tipo);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(contenido, 0, 4));
    }

    [Fact]
    public async Task SinDatosDelNegocioAvisaYNoPermiteGenerarElPdf()
    {
        var contacto = new Contacto { Nombres = "Rosa", Telefono = "987654321" };
        await Servicio<IRepositorioContactos>().GuardarContactoAsync(contacto, Ct);
        var cotizacion = new Cotizacion { ContactoId = contacto.Id };
        cotizacion.Lineas.Add(new LineaCotizacion { Descripcion = "Polo", PrecioUnitario = 30 });
        await Cotizaciones.GuardarAsync(cotizacion, Ct);

        var pagina = Render<CotizacionEditar>(p => p.Add(c => c.Id, cotizacion.Id));

        Assert.Contains("Completa los datos de tu negocio", pagina.Markup);
        Assert.True(pagina.FindAll("button").First(b => b.TextContent.Contains("Descargar PDF")).HasAttribute("disabled"));
    }
}
