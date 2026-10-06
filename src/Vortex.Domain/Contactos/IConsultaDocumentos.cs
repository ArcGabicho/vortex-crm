namespace Vortex.Domain.Contactos;

/// <summary>
/// Consulta de datos públicos de un RUC (padrón de SUNAT) o un DNI (RENIEC)
/// a través de un proveedor externo.
/// </summary>
public interface IConsultaDocumentos
{
    /// <summary>Indica si los datos son inventados (proveedor de prueba) en lugar de venir de SUNAT/RENIEC.</summary>
    bool EsDePrueba => false;

    /// <summary>Devuelve los datos del RUC, o <c>null</c> si el proveedor no lo encuentra.</summary>
    Task<DatosRuc?> ConsultarRucAsync(string ruc, CancellationToken cancellationToken = default);

    /// <summary>Devuelve los datos del DNI, o <c>null</c> si el proveedor no lo encuentra.</summary>
    Task<DatosDni?> ConsultarDniAsync(string dni, CancellationToken cancellationToken = default);
}

public sealed record DatosRuc(
    string Ruc,
    string RazonSocial,
    string? NombreComercial,
    string? Direccion,
    string Estado,
    string Condicion,
    string? Ubigeo = null);

public sealed record DatosDni(
    string Dni,
    string Nombres,
    string ApellidoPaterno,
    string ApellidoMaterno)
{
    public string Apellidos => $"{ApellidoPaterno} {ApellidoMaterno}".Trim();
}
