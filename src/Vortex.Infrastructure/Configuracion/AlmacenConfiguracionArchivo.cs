using System.Text.Json;
using System.Text.Json.Serialization;
using Vortex.Domain.Configuracion;

namespace Vortex.Infrastructure.Configuracion;

/// <summary>
/// La configuración de la instalación en un archivo JSON dentro de la carpeta de datos de la app
/// (en Windows instalada desde la Store, la carpeta privada del paquete). Si el archivo no existe,
/// la app arranca en modo Local.
/// </summary>
public sealed class AlmacenConfiguracionArchivo : IAlmacenConfiguracion
{
    private static readonly JsonSerializerOptions Opciones = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string ruta;
    private readonly Lock candado = new();

    public AlmacenConfiguracionArchivo(string ruta)
    {
        this.ruta = ruta;
        ModoEnUso = Cargar().Modo;
    }

    public ModoDatos ModoEnUso { get; }

    public ConfiguracionInstalacion Cargar()
    {
        lock (candado)
        {
            if (!File.Exists(ruta))
            {
                return new ConfiguracionInstalacion();
            }

            try
            {
                return JsonSerializer.Deserialize<ConfiguracionInstalacion>(File.ReadAllText(ruta), Opciones) ?? new ConfiguracionInstalacion();
            }
            catch (JsonException)
            {
                // Un archivo dañado no debe impedir abrir la app: se arranca en Local, como la primera vez
                return new ConfiguracionInstalacion();
            }
        }
    }

    public void Guardar(ConfiguracionInstalacion configuracion)
    {
        lock (candado)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ruta))!);

            // Se escribe a un temporal y se reemplaza, para no dejar el archivo a medias si se corta la luz
            var temporal = ruta + ".tmp";
            File.WriteAllText(temporal, JsonSerializer.Serialize(configuracion, Opciones));
            File.Move(temporal, ruta, overwrite: true);
        }
    }
}

/// <summary>Configuración que vive solo mientras la app está abierta (tests).</summary>
public sealed class AlmacenConfiguracionEnMemoria : IAlmacenConfiguracion
{
    private ConfiguracionInstalacion configuracion = new();

    public ModoDatos ModoEnUso => ModoDatos.Local;

    public ConfiguracionInstalacion Cargar() => configuracion.Clonar();

    public void Guardar(ConfiguracionInstalacion configuracion) => this.configuracion = configuracion.Clonar();
}
