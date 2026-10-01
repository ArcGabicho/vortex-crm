using Vortex.Domain.Ventas;

namespace Vortex.Infrastructure.Ventas;

/// <summary>
/// Repositorio temporal en memoria: los datos se pierden al cerrar la app. Se
/// reemplazará por SQLite (app) y SQL Server (API) en la Fase 2.
/// </summary>
/// <remarks>Guarda y devuelve copias, igual que el repositorio de contactos.</remarks>
public sealed class RepositorioOportunidadesEnMemoria : IRepositorioOportunidades
{
    private readonly Lock candado = new();
    private readonly Dictionary<Guid, Oportunidad> oportunidades = [];

    public Task<IReadOnlyList<Oportunidad>> ListarAsync(CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            IReadOnlyList<Oportunidad> resultado = oportunidades.Values
                .OrderBy(o => o.FechaCierreEstimada is null)
                .ThenBy(o => o.FechaCierreEstimada)
                .ThenBy(o => o.CreadoEn)
                .Select(o => o.Clonar())
                .ToList();
            return Task.FromResult(resultado);
        }
    }

    public Task<Oportunidad?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            return Task.FromResult(oportunidades.GetValueOrDefault(id)?.Clonar());
        }
    }

    public Task GuardarAsync(Oportunidad oportunidad, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            oportunidades[oportunidad.Id] = oportunidad.Clonar();
        }

        return Task.CompletedTask;
    }

    public Task EliminarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            oportunidades.Remove(id);
        }

        return Task.CompletedTask;
    }

    public Task<int> ContarDeClienteAsync(Guid clienteId, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            return Task.FromResult(oportunidades.Values.Count(o => o.EmpresaId == clienteId || o.ContactoId == clienteId));
        }
    }
}
