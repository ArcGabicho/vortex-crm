using Microsoft.Extensions.DependencyInjection;
using Vortex.Domain.Catalogo;
using Vortex.Domain.Configuracion;
using Vortex.Domain.Contactos;
using Vortex.Domain.Tareas;
using Vortex.Domain.Ventas;
using Vortex.Infrastructure.Catalogo;
using Vortex.Infrastructure.Comun;
using Vortex.Infrastructure.Configuracion;
using Vortex.Infrastructure.Contactos;
using Vortex.Infrastructure.Datos;
using Vortex.Infrastructure.Tareas;
using Vortex.Infrastructure.Ventas;
using Vortex.Infrastructure.Ventas.Pdf;

namespace Vortex.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra los servicios de datos e integraciones de Vortex CRM. Sin <paramref name="datos"/>
    /// todo queda en memoria (tests); la app y la web pasan la base de datos de la instalación.
    /// </summary>
    public static IServiceCollection AddVortexInfraestructura(this IServiceCollection services, OpcionesDatos? datos = null)
    {
        datos ??= OpcionesDatos.EnMemoria();
        services.AddSingleton(datos.Configuracion);

        if (datos.Base is { } fabrica)
        {
            services.AddSingleton(fabrica);
            services.AddSingleton<IRepositorioNegocio, RepositorioNegocioSql>();
            services.AddSingleton<IRepositorioContactos, RepositorioContactosSql>();
            services.AddSingleton<IRepositorioOportunidades, RepositorioOportunidadesSql>();
            services.AddSingleton<IRepositorioCotizaciones, RepositorioCotizacionesSql>();
            services.AddSingleton<IRepositorioProductos, RepositorioProductosSql>();
            services.AddSingleton<RepositorioTareasSql>();
            services.AddSingleton<IRepositorioTareas>(sp => new RepositorioTareasConRecordatorios(
                sp.GetRequiredService<RepositorioTareasSql>(),
                sp.GetRequiredService<IServicioRecordatorios>()));
        }
        else
        {
            services.AddSingleton<IRepositorioNegocio, RepositorioNegocioEnMemoria>();
            services.AddSingleton<IRepositorioContactos, RepositorioContactosEnMemoria>();
            services.AddSingleton<IRepositorioOportunidades, RepositorioOportunidadesEnMemoria>();
            services.AddSingleton<IRepositorioCotizaciones, RepositorioCotizacionesEnMemoria>();
            services.AddSingleton<IRepositorioProductos, RepositorioProductosEnMemoria>();
            services.AddSingleton<RepositorioTareasEnMemoria>();
            services.AddSingleton<IRepositorioTareas>(sp => new RepositorioTareasConRecordatorios(
                sp.GetRequiredService<RepositorioTareasEnMemoria>(),
                sp.GetRequiredService<IServicioRecordatorios>()));
        }

        // Sin notificaciones del sistema; la app de Android registra las suyas después de esto
        services.AddSingleton<IServicioRecordatorios, SinRecordatorios>();

        // Viene dentro de la app: se lee una sola vez, la primera vez que se necesita
        services.AddSingleton(_ => UbigeosInei.Cargar());

        services.AddSingleton<IGeneradorPdfCotizaciones, GeneradorPdfCotizaciones>();

        // TODO: reemplazar por un proveedor real de RUC/DNI cuando haya cuenta
        services.AddSingleton<IConsultaDocumentos>(new ConsultaDocumentosFalsa());

        return services;
    }
}
