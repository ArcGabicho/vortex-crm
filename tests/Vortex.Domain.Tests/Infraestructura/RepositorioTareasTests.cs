using Vortex.Domain.Tareas;
using Vortex.Infrastructure.Tareas;
using Xunit;

namespace Vortex.Domain.Tests.Infraestructura;

public class RepositorioTareasTests
{
    /// <summary>1 de octubre de 2026, 7:00 a. m. en Lima.</summary>
    private static readonly DateTimeOffset Ahora = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly RepositorioTareasEnMemoria enMemoria = new();
    private readonly RecordatoriosAnotados recordatorios = new();
    private readonly RepositorioTareasConRecordatorios repositorio;
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    public RepositorioTareasTests()
    {
        repositorio = new RepositorioTareasConRecordatorios(enMemoria, recordatorios, new RelojFijo(Ahora));
    }

    [Fact]
    public async Task ListaPorFechaYHora()
    {
        await repositorio.GuardarAsync(new Tarea { Titulo = "Viernes", Fecha = new DateOnly(2026, 10, 9) }, ct);
        await repositorio.GuardarAsync(new Tarea { Titulo = "Lunes tarde", Fecha = new DateOnly(2026, 10, 5), Hora = new TimeOnly(15, 0) }, ct);
        await repositorio.GuardarAsync(new Tarea { Titulo = "Lunes temprano", Fecha = new DateOnly(2026, 10, 5), Hora = new TimeOnly(9, 0) }, ct);

        var lista = await repositorio.ListarAsync(ct);

        Assert.Equal(["Lunes temprano", "Lunes tarde", "Viernes"], lista.Select(t => t.Titulo));
    }

    [Fact]
    public async Task ListarDeBuscaEnEmpresaContactoYOportunidad()
    {
        var id = Guid.NewGuid();
        await repositorio.GuardarAsync(new Tarea { Titulo = "De la empresa", EmpresaId = id }, ct);
        await repositorio.GuardarAsync(new Tarea { Titulo = "Del contacto", ContactoId = id }, ct);
        await repositorio.GuardarAsync(new Tarea { Titulo = "De la oportunidad", OportunidadId = id }, ct);
        await repositorio.GuardarAsync(new Tarea { Titulo = "De otro", ContactoId = Guid.NewGuid() }, ct);

        Assert.Equal(3, (await repositorio.ListarDeAsync(id, ct)).Count);
    }

    [Fact]
    public async Task DesvincularQuitaSoloEseVinculoYConservaLaTarea()
    {
        var contactoId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var tarea = new Tarea { Titulo = "Llamar", ContactoId = contactoId, EmpresaId = empresaId };
        await repositorio.GuardarAsync(tarea, ct);

        await repositorio.DesvincularAsync(contactoId, ct);

        var guardada = (await repositorio.ObtenerAsync(tarea.Id, ct))!;
        Assert.Null(guardada.ContactoId);
        Assert.Equal(empresaId, guardada.EmpresaId);
    }

    [Fact]
    public async Task CompletarUnaCopiaNoAfectaLoGuardado()
    {
        var tarea = new Tarea { Titulo = "Llamar" };
        await repositorio.GuardarAsync(tarea, ct);

        (await repositorio.ObtenerAsync(tarea.Id, ct))!.Completar(Ahora);

        Assert.False((await repositorio.ObtenerAsync(tarea.Id, ct))!.Completada);
    }

    [Fact]
    public async Task GuardarConAvisoLoProgramaYCompletarlaLoCancela()
    {
        var tarea = new Tarea { Titulo = "Llamar", Fecha = new DateOnly(2026, 10, 5), Hora = new TimeOnly(10, 0), AvisarMinutosAntes = 15 };

        await repositorio.GuardarAsync(tarea, ct);
        var programado = Assert.Single(recordatorios.Programados);
        Assert.Equal(tarea.Id, programado.TareaId);

        tarea.Completar(Ahora);
        await repositorio.GuardarAsync(tarea, ct);
        Assert.Equal([tarea.Id], recordatorios.Cancelados);
    }

    [Fact]
    public async Task GuardarSinAvisoOConAvisoPasadoLoCancela()
    {
        var sinAviso = new Tarea { Titulo = "Sin aviso", Fecha = new DateOnly(2026, 10, 5) };
        var pasada = new Tarea { Titulo = "Ayer", Fecha = new DateOnly(2026, 9, 30), AvisarMinutosAntes = 0 };

        await repositorio.GuardarAsync(sinAviso, ct);
        await repositorio.GuardarAsync(pasada, ct);

        Assert.Empty(recordatorios.Programados);
        Assert.Equal([sinAviso.Id, pasada.Id], recordatorios.Cancelados);
    }

    [Fact]
    public async Task EliminarCancelaElAviso()
    {
        var tarea = new Tarea { Titulo = "Llamar", Fecha = new DateOnly(2026, 10, 5), AvisarMinutosAntes = 0 };
        await repositorio.GuardarAsync(tarea, ct);

        await repositorio.EliminarAsync(tarea.Id, ct);

        Assert.Null(await repositorio.ObtenerAsync(tarea.Id, ct));
        Assert.Equal([tarea.Id], recordatorios.Cancelados);
    }

    private sealed class RecordatoriosAnotados : IServicioRecordatorios
    {
        public List<Recordatorio> Programados { get; } = [];

        public List<Guid> Cancelados { get; } = [];

        public bool Disponible => true;

        public Task<bool> SolicitarPermisoAsync() => Task.FromResult(true);

        public Task ProgramarAsync(Recordatorio recordatorio, CancellationToken cancellationToken = default)
        {
            Programados.Add(recordatorio);
            return Task.CompletedTask;
        }

        public Task CancelarAsync(Guid tareaId, CancellationToken cancellationToken = default)
        {
            Cancelados.Add(tareaId);
            return Task.CompletedTask;
        }
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
