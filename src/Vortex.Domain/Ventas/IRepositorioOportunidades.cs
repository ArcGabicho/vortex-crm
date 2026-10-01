namespace Vortex.Domain.Ventas;

/// <summary>Almacenamiento de las oportunidades del pipeline.</summary>
public interface IRepositorioOportunidades
{
    /// <summary>Lista las oportunidades, primero las de fecha de cierre más próxima.</summary>
    Task<IReadOnlyList<Oportunidad>> ListarAsync(CancellationToken cancellationToken = default);

    Task<Oportunidad?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Crea la oportunidad o, si ya existe una con el mismo Id, la actualiza.</summary>
    Task GuardarAsync(Oportunidad oportunidad, CancellationToken cancellationToken = default);

    Task EliminarAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Cuántas oportunidades tiene una empresa o un contacto (busca el Id en ambos campos).</summary>
    Task<int> ContarDeClienteAsync(Guid clienteId, CancellationToken cancellationToken = default);
}
