using Vortex.Domain.Tareas;
using Vortex.Infrastructure.Datos;
using Vortex.Infrastructure.Tareas;
using Xunit;

namespace Vortex.Domain.Tests.Infraestructura;

public abstract class RepositorioTareasTests
{
    /// <summary>1 de octubre de 2026, 7:00 a. m. en Lima.</summary>
    private static readonly DateTimeOffset Ahora = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly RecordatoriosAnotados recordatorios = new();
    private RepositorioTareasConRecordatorios? creado;
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private RepositorioTareasConRecordatorios Repositorio => creado ??= new(CrearInterno(), recordatorios, new RelojFijo(Ahora));

    /// <summary>El repositorio que guarda de verdad; los avisos los agrega <see cref="RepositorioTareasConRecordatorios"/>.</summary>
    protected abstract IRepositorioTareas CrearInterno();

    [Fact]
    public async Task ListaPorFechaYHora()
    {
        await Repositorio.GuardarAsync(new Tarea { Titulo = "Viernes", Fecha = new DateOnly(2026, 10, 9) }, ct);
        await Repositorio.GuardarAsync(new Tarea { Titulo = "Lunes tarde", Fecha = new DateOnly(2026, 10, 5), Hora = new TimeOnly(15, 0) }, ct);
        await Repositorio.GuardarAsync(new Tarea { Titulo = "Lunes temprano", Fecha = new DateOnly(2026, 10, 5), Hora = new TimeOnly(9, 0) }, ct);

        var lista = await Repositorio.ListarAsync(ct);

        Assert.Equal(["Lunes temprano", "Lunes tarde", "Viernes"], lista.Select(t => t.Titulo));
    }

    [Fact]
    public async Task ListarDeBuscaEnEmpresaContactoYOportunidad()
    {
        var id = Guid.NewGuid();
        await Repositorio.GuardarAsync(new Tarea { Titulo = "De la empresa", EmpresaId = id }, ct);
        await Repositorio.GuardarAsync(new Tarea { Titulo = "Del contacto", ContactoId = id }, ct);
        await Repositorio.GuardarAsync(new Tarea { Titulo = "De la oportunidad", OportunidadId = id }, ct);
        await Repositorio.GuardarAsync(new Tarea { Titulo = "De otro", ContactoId = Guid.NewGuid() }, ct);

        Assert.Equal(3, (await Repositorio.ListarDeAsync(id, ct)).Count);
    }

    [Fact]
    public async Task DesvincularQuitaSoloEseVinculoYConservaLaTarea()
    {
        var contactoId = Guid.NewGuid();
        var empresaId = Guid.NewGuid();
        var tarea = new Tarea { Titulo = "Llamar", ContactoId = contactoId, EmpresaId = empresaId };
        await Repositorio.GuardarAsync(tarea, ct);

        await Repositorio.DesvincularAsync(contactoId, ct);

        var guardada = (await Repositorio.ObtenerAsync(tarea.Id, ct))!;
        Assert.Null(guardada.ContactoId);
        Assert.Equal(empresaId, guardada.EmpresaId);
    }

    [Fact]
    public async Task CompletarUnaCopiaNoAfectaLoGuardado()
    {
        var tarea = new Tarea { Titulo = "Llamar" };
        await Repositorio.GuardarAsync(tarea, ct);

        (await Repositorio.ObtenerAsync(tarea.Id, ct))!.Completar(Ahora);

        Assert.False((await Repositorio.ObtenerAsync(tarea.Id, ct))!.Completada);
    }

    [Fact]
    public async Task GuardarConAvisoLoProgramaYCompletarlaLoCancela()
    {
        var tarea = new Tarea { Titulo = "Llamar", Fecha = new DateOnly(2026, 10, 5), Hora = new TimeOnly(10, 0), AvisarMinutosAntes = 15 };

        await Repositorio.GuardarAsync(tarea, ct);
        var programado = Assert.Single(recordatorios.Programados);
        Assert.Equal(tarea.Id, programado.TareaId);

        tarea.Completar(Ahora);
        await Repositorio.GuardarAsync(tarea, ct);
        Assert.Equal([tarea.Id], recordatorios.Cancelados);
    }

    [Fact]
    public async Task GuardarSinAvisoOConAvisoPasadoLoCancela()
    {
        var sinAviso = new Tarea { Titulo = "Sin aviso", Fecha = new DateOnly(2026, 10, 5) };
        var pasada = new Tarea { Titulo = "Ayer", Fecha = new DateOnly(2026, 9, 30), AvisarMinutosAntes = 0 };

        await Repositorio.GuardarAsync(sinAviso, ct);
        await Repositorio.GuardarAsync(pasada, ct);

        Assert.Empty(recordatorios.Programados);
        Assert.Equal([sinAviso.Id, pasada.Id], recordatorios.Cancelados);
    }

    [Fact]
    public async Task EliminarCancelaElAviso()
    {
        var tarea = new Tarea { Titulo = "Llamar", Fecha = new DateOnly(2026, 10, 5), AvisarMinutosAntes = 0 };
        await Repositorio.GuardarAsync(tarea, ct);

        await Repositorio.EliminarAsync(tarea.Id, ct);

        Assert.Null(await Repositorio.ObtenerAsync(tarea.Id, ct));
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

public sealed class RepositorioTareasEnMemoriaTests : RepositorioTareasTests
{
    protected override IRepositorioTareas CrearInterno() => new RepositorioTareasEnMemoria();
}

public sealed class RepositorioTareasSqliteTests : RepositorioTareasTests, IDisposable
{
    private readonly BaseSqliteDePrueba bd = new();

    protected override IRepositorioTareas CrearInterno() => new RepositorioTareasSql(bd.Fabrica);

    public void Dispose() => bd.Dispose();
}
