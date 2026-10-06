namespace Vortex.Domain.Contactos;

/// <summary>Un negocio cliente o prospecto, identificado por su RUC.</summary>
public sealed class Empresa
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Ruc { get; set; } = "";

    public string RazonSocial { get; set; } = "";

    public string? NombreComercial { get; set; }

    public string? Direccion { get; set; }

    /// <summary>Código de ubigeo del INEI del distrito de la dirección fiscal (ver <see cref="Comun.Ubigeo"/>).</summary>
    public string? Ubigeo { get; set; }

    public string? Telefono { get; set; }

    public string? Email { get; set; }

    /// <summary>Estado del contribuyente según SUNAT (ACTIVO, BAJA DE OFICIO, etc.).</summary>
    public string? EstadoSunat { get; set; }

    /// <summary>Condición del domicilio fiscal según SUNAT (HABIDO, NO HABIDO, etc.).</summary>
    public string? CondicionSunat { get; set; }

    public DateTimeOffset CreadoEn { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>El nombre comercial si existe; si no, la razón social.</summary>
    public string NombreVisible =>
        string.IsNullOrWhiteSpace(NombreComercial) ? RazonSocial : NombreComercial;

    public IReadOnlyList<string> Validar()
    {
        var errores = new List<string>();

        if (!DocumentoIdentidad.EsRucValido(Ruc))
        {
            errores.Add("El RUC no es válido.");
        }

        if (string.IsNullOrWhiteSpace(RazonSocial))
        {
            errores.Add("La razón social es obligatoria.");
        }

        if (Ubigeo is not null && !Comun.Ubigeo.EsCodigoValido(Ubigeo))
        {
            errores.Add("El distrito no es válido.");
        }

        return errores;
    }

    public Empresa Clonar() => (Empresa)MemberwiseClone();
}
