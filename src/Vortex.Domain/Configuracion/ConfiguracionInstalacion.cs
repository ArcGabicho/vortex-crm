using System.Security.Cryptography;

namespace Vortex.Domain.Configuracion;

public enum ModoDatos
{
    /// <summary>Base de datos SQLite en el propio equipo: para un negocio de una sola sucursal, sin instalar nada más.</summary>
    Local,

    /// <summary>Los datos están en el servidor de Vortex (API con SQL Server) y los comparten todas las sucursales.</summary>
    Servidor,
}

/// <summary>
/// Cómo trabaja esta instalación de la app: dónde están los datos y a qué sucursal pertenece.
/// Se guarda en el propio equipo (no en la base de datos) porque se necesita antes de abrirla,
/// y solo la cambia quien tiene la clave de administrador.
/// </summary>
public sealed class ConfiguracionInstalacion
{
    private const int Iteraciones = 100_000;
    private const int LongitudSal = 16;
    private const int LongitudHash = 32;

    public ModoDatos Modo { get; set; } = ModoDatos.Local;

    /// <summary>Dirección del servidor de Vortex, en el modo <see cref="ModoDatos.Servidor"/>.</summary>
    public string? UrlServidor { get; set; }

    /// <summary>Sucursal a la que pertenece este equipo; <c>null</c> es el domicilio fiscal.</summary>
    public Guid? SucursalId { get; set; }

    /// <summary>Hash PBKDF2 (SHA-256) de la clave de administrador, en Base64. Nunca se guarda la clave.</summary>
    public string? ClaveHash { get; set; }

    public string? ClaveSal { get; set; }

    public bool TieneClave => ClaveHash is not null && ClaveSal is not null;

    public static IReadOnlyList<string> ValidarClaveNueva(string? clave, string? confirmacion)
    {
        var errores = new List<string>();

        if (clave is null || clave.Length < 6)
        {
            errores.Add("La clave debe tener al menos 6 caracteres.");
        }
        else if (clave != confirmacion)
        {
            errores.Add("Las claves no coinciden.");
        }

        return errores;
    }

    public void EstablecerClave(string clave)
    {
        var sal = RandomNumberGenerator.GetBytes(LongitudSal);
        ClaveSal = Convert.ToBase64String(sal);
        ClaveHash = Convert.ToBase64String(Derivar(clave, sal));
    }

    public bool VerificarClave(string? clave)
    {
        if (!TieneClave || string.IsNullOrEmpty(clave))
        {
            return false;
        }

        var esperado = Convert.FromBase64String(ClaveHash!);
        return CryptographicOperations.FixedTimeEquals(Derivar(clave, Convert.FromBase64String(ClaveSal!)), esperado);
    }

    public IReadOnlyList<string> Validar()
    {
        var errores = new List<string>();

        if (Modo == ModoDatos.Servidor
            && !(Uri.TryCreate(UrlServidor, UriKind.Absolute, out var url) && (url.Scheme == Uri.UriSchemeHttps || url.Scheme == Uri.UriSchemeHttp)))
        {
            errores.Add("Escribe la dirección completa del servidor, por ejemplo https://vortex.tunegocio.pe");
        }

        return errores;
    }

    public ConfiguracionInstalacion Clonar() => (ConfiguracionInstalacion)MemberwiseClone();

    private static byte[] Derivar(string clave, byte[] sal) =>
        Rfc2898DeriveBytes.Pbkdf2(clave, sal, Iteraciones, HashAlgorithmName.SHA256, LongitudHash);
}

/// <summary>Dónde vive la configuración de la instalación: un archivo en la carpeta de datos de la app.</summary>
public interface IAlmacenConfiguracion
{
    ConfiguracionInstalacion Cargar();

    void Guardar(ConfiguracionInstalacion configuracion);

    /// <summary>Con qué modo arrancó la app: un cambio de modo se aplica al volver a abrirla.</summary>
    ModoDatos ModoEnUso { get; }
}
