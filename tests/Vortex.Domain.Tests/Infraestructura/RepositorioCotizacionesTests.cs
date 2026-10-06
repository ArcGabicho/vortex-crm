using Vortex.Domain.Ventas;
using Vortex.Infrastructure.Datos;
using Vortex.Infrastructure.Ventas;
using Xunit;

namespace Vortex.Domain.Tests.Infraestructura;

public abstract class RepositorioCotizacionesTests
{
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;
    private IRepositorioCotizaciones? creado;

    private IRepositorioCotizaciones Repositorio => creado ??= Crear();

    protected abstract IRepositorioCotizaciones Crear();

    [Fact]
    public async Task GuardarAsignaNumerosCorrelativosSoloALasNuevas()
    {
        var primera = new Cotizacion();
        var segunda = new Cotizacion();

        await Repositorio.GuardarAsync(primera, ct);
        await Repositorio.GuardarAsync(segunda, ct);
        await Repositorio.GuardarAsync(primera, ct); // actualizar no cambia el número

        Assert.Equal(1, primera.Numero);
        Assert.Equal(2, segunda.Numero);
        Assert.Equal([2, 1], (await Repositorio.ListarAsync(ct)).Select(c => c.Numero));
    }

    [Fact]
    public async Task EliminarUnaOportunidadDejaSusCotizacionesSinVinculo()
    {
        var oportunidadId = Guid.NewGuid();
        var cotizacion = new Cotizacion { OportunidadId = oportunidadId, EmpresaId = Guid.NewGuid() };
        await Repositorio.GuardarAsync(cotizacion, ct);

        await Repositorio.DesvincularOportunidadAsync(oportunidadId, ct);

        Assert.Null((await Repositorio.ObtenerAsync(cotizacion.Id, ct))!.OportunidadId);
        Assert.Equal(1, await Repositorio.ContarDeClienteAsync(cotizacion.EmpresaId!.Value, ct));
    }

    [Fact]
    public async Task LasLineasYElEstadoSeGuardanEnOrdenYSePuedenCambiar()
    {
        var cotizacion = new Cotizacion { ContactoId = Guid.NewGuid(), ModoIgv = ModoIgv.Adicional, Condiciones = "Pago contra entrega" };
        cotizacion.Lineas.Add(new LineaCotizacion { Descripcion = "Polo", Cantidad = 50, PrecioUnitario = 25.5m, ProductoId = Guid.NewGuid() });
        cotizacion.Lineas.Add(new LineaCotizacion { Descripcion = "Diseño", Unidad = "SERV", Cantidad = 1, PrecioUnitario = 250 });
        cotizacion.CambiarEstado(EstadoCotizacion.Enviada, null, DateTimeOffset.UtcNow);
        await Repositorio.GuardarAsync(cotizacion, ct);

        var guardada = (await Repositorio.ObtenerAsync(cotizacion.Id, ct))!;
        Assert.Equal(["Polo", "Diseño"], guardada.Lineas.Select(l => l.Descripcion));
        Assert.Equal(cotizacion.Lineas[0].ProductoId, guardada.Lineas[0].ProductoId);
        Assert.Equal(cotizacion.Totales, guardada.Totales);
        Assert.Equal(EstadoCotizacion.Enviada, guardada.Estado);
        Assert.Equal(ModoIgv.Adicional, guardada.ModoIgv);
        Assert.Equal(cotizacion.FechaEmision, guardada.FechaEmision);

        guardada.Lineas.RemoveAt(0);
        guardada.Lineas[0].Cantidad = 2;
        await Repositorio.GuardarAsync(guardada, ct);

        var editada = (await Repositorio.ObtenerAsync(cotizacion.Id, ct))!;
        Assert.Equal(500, Assert.Single(editada.Lineas).Importe);
    }

    [Fact]
    public async Task EditarLasLineasDeUnaCopiaNoCambiaLoGuardado()
    {
        var cotizacion = new Cotizacion { ContactoId = Guid.NewGuid() };
        cotizacion.Lineas.Add(new LineaCotizacion { Descripcion = "Polo", PrecioUnitario = 25 });
        await Repositorio.GuardarAsync(cotizacion, ct);

        var copia = (await Repositorio.ObtenerAsync(cotizacion.Id, ct))!;
        copia.Lineas[0].PrecioUnitario = 999;
        cotizacion.Lineas.Clear();

        Assert.Equal(25, (await Repositorio.ObtenerAsync(cotizacion.Id, ct))!.Lineas[0].PrecioUnitario);
    }

    [Fact]
    public async Task EliminarQuitaLaCotizacion()
    {
        var cotizacion = new Cotizacion { ContactoId = Guid.NewGuid() };
        await Repositorio.GuardarAsync(cotizacion, ct);

        await Repositorio.EliminarAsync(cotizacion.Id, ct);

        Assert.Null(await Repositorio.ObtenerAsync(cotizacion.Id, ct));
    }
}

public sealed class RepositorioCotizacionesEnMemoriaTests : RepositorioCotizacionesTests
{
    protected override IRepositorioCotizaciones Crear() => new RepositorioCotizacionesEnMemoria();
}

public sealed class RepositorioCotizacionesSqliteTests : RepositorioCotizacionesTests, IDisposable
{
    private readonly BaseSqliteDePrueba bd = new();

    protected override IRepositorioCotizaciones Crear() => new RepositorioCotizacionesSql(bd.Fabrica);

    public void Dispose() => bd.Dispose();
}
