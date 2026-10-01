using Vortex.Domain.Configuracion;

namespace Vortex.Domain.Ventas;

public interface IRepositorioCotizaciones
{
    /// <summary>Lista las cotizaciones, las más recientes primero.</summary>
    Task<IReadOnlyList<Cotizacion>> ListarAsync(CancellationToken cancellationToken = default);

    Task<Cotizacion?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Crea o actualiza la cotización. Al crearla le asigna el siguiente número correlativo.</summary>
    Task GuardarAsync(Cotizacion cotizacion, CancellationToken cancellationToken = default);

    Task EliminarAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Cuántas cotizaciones tiene una empresa o un contacto (busca el Id en ambos campos).</summary>
    Task<int> ContarDeClienteAsync(Guid clienteId, CancellationToken cancellationToken = default);

    /// <summary>Quita el vínculo con una oportunidad eliminada; las cotizaciones se conservan.</summary>
    Task DesvincularOportunidadAsync(Guid oportunidadId, CancellationToken cancellationToken = default);
}

public interface IGeneradorPdfCotizaciones
{
    /// <summary>Genera el PDF de la cotización listo para enviar al cliente.</summary>
    byte[] Generar(Cotizacion cotizacion, Negocio negocio, ClienteCotizacion cliente);
}
