using Vortex.Domain.Ventas;
using Vortex.Infrastructure.Datos;
using Vortex.Infrastructure.Ventas;
using Xunit;

namespace Vortex.Domain.Tests.Infraestructura;

public abstract class RepositorioOportunidadesTests
{
    private IRepositorioOportunidades? creado;
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private IRepositorioOportunidades Repositorio => creado ??= Crear();

    protected abstract IRepositorioOportunidades Crear();

    [Fact]
    public async Task ListaPrimeroLasDeCierreMasProximoYAlFinalLasSinFecha()
    {
        await Repositorio.GuardarAsync(new Oportunidad { Titulo = "Sin fecha" }, ct);
        await Repositorio.GuardarAsync(new Oportunidad { Titulo = "Noviembre", FechaCierreEstimada = new DateOnly(2026, 11, 15) }, ct);
        await Repositorio.GuardarAsync(new Oportunidad { Titulo = "Octubre", FechaCierreEstimada = new DateOnly(2026, 10, 5) }, ct);

        var lista = await Repositorio.ListarAsync(ct);

        Assert.Equal(["Octubre", "Noviembre", "Sin fecha"], lista.Select(o => o.Titulo));
    }

    [Fact]
    public async Task CambiarLaEtapaDeUnaCopiaNoAfectaLoGuardado()
    {
        var oportunidad = new Oportunidad { Titulo = "Polos" };
        await Repositorio.GuardarAsync(oportunidad, ct);

        var copia = (await Repositorio.ObtenerAsync(oportunidad.Id, ct))!;
        copia.CambiarEtapa(EtapaOportunidad.Ganado, DateTimeOffset.UtcNow);

        Assert.Equal(EtapaOportunidad.Prospecto, (await Repositorio.ObtenerAsync(oportunidad.Id, ct))!.Etapa);
    }

    [Fact]
    public async Task ContarDeClienteBuscaEnEmpresaYEnContacto()
    {
        var empresaId = Guid.NewGuid();
        var contactoId = Guid.NewGuid();
        await Repositorio.GuardarAsync(new Oportunidad { EmpresaId = empresaId }, ct);
        await Repositorio.GuardarAsync(new Oportunidad { EmpresaId = empresaId, ContactoId = contactoId }, ct);
        await Repositorio.GuardarAsync(new Oportunidad { ContactoId = Guid.NewGuid() }, ct);

        Assert.Equal(2, await Repositorio.ContarDeClienteAsync(empresaId, ct));
        Assert.Equal(1, await Repositorio.ContarDeClienteAsync(contactoId, ct));
        Assert.Equal(0, await Repositorio.ContarDeClienteAsync(Guid.NewGuid(), ct));
    }
}

public sealed class RepositorioOportunidadesEnMemoriaTests : RepositorioOportunidadesTests
{
    protected override IRepositorioOportunidades Crear() => new RepositorioOportunidadesEnMemoria();
}

public sealed class RepositorioOportunidadesSqliteTests : RepositorioOportunidadesTests, IDisposable
{
    private readonly BaseSqliteDePrueba bd = new();

    protected override IRepositorioOportunidades Crear() => new RepositorioOportunidadesSql(bd.Fabrica);

    public void Dispose() => bd.Dispose();
}
