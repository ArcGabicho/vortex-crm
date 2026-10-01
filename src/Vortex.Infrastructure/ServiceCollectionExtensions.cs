using Microsoft.Extensions.DependencyInjection;
using Vortex.Domain.Contactos;
using Vortex.Infrastructure.Contactos;

namespace Vortex.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>Registra los servicios de datos e integraciones de Vortex CRM.</summary>
    public static IServiceCollection AddVortexInfraestructura(this IServiceCollection services)
    {
        // TODO Fase 2: reemplazar por SQLite (app) / API con SQL Server
        services.AddSingleton<IRepositorioContactos, RepositorioContactosEnMemoria>();

        // TODO: reemplazar por un proveedor real de RUC/DNI cuando haya cuenta
        services.AddSingleton<IConsultaDocumentos>(new ConsultaDocumentosFalsa());

        return services;
    }
}
