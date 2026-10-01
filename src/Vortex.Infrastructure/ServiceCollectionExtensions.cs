using Microsoft.Extensions.DependencyInjection;
using Vortex.Domain.Contactos;
using Vortex.Domain.Ventas;
using Vortex.Infrastructure.Contactos;
using Vortex.Infrastructure.Ventas;

namespace Vortex.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>Registra los servicios de datos e integraciones de Vortex CRM.</summary>
    public static IServiceCollection AddVortexInfraestructura(this IServiceCollection services)
    {
        // TODO Fase 2: reemplazar por SQLite (app) / API con SQL Server
        services.AddSingleton<IRepositorioContactos, RepositorioContactosEnMemoria>();
        services.AddSingleton<IRepositorioOportunidades, RepositorioOportunidadesEnMemoria>();

        // TODO: reemplazar por un proveedor real de RUC/DNI cuando haya cuenta
        services.AddSingleton<IConsultaDocumentos>(new ConsultaDocumentosFalsa());

        return services;
    }
}
