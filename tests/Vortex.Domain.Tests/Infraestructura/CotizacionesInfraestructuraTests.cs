using UglyToad.PdfPig;
using Vortex.Domain.Comun;
using Vortex.Domain.Configuracion;
using Vortex.Domain.Contactos;
using Vortex.Domain.Ventas;
using Vortex.Infrastructure.Ventas.Pdf;
using Xunit;

namespace Vortex.Domain.Tests.Infraestructura;

public class CotizacionesInfraestructuraTests
{
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
    public void ElPdfEscribeElDistritoDespuesDeLasDirecciones()
    {
        var ubigeos = new CatalogoUbigeos([new Ubigeo("150122", "LIMA", "LIMA", "MIRAFLORES"), new Ubigeo("150131", "LIMA", "LIMA", "SAN ISIDRO")]);
        var negocio = new Negocio { RazonSocial = "Bordados Rosita", Direccion = "Av. Larco 345", Ubigeo = "150122" };
        var cliente = ClienteCotizacion.Crear(
            new Empresa { Ruc = "20131312955", RazonSocial = "SUNAT", Direccion = "Av. Garcilaso 381", Ubigeo = "150131" }, null, ubigeos);

        var pdf = new GeneradorPdfCotizaciones(ubigeos).Generar(Ejemplo(), negocio, cliente);

        using var documento = PdfDocument.Open(pdf);
        var texto = string.Join(" ", documento.GetPage(1).GetWords().Select(w => w.Text));
        Assert.Contains("Av. Larco 345, MIRAFLORES - LIMA - LIMA", texto);
        Assert.Contains("Av. Garcilaso 381, SAN ISIDRO - LIMA - LIMA", texto);
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
