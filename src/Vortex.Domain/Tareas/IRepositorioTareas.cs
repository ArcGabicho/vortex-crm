namespace Vortex.Domain.Tareas;

public interface IRepositorioTareas
{
    /// <summary>Todas las tareas, por fecha y hora.</summary>
    Task<IReadOnlyList<Tarea>> ListarAsync(CancellationToken cancellationToken = default);

    /// <summary>Las tareas de una empresa, un contacto o una oportunidad (busca el Id en los tres campos).</summary>
    Task<IReadOnlyList<Tarea>> ListarDeAsync(Guid relacionadoId, CancellationToken cancellationToken = default);

    Task<Tarea?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Crea la tarea o, si ya existe una con el mismo Id, la actualiza.</summary>
    Task GuardarAsync(Tarea tarea, CancellationToken cancellationToken = default);

    Task EliminarAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Quita el vínculo con una empresa, un contacto o una oportunidad que se eliminó; las
    /// tareas se conservan.
    /// </summary>
    Task DesvincularAsync(Guid relacionadoId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Notificaciones del sistema para los avisos de las tareas. En la app de Android son
/// notificaciones reales; donde no hay (web, Windows) no hacen nada y las tareas se ven en Inicio.
/// </summary>
public interface IServicioRecordatorios
{
    /// <summary>Si este dispositivo puede mostrar avisos fuera de la app.</summary>
    bool Disponible { get; }

    /// <summary>Pide permiso para mostrar notificaciones (Android 13 o superior). <c>true</c> si se podrá avisar.</summary>
    Task<bool> SolicitarPermisoAsync();

    /// <summary>Programa el aviso; si la tarea ya tenía uno, lo reemplaza.</summary>
    Task ProgramarAsync(Recordatorio recordatorio, CancellationToken cancellationToken = default);

    /// <summary>Cancela el aviso pendiente de la tarea y lo quita si ya se estaba mostrando.</summary>
    Task CancelarAsync(Guid tareaId, CancellationToken cancellationToken = default);
}
