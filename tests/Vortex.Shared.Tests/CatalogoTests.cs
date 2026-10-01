using Bunit;
using Vortex.Domain.Catalogo;
using Vortex.Domain.Comun;
using Vortex.Domain.Configuracion;
using Vortex.Domain.Contactos;
using Vortex.Domain.Ventas;
using Vortex.Shared.Pages.Catalogo;
using Vortex.Shared.Pages.Ventas;
using Xunit;

namespace Vortex.Shared.Tests;

public class CatalogoTests : PruebaUi
{
    private IRepositorioProductos Productos => Servicio<IRepositorioProductos>();

    [Fact]
    public async Task GuardarUnProductoNuevoLoAgregaAlCatalogo()
    {
        var pagina = Render<ProductoEditar>();

        pagina.FindAll("input")[1].Change("Polo bordado"); // [0] es el select de tipo
        pagina.Find("input[inputmode=decimal]").Change("25");
        pagina.FindAll("button").First(b => b.TextContent.Trim() == "Guardar").Click();

        var producto = Assert.Single(await Productos.ListarAsync(cancellationToken: Ct));
        Assert.Equal("Polo bordado", producto.Nombre);
        Assert.Equal(25, producto.Precio);
        Assert.True(producto.PrecioIncluyeIgv);
    }

    [Fact]
    public async Task NoPermiteRepetirElCodigo()
    {
        await Productos.GuardarAsync(new Producto { Nombre = "Polo", Codigo = "POL-01" }, Ct);
        var pagina = Render<ProductoEditar>();

        pagina.FindAll("input")[1].Change("Otro polo");
        pagina.FindAll("input")[2].Change("pol-01");
        pagina.FindAll("button").First(b => b.TextContent.Trim() == "Guardar").Click();

        Assert.Contains("Ya tienes otro producto con el código pol-01", pagina.Markup);
        Assert.Single(await Productos.ListarAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task LaListaMuestraPrecioYSiIncluyeIgvYOcultaLosInactivos()
    {
        await Productos.GuardarAsync(new Producto { Nombre = "Polo bordado", Precio = 25 }, Ct);
        await Productos.GuardarAsync(new Producto { Nombre = "Diseño", Tipo = TipoProducto.Servicio, Unidad = "SERV", Precio = 100, PrecioIncluyeIgv = false }, Ct);
        await Productos.GuardarAsync(new Producto { Nombre = "Chompa", Activo = false }, Ct);

        var pagina = Render<Productos>();

        var polo = pagina.Find("[data-producto='Polo bordado']").TextContent;
        Assert.Contains(FormatoPeru.Soles(25), polo);
        Assert.Contains("con IGV", polo);
        Assert.Contains("sin IGV", pagina.Find("[data-producto='Diseño']").TextContent);
        Assert.Empty(pagina.FindAll("[data-producto='Chompa']"));
    }

    [Fact]
    public async Task DesdeLaCotizacionSeAgregaUnProductoConElPrecioConvertido()
    {
        await Servicio<IRepositorioNegocio>().GuardarAsync(new Negocio { RazonSocial = "Bordados Rosita" }, Ct);
        var contacto = new Contacto { Nombres = "Rosa" };
        await Servicio<IRepositorioContactos>().GuardarContactoAsync(contacto, Ct);
        await Productos.GuardarAsync(new Producto { Nombre = "Diseño del bordado", Unidad = "SERV", Precio = 100, PrecioIncluyeIgv = false }, Ct);

        var pagina = Render<CotizacionEditar>();
        pagina.FindAll("button").First(b => b.TextContent.Trim() == "Agregar del catálogo").Click();

        // El diálogo muestra el precio ya convertido al modo de la cotización (IGV incluido)
        Dialogos.WaitForAssertion(() => Assert.NotEmpty(Dialogos.FindAll("[data-elegir-producto]")));
        Assert.Contains(FormatoPeru.Soles(118), Dialogos.Find("[data-elegir-producto]").TextContent);
        Dialogos.Find("[data-elegir-producto]").Click();

        // Reemplaza la línea vacía de la cotización nueva en lugar de agregar otra
        pagina.WaitForAssertion(() => Assert.Equal(FormatoPeru.Soles(118), pagina.Find("[data-total=total]").TextContent.Trim()));
        Assert.Single(pagina.FindAll("[data-linea]"));
    }
}
