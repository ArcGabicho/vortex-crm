using Vortex.Domain.Tareas;

namespace Vortex.Infrastructure.Tareas;

/// <summary>
/// Repositorio temporal en memoria: los datos se pierden al cerrar la app. Se
/// reemplazará por SQLite (app) y SQL Server (API) en la Fase 2.
/// </summary>
/// <remarks>Guarda y devuelve copias, igual que los demás repositorios en memoria.</remarks>
public sealed class RepositorioTareasEnMemoria : IRepositorioTareas
{
    private readonly Lock candado = new();
    private readonly Dictionary<Guid, Tarea> tareas = [];

    public Task<IReadOnlyList<Tarea>> ListarAsync(CancellationToken cancellationToken = default) =>
        Listar(_ => true);

    public Task<IReadOnlyList<Tarea>> ListarDeAsync(Guid relacionadoId, CancellationToken cancellationToken = default) =>
        Listar(t => EstaVinculada(t, relacionadoId));

    public Task<Tarea?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            return Task.FromResult(tareas.GetValueOrDefault(id)?.Clonar());
        }
    }

    public Task GuardarAsync(Tarea tarea, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            tareas[tarea.Id] = tarea.Clonar();
        }

        return Task.CompletedTask;
    }

    public Task EliminarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            tareas.Remove(id);
        }

        return Task.CompletedTask;
    }

    public Task DesvincularAsync(Guid relacionadoId, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            foreach (var tarea in tareas.Values.Where(t => EstaVinculada(t, relacionadoId)))
            {
                if (tarea.EmpresaId == relacionadoId)
                {
                    tarea.EmpresaId = null;
                }

                if (tarea.ContactoId == relacionadoId)
                {
                    tarea.ContactoId = null;
                }

                if (tarea.OportunidadId == relacionadoId)
                {
                    tarea.OportunidadId = null;
                }
            }
        }

        return Task.CompletedTask;
    }

    private Task<IReadOnlyList<Tarea>> Listar(Func<Tarea, bool> condicion)
    {
        lock (candado)
        {
            IReadOnlyList<Tarea> resultado = tareas.Values
                .Where(condicion)
                .EnOrden()
                .Select(t => t.Clonar())
                .ToList();
            return Task.FromResult(resultado);
        }
    }

    private static bool EstaVinculada(Tarea tarea, Guid id) =>
        tarea.EmpresaId == id || tarea.ContactoId == id || tarea.OportunidadId == id;
}
