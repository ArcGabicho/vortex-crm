using Vortex.Domain.Catalogo;
using Vortex.Domain.Ventas;
using Vortex.Infrastructure.Catalogo;
using Xunit;

namespace Vortex.Domain.Tests.Catalogo;

public class ProductoTests
{
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    [Theory]
    // Precio registrado sin IGV
    [InlineData(100, false, ModoIgv.Incluido, 118)]
    [InlineData(100, false, ModoIgv.Adicional, 100)]
    [InlineData(100, false, ModoIgv.NoAplica, 100)]
    // Precio registrado con IGV
    [InlineData(118, true, ModoIgv.Adicional, 100)]
    [InlineData(25, true, ModoIgv.Adicional, 21.19)] // 25 / 1.18 = 21.186...
    [InlineData(118, true, ModoIgv.Incluido, 118)]
    [InlineData(118, true, ModoIgv.NoAplica, 118)]
    public void ElPrecioSeConvierteSegunElModoDeIgvDeLaCotizacion(decimal precio, bool incluyeIgv, ModoIgv modo, decimal esperado)
    {
        var producto = new Producto { Precio = precio, PrecioIncluyeIgv = incluyeIgv };

        Assert.Equal(esperado, producto.PrecioPara(modo));
    }

    [Fact]
    public void LaLineaCopiaLosDatosDelProductoYUsaLaDescripcionSiExiste()
    {
        var producto = new Producto
        {
            Nombre = "Polo bordado",
            Descripcion = "Polo cuello redondo jersey 30/1 con logo bordado",
            Unidad = "UND",
            Precio = 100,
            PrecioIncluyeIgv = false,
        };

        var linea = producto.CrearLinea(ModoIgv.Incluido, cantidad: 50);

        Assert.Equal(producto.Id, linea.ProductoId);
        Assert.Equal("Polo cuello redondo jersey 30/1 con logo bordado", linea.Descripcion);
        Assert.Equal(50, linea.Cantidad);
        Assert.Equal(118, linea.PrecioUnitario);

        // Cambiar el producto después no altera la línea
        producto.Precio = 999;
        Assert.Equal(118, linea.PrecioUnitario);
    }

    [Fact]
    public void SinDescripcionLaLineaUsaElNombre() =>
        Assert.Equal("Gorra", new Producto { Nombre = "Gorra", Descripcion = " " }.CrearLinea(ModoIgv.Incluido).Descripcion);

    [Fact]
    public void AgregarUnaLineaReemplazaLaLineaVaciaDeUnaCotizacionNueva()
    {
        var cotizacion = new Cotizacion();
        cotizacion.Lineas.Add(new LineaCotizacion());
        var gorra = new Producto { Nombre = "Gorra", Precio = 18.5m };

        cotizacion.AgregarLinea(gorra.CrearLinea(cotizacion.ModoIgv));
        cotizacion.AgregarLinea(gorra.CrearLinea(cotizacion.ModoIgv));

        Assert.Equal(2, cotizacion.Lineas.Count);
        Assert.All(cotizacion.Lineas, l => Assert.Equal("Gorra", l.Descripcion));
    }

    [Fact]
    public void ValidarExigeNombreUnidadYPrecioNoNegativo()
    {
        Assert.Equal(3, new Producto { Unidad = "", Precio = -1 }.Validar().Count);
        Assert.Empty(new Producto { Nombre = "Gorra", Precio = 0 }.Validar());
    }

    [Fact]
    public void LosServiciosSeProponenPorServicio()
    {
        Assert.Equal("SERV", Unidades.PredeterminadaPara(TipoProducto.Servicio));
        Assert.Equal("UND", Unidades.PredeterminadaPara(TipoProducto.Producto));
        Assert.Contains(Unidades.Comunes, u => u.Codigo == "DOC" && u.Nombre == "Docena");
    }

    [Fact]
    public async Task ElRepositorioOcultaLosInactivosBuscaYOrdenaPorNombre()
    {
        var repositorio = new RepositorioProductosEnMemoria();
        await repositorio.GuardarAsync(new Producto { Nombre = "Polo bordado", Codigo = "POL-01" }, ct);
        await repositorio.GuardarAsync(new Producto { Nombre = "Gorra bordada", Codigo = "GOR-01" }, ct);
        await repositorio.GuardarAsync(new Producto { Nombre = "Chompa", Activo = false }, ct);

        Assert.Equal(["Gorra bordada", "Polo bordado"], (await repositorio.ListarAsync(cancellationToken: ct)).Select(p => p.Nombre));
        Assert.Equal(3, (await repositorio.ListarAsync(incluirInactivos: true, cancellationToken: ct)).Count);
        Assert.Equal(["Polo bordado"], (await repositorio.ListarAsync("pol-0", cancellationToken: ct)).Select(p => p.Nombre));
    }

    [Fact]
    public async Task ElCodigoNoSePuedeRepetirSinImportarMayusculas()
    {
        var repositorio = new RepositorioProductosEnMemoria();
        var polo = new Producto { Nombre = "Polo", Codigo = "POL-01" };
        await repositorio.GuardarAsync(polo, ct);

        Assert.True(await repositorio.ExisteCodigoAsync("pol-01 ", cancellationToken: ct));
        Assert.False(await repositorio.ExisteCodigoAsync("POL-01", polo.Id, ct));
    }
}
