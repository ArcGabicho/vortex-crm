using Vortex.Domain.Catalogo;
using Vortex.Domain.Configuracion;
using Vortex.Infrastructure.Catalogo;
using Vortex.Infrastructure.Configuracion;
using Vortex.Infrastructure.Datos;
using Xunit;

namespace Vortex.Domain.Tests.Infraestructura;

public abstract class RepositorioNegocioTests
{
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;
    private IRepositorioNegocio? creado;

    private IRepositorioNegocio Repositorio => creado ??= Crear();

    protected abstract IRepositorioNegocio Crear();

    [Fact]
    public async Task SinConfigurarDevuelveUnNegocioVacio()
    {
        var negocio = await Repositorio.ObtenerAsync(ct);

        Assert.False(negocio.EstaConfigurado);
        Assert.Empty(negocio.Sucursales);
    }

    [Fact]
    public async Task GuardaLosDatosYLasSucursales()
    {
        var negocio = new Negocio { RazonSocial = "Bordados Rosita", Ruc = "10460278975", Ubigeo = "150122", Regimen = RegimenTributario.NuevoRus };
        negocio.Sucursales.Add(new Sucursal { Nombre = "Gamarra", CodigoEstablecimiento = "0001", Ubigeo = "150115" });
        negocio.Sucursales.Add(new Sucursal { Nombre = "Principal" });
        await Repositorio.GuardarAsync(negocio, ct);
        await Repositorio.GuardarAsync(negocio, ct); // guardar dos veces actualiza, no duplica

        var guardado = await Repositorio.ObtenerAsync(ct);

        Assert.Equal("Bordados Rosita", guardado.RazonSocial);
        Assert.Equal("150122", guardado.Ubigeo);
        Assert.Equal(RegimenTributario.NuevoRus, guardado.Regimen);
        Assert.Equal(["Principal", "Gamarra"], guardado.Sucursales.Select(s => s.Nombre)); // por código de establecimiento
    }

    [Fact]
    public async Task AgregarCambiarYQuitarSucursales()
    {
        var negocio = new Negocio { RazonSocial = "Bordados Rosita" };
        negocio.Sucursales.Add(new Sucursal { Nombre = "Principal" });
        negocio.Sucursales.Add(new Sucursal { Nombre = "Gamarra", CodigoEstablecimiento = "0001" });
        await Repositorio.GuardarAsync(negocio, ct);

        var editado = await Repositorio.ObtenerAsync(ct);
        editado.Sucursales[0].Nombre = "Casa matriz";
        editado.Sucursales.RemoveAt(1);
        editado.Sucursales.Add(new Sucursal { Nombre = "Arequipa", CodigoEstablecimiento = "0002", Activa = false });
        await Repositorio.GuardarAsync(editado, ct);

        var guardado = await Repositorio.ObtenerAsync(ct);
        Assert.Equal(["Casa matriz", "Arequipa"], guardado.Sucursales.Select(s => s.Nombre));
        Assert.False(guardado.Sucursales[1].Activa);
    }

    [Fact]
    public async Task EditarUnaCopiaNoCambiaLoGuardado()
    {
        var negocio = new Negocio { RazonSocial = "Original" };
        negocio.Sucursales.Add(new Sucursal { Nombre = "Principal" });
        await Repositorio.GuardarAsync(negocio, ct);

        var copia = await Repositorio.ObtenerAsync(ct);
        copia.RazonSocial = "Cambiado";
        copia.Sucursales[0].Nombre = "Cambiada";

        var guardado = await Repositorio.ObtenerAsync(ct);
        Assert.Equal("Original", guardado.RazonSocial);
        Assert.Equal("Principal", guardado.Sucursales[0].Nombre);
    }
}

public sealed class RepositorioNegocioEnMemoriaTests : RepositorioNegocioTests
{
    protected override IRepositorioNegocio Crear() => new RepositorioNegocioEnMemoria();
}

public sealed class RepositorioNegocioSqliteTests : RepositorioNegocioTests, IDisposable
{
    private readonly BaseSqliteDePrueba bd = new();

    protected override IRepositorioNegocio Crear() => new RepositorioNegocioSql(bd.Fabrica);

    public void Dispose() => bd.Dispose();
}

public abstract class RepositorioProductosTests
{
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;
    private IRepositorioProductos? creado;

    private IRepositorioProductos Repositorio => creado ??= Crear();

    protected abstract IRepositorioProductos Crear();

    [Fact]
    public async Task ListaPorNombreFiltraYOcultaLosInactivos()
    {
        await Repositorio.GuardarAsync(new Producto { Nombre = "Polo bordado", Codigo = "POL-01", Precio = 25 }, ct);
        await Repositorio.GuardarAsync(new Producto { Nombre = "Gorra", Descripcion = "Con logo bordado" }, ct);
        await Repositorio.GuardarAsync(new Producto { Nombre = "Bordado antiguo", Activo = false }, ct);

        Assert.Equal(["Gorra", "Polo bordado"], (await Repositorio.ListarAsync("BORDADO", cancellationToken: ct)).Select(p => p.Nombre));
        Assert.Equal(3, (await Repositorio.ListarAsync(incluirInactivos: true, cancellationToken: ct)).Count);
    }

    [Fact]
    public async Task ExisteCodigoNoDistingueMayusculasNiEspaciosEIgnoraElPropio()
    {
        var polo = new Producto { Nombre = "Polo", Codigo = "POL-01" };
        await Repositorio.GuardarAsync(polo, ct);

        Assert.True(await Repositorio.ExisteCodigoAsync(" pol-01 ", cancellationToken: ct));
        Assert.False(await Repositorio.ExisteCodigoAsync("POL-01", polo.Id, ct));
    }

    [Fact]
    public async Task GuardarActualizaYEliminarQuita()
    {
        var polo = new Producto { Nombre = "Polo", Precio = 25, Tipo = TipoProducto.Servicio };
        await Repositorio.GuardarAsync(polo, ct);
        polo.Precio = 30;
        await Repositorio.GuardarAsync(polo, ct);

        var guardado = (await Repositorio.ObtenerAsync(polo.Id, ct))!;
        Assert.Equal(30, guardado.Precio);
        Assert.Equal(TipoProducto.Servicio, guardado.Tipo);

        await Repositorio.EliminarAsync(polo.Id, ct);
        Assert.Null(await Repositorio.ObtenerAsync(polo.Id, ct));
    }
}

public sealed class RepositorioProductosEnMemoriaTests : RepositorioProductosTests
{
    protected override IRepositorioProductos Crear() => new RepositorioProductosEnMemoria();
}

public sealed class RepositorioProductosSqliteTests : RepositorioProductosTests, IDisposable
{
    private readonly BaseSqliteDePrueba bd = new();

    protected override IRepositorioProductos Crear() => new RepositorioProductosSql(bd.Fabrica);

    public void Dispose() => bd.Dispose();
}
