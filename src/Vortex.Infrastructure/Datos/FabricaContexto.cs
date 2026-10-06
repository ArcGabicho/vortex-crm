using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Vortex.Infrastructure.Datos;

/// <summary>
/// Crea un contexto nuevo para cada operación: Blazor atiende varias pantallas a la vez y un
/// DbContext no se puede compartir entre hilos.
/// </summary>
public interface IFabricaContexto
{
    VortexDbContext Crear();
}

public sealed class FabricaContextoSqlite(DbContextOptions<VortexDbContextSqlite> opciones) : IFabricaContexto
{
    /// <summary>Base de datos en un archivo; la carpeta se crea si no existe.</summary>
    public static FabricaContextoSqlite ParaArchivo(string ruta)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ruta))!);
        var cadena = new SqliteConnectionStringBuilder { DataSource = ruta, ForeignKeys = true }.ToString();
        return new FabricaContextoSqlite(new DbContextOptionsBuilder<VortexDbContextSqlite>().UseSqlite(cadena).Options);
    }

    /// <summary>Sobre una conexión ya abierta (los tests usan una base en memoria que vive mientras la conexión esté abierta).</summary>
    public static FabricaContextoSqlite ParaConexion(SqliteConnection conexion) =>
        new(new DbContextOptionsBuilder<VortexDbContextSqlite>().UseSqlite(conexion).Options);

    public VortexDbContext Crear() => new VortexDbContextSqlite(opciones);
}

/// <summary>Para <c>dotnet ef migrations add</c>: las migraciones de SQLite se generan contra un archivo cualquiera.</summary>
public sealed class FabricaContextoSqliteDiseno : IDesignTimeDbContextFactory<VortexDbContextSqlite>
{
    public VortexDbContextSqlite CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<VortexDbContextSqlite>().UseSqlite("Data Source=vortex-diseno.db").Options);
}
