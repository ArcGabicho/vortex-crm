using Vortex.Domain.Ventas;
using Vortex.Infrastructure.Ventas;
using Xunit;

namespace Vortex.Domain.Tests.Infraestructura;

public class RepositorioOportunidadesEnMemoriaTests
{
    private readonly RepositorioOportunidadesEnMemoria repositorio = new();
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task ListaPrimeroLasDeCierreMasProximoYAlFinalLasSinFecha()
    {
        await repositorio.GuardarAsync(new Oportunidad { Titulo = "Sin fecha" }, ct);
        await repositorio.GuardarAsync(new Oportunidad { Titulo = "Noviembre", FechaCierreEstimada = new DateOnly(2026, 11, 15) }, ct);
        await repositorio.GuardarAsync(new Oportunidad { Titulo = "Octubre", FechaCierreEstimada = new DateOnly(2026, 10, 5) }, ct);

        var lista = await repositorio.ListarAsync(ct);

        Assert.Equal(["Octubre", "Noviembre", "Sin fecha"], lista.Select(o => o.Titulo));
    }

    [Fact]
    public async Task CambiarLaEtapaDeUnaCopiaNoAfectaLoGuardado()
    {
        var oportunidad = new Oportunidad { Titulo = "Polos" };
        await repositorio.GuardarAsync(oportunidad, ct);

        var copia = (await repositorio.ObtenerAsync(oportunidad.Id, ct))!;
        copia.CambiarEtapa(EtapaOportunidad.Ganado, DateTimeOffset.UtcNow);

        Assert.Equal(EtapaOportunidad.Prospecto, (await repositorio.ObtenerAsync(oportunidad.Id, ct))!.Etapa);
    }

    [Fact]
    public async Task ContarDeClienteBuscaEnEmpresaYEnContacto()
    {
        var empresaId = Guid.NewGuid();
        var contactoId = Guid.NewGuid();
        await repositorio.GuardarAsync(new Oportunidad { EmpresaId = empresaId }, ct);
        await repositorio.GuardarAsync(new Oportunidad { EmpresaId = empresaId, ContactoId = contactoId }, ct);
        await repositorio.GuardarAsync(new Oportunidad { ContactoId = Guid.NewGuid() }, ct);

        Assert.Equal(2, await repositorio.ContarDeClienteAsync(empresaId, ct));
        Assert.Equal(1, await repositorio.ContarDeClienteAsync(contactoId, ct));
        Assert.Equal(0, await repositorio.ContarDeClienteAsync(Guid.NewGuid(), ct));
    }
}
