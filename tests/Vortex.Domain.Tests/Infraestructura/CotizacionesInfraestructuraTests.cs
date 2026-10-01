using UglyToad.PdfPig;
using Vortex.Domain.Configuracion;
using Vortex.Domain.Contactos;
using Vortex.Domain.Ventas;
using Vortex.Infrastructure.Ventas;
using Vortex.Infrastructure.Ventas.Pdf;
using Xunit;

namespace Vortex.Domain.Tests.Infraestructura;

public class CotizacionesInfraestructuraTests
{
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private static Cotizacion Ejemplo()
    {
        var cotizacion = new Cotizacion
        {
            Numero = 1,
            ContactoId = Guid.NewGuid(),
            FechaEmision = new DateOnly(2026, 10, 1),
            ValidaHasta = new DateOnly(2026, 10, 16),
            Condiciones = "Pago: 50 % adelantado\nEntrega: 7 días hábiles",
        };
        cotizacion.Lineas.Add(new LineaCotizacion { Descripcion = "Polo bordado con logo", Cantidad = 50, PrecioUnitario = 25 });
        cotizacion.Lineas.Add(new LineaCotizacion { Descripcion = "Diseño del bordado", Unidad = "SERV", Cantidad = 1, PrecioUnitario = 250 });
        return cotizacion;
    }

    [Fact]
    public async Task GuardarAsignaNumerosCorrelativosSoloALasNuevas()
    {
        var repositorio = new RepositorioCotizacionesEnMemoria();
        var primera = new Cotizacion();
        var segunda = new Cotizacion();

        await repositorio.GuardarAsync(primera, ct);
        await repositorio.GuardarAsync(segunda, ct);
        await repositorio.GuardarAsync(primera, ct); // actualizar no cambia el número

        Assert.Equal(1, primera.Numero);
        Assert.Equal(2, segunda.Numero);
        Assert.Equal([2, 1], (await repositorio.ListarAsync(ct)).Select(c => c.Numero));
    }

    [Fact]
    public async Task EliminarUnaOportunidadDejaSusCotizacionesSinVinculo()
    {
        var repositorio = new RepositorioCotizacionesEnMemoria();
        var oportunidadId = Guid.NewGuid();
        var cotizacion = new Cotizacion { OportunidadId = oportunidadId, EmpresaId = Guid.NewGuid() };
        await repositorio.GuardarAsync(cotizacion, ct);

        await repositorio.DesvincularOportunidadAsync(oportunidadId, ct);

        Assert.Null((await repositorio.ObtenerAsync(cotizacion.Id, ct))!.OportunidadId);
        Assert.Equal(1, await repositorio.ContarDeClienteAsync(cotizacion.EmpresaId!.Value, ct));
    }

    [Fact]
    public void ElPdfTieneLosDatosDelNegocioElClienteLasLineasYElTotalEnLetras()
    {
        var negocio = new Negocio
        {
            RazonSocial = "QUISPE FLORES ROSA",
            NombreComercial = "Bordados Rosita",
            Ruc = "10460278975",
            Telefono = "987654321",
        };
        var cliente = ClienteCotizacion.Crear(
            new Empresa { Ruc = "20131312955", RazonSocial = "TEXTILES ANDINA S.A.C." },
            new Contacto { Nombres = "José", Apellidos = "Huamán" });

        var pdf = new GeneradorPdfCotizaciones().Generar(Ejemplo(), negocio, cliente);

        using var documento = PdfDocument.Open(pdf);
        var texto = string.Join(" ", documento.GetPages().Select(p => string.Join(" ", p.GetWords().Select(w => w.Text))));

        Assert.Equal(1, documento.NumberOfPages);
        Assert.Contains("Bordados Rosita", texto);
        Assert.Contains("RUC 10460278975", texto);
        Assert.Contains("COT-0001", texto);
        Assert.Contains("TEXTILES ANDINA S.A.C.", texto);
        Assert.Contains("Polo bordado con logo", texto);
        // 50 × 25 + 250 = 1,500 con IGV incluido
        Assert.Contains("S/ 1,500.00", texto);
        Assert.Contains("S/ 228.81", texto);
        Assert.Contains("MIL QUINIENTOS CON 00/100 SOLES", texto);
        Assert.Contains("Entrega: 7 días hábiles", texto);
    }

    [Fact]
    public void SinIgvElPdfNoMuestraSubtotalNiIgv()
    {
        var cotizacion = Ejemplo();
        cotizacion.ModoIgv = ModoIgv.NoAplica;

        var pdf = new GeneradorPdfCotizaciones().Generar(
            cotizacion, new Negocio { RazonSocial = "Rosa" }, new ClienteCotizacion("Cliente", null, null, null, null, null));

        using var documento = PdfDocument.Open(pdf);
        var texto = string.Join(" ", documento.GetPage(1).GetWords().Select(w => w.Text));
        Assert.DoesNotContain("IGV (18", texto);
        Assert.Contains("no afecta al IGV", texto);
    }
}
