using Vortex.Domain.Tareas;

namespace Vortex.Infrastructure.Tareas;

/// <summary>
/// Mantiene los avisos al día con las tareas: cada vez que se guarda una tarea se
/// programa su aviso, o se cancela si ya no corresponde (se completó, se quitó el aviso o
/// ya pasó), y al eliminarla se cancela. Así ninguna pantalla tiene que acordarse de hacerlo.
/// </summary>
public sealed class RepositorioTareasConRecordatorios(
    IRepositorioTareas interno,
    IServicioRecordatorios recordatorios,
    TimeProvider? reloj = null) : IRepositorioTareas
{
    private readonly TimeProvider reloj = reloj ?? TimeProvider.System;

    public Task<IReadOnlyList<Tarea>> ListarAsync(CancellationToken cancellationToken = default) =>
        interno.ListarAsync(cancellationToken);

    public Task<IReadOnlyList<Tarea>> ListarDeAsync(Guid relacionadoId, CancellationToken cancellationToken = default) =>
        interno.ListarDeAsync(relacionadoId, cancellationToken);

    public Task<Tarea?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default) =>
        interno.ObtenerAsync(id, cancellationToken);

    public async Task GuardarAsync(Tarea tarea, CancellationToken cancellationToken = default)
    {
        await interno.GuardarAsync(tarea, cancellationToken);

        if (tarea.CrearRecordatorio(reloj.GetUtcNow()) is { } recordatorio)
        {
            await recordatorios.ProgramarAsync(recordatorio, cancellationToken);
        }
        else
        {
            await recordatorios.CancelarAsync(tarea.Id, cancellationToken);
        }
    }

    public async Task EliminarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await interno.EliminarAsync(id, cancellationToken);
        await recordatorios.CancelarAsync(id, cancellationToken);
    }

    /// <summary>El aviso solo lleva el título y la hora, así que desvincular no lo cambia.</summary>
    public Task DesvincularAsync(Guid relacionadoId, CancellationToken cancellationToken = default) =>
        interno.DesvincularAsync(relacionadoId, cancellationToken);
}
