using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vortex.Infrastructure.Datos;

namespace Vortex.Domain.Tests.Infraestructura;

/// <summary>
/// Una base SQLite en memoria con las migraciones aplicadas, nueva para cada test. Vive mientras
/// la conexión esté abierta.
/// </summary>
public sealed class BaseSqliteDePrueba : IDisposable
{
    private readonly SqliteConnection conexion = new("DataSource=:memory:");

    public BaseSqliteDePrueba()
    {
        conexion.Open();
        Fabrica = FabricaContextoSqlite.ParaConexion(conexion);
        using var db = Fabrica.Crear();
        db.Database.Migrate();
    }

    public FabricaContextoSqlite Fabrica { get; }

    public void Dispose() => conexion.Dispose();
}
