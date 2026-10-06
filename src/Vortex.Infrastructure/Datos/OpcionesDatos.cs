using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vortex.Domain.Configuracion;
using Vortex.Infrastructure.Configuracion;

namespace Vortex.Infrastructure.Datos;

/// <summary>Dónde guarda los datos esta instalación y dónde está su configuración.</summary>
/// <param name="Base">La base de datos; <c>null</c> guarda en memoria (tests).</param>
public sealed record OpcionesDatos(IAlmacenConfiguracion Configuracion, IFabricaContexto? Base)
{
    /// <summary>Todo en memoria: se pierde al cerrar. Para los tests.</summary>
    public static OpcionesDatos EnMemoria() => new(new AlmacenConfiguracionEnMemoria(), null);

    /// <summary>
    /// Según la configuración de la instalación. En modo Local (o mientras el modo Servidor no
    /// esté disponible) los datos van a la base SQLite de <paramref name="rutaSqlite"/>.
    /// </summary>
    public static OpcionesDatos SegunConfiguracion(IAlmacenConfiguracion configuracion, string rutaSqlite) =>
        new(configuracion, FabricaContextoSqlite.ParaArchivo(rutaSqlite));
}

public static class BaseDeDatos
{
    /// <summary>
    /// Crea la base si no existe y le aplica las migraciones pendientes. Se llama al abrir la app,
    /// antes de mostrar la primera pantalla; con los datos en memoria no hace nada.
    /// </summary>
    public static void PrepararBaseDeDatos(this IServiceProvider servicios)
    {
        if (servicios.GetService<IFabricaContexto>() is { } fabrica)
        {
            using var db = fabrica.Crear();
            db.Database.Migrate();
        }
    }
}
