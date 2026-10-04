using Vortex.Domain.Tareas;

namespace Vortex.Infrastructure.Tareas;

/// <summary>Para donde no hay notificaciones del sistema (web, Windows): las tareas se ven en Inicio.</summary>
public sealed class SinRecordatorios : IServicioRecordatorios
{
    public bool Disponible => false;

    public Task<bool> SolicitarPermisoAsync() => Task.FromResult(false);

    public Task ProgramarAsync(Recordatorio recordatorio, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task CancelarAsync(Guid tareaId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
