using Vortex.Domain.Ventas;

namespace Vortex.Infrastructure.Ventas;

/// <summary>
/// Repositorio temporal en memoria: los datos se pierden al cerrar la app. Se
/// reemplazará por SQLite (app) y SQL Server (API) en la Fase 2.
/// </summary>
/// <remarks>Guarda y devuelve copias, igual que los demás repositorios en memoria.</remarks>
public sealed class RepositorioCotizacionesEnMemoria : IRepositorioCotizaciones
{
    private readonly Lock candado = new();
    private readonly Dictionary<Guid, Cotizacion> cotizaciones = [];
    private int ultimoNumero;

    public Task<IReadOnlyList<Cotizacion>> ListarAsync(CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            IReadOnlyList<Cotizacion> resultado = cotizaciones.Values
                .OrderByDescending(c => c.Numero)
                .Select(c => c.Clonar())
                .ToList();
            return Task.FromResult(resultado);
        }
    }

    public Task<Cotizacion?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            return Task.FromResult(cotizaciones.GetValueOrDefault(id)?.Clonar());
        }
    }

    public Task GuardarAsync(Cotizacion cotizacion, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            if (cotizacion.Numero == 0)
            {
                cotizacion.Numero = ++ultimoNumero;
            }

            cotizaciones[cotizacion.Id] = cotizacion.Clonar();
        }

        return Task.CompletedTask;
    }

    public Task EliminarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            cotizaciones.Remove(id);
        }

        return Task.CompletedTask;
    }

    public Task<int> ContarDeClienteAsync(Guid clienteId, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            return Task.FromResult(cotizaciones.Values.Count(c => c.EmpresaId == clienteId || c.ContactoId == clienteId));
        }
    }

    public Task DesvincularOportunidadAsync(Guid oportunidadId, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            foreach (var cotizacion in cotizaciones.Values.Where(c => c.OportunidadId == oportunidadId))
            {
                cotizacion.OportunidadId = null;
            }
        }

        return Task.CompletedTask;
    }
}
