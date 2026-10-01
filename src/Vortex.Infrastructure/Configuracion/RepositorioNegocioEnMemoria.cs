using Vortex.Domain.Configuracion;

namespace Vortex.Infrastructure.Configuracion;

/// <summary>Datos del negocio en memoria, hasta la Fase 2.</summary>
public sealed class RepositorioNegocioEnMemoria : IRepositorioNegocio
{
    private readonly Lock candado = new();
    private Negocio negocio = new();

    public Task<Negocio> ObtenerAsync(CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            return Task.FromResult(negocio.Clonar());
        }
    }

    public Task GuardarAsync(Negocio negocio, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            this.negocio = negocio.Clonar();
        }

        return Task.CompletedTask;
    }
}
