using Microsoft.Extensions.DependencyInjection;
using Vortex.Domain.Catalogo;
using Vortex.Domain.Configuracion;
using Vortex.Domain.Contactos;
using Vortex.Domain.Tareas;
using Vortex.Domain.Ventas;
using Vortex.Infrastructure.Catalogo;
using Vortex.Infrastructure.Configuracion;
using Vortex.Infrastructure.Contactos;
using Vortex.Infrastructure.Tareas;
using Vortex.Infrastructure.Ventas;
using Vortex.Infrastructure.Ventas.Pdf;

namespace Vortex.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>Registra los servicios de datos e integraciones de Vortex CRM.</summary>
    public static IServiceCollection AddVortexInfraestructura(this IServiceCollection services)
    {
        // TODO Fase 2: reemplazar por SQLite (app) / API con SQL Server
        services.AddSingleton<IRepositorioNegocio, RepositorioNegocioEnMemoria>();
        services.AddSingleton<IRepositorioContactos, RepositorioContactosEnMemoria>();
        services.AddSingleton<IRepositorioOportunidades, RepositorioOportunidadesEnMemoria>();
        services.AddSingleton<IRepositorioCotizaciones, RepositorioCotizacionesEnMemoria>();
        services.AddSingleton<IRepositorioProductos, RepositorioProductosEnMemoria>();
        services.AddSingleton<RepositorioTareasEnMemoria>();
        services.AddSingleton<IRepositorioTareas>(sp => new RepositorioTareasConRecordatorios(
            sp.GetRequiredService<RepositorioTareasEnMemoria>(),
            sp.GetRequiredService<IServicioRecordatorios>()));

        // Sin notificaciones del sistema; la app de Android registra las suyas después de esto
        services.AddSingleton<IServicioRecordatorios, SinRecordatorios>();

        services.AddSingleton<IGeneradorPdfCotizaciones, GeneradorPdfCotizaciones>();

        // TODO: reemplazar por un proveedor real de RUC/DNI cuando haya cuenta
        services.AddSingleton<IConsultaDocumentos>(new ConsultaDocumentosFalsa());

        return services;
    }
}
