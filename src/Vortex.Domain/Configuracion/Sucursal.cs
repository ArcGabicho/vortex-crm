namespace Vortex.Domain.Configuracion;

/// <summary>
/// Un local del negocio. SUNAT lo llama establecimiento: el domicilio fiscal es el 0000 y cada
/// establecimiento anexo tiene su propio código de 4 dígitos, que se imprime en los comprobantes.
/// </summary>
public sealed class Sucursal
{
    public const string CodigoDomicilioFiscal = "0000";

    public Guid Id { get; init; } = Guid.NewGuid();

    public string Nombre { get; set; } = "";

    public string? Direccion { get; set; }

    /// <summary>Código de ubigeo del INEI del distrito (ver <see cref="Comun.Ubigeo"/>).</summary>
    public string? Ubigeo { get; set; }

    /// <summary>Código de establecimiento en SUNAT (4 dígitos).</summary>
    public string CodigoEstablecimiento { get; set; } = CodigoDomicilioFiscal;

    /// <summary>Las sucursales cerradas se conservan para el historial, pero ya no se eligen.</summary>
    public bool Activa { get; set; } = true;

    public bool EsDomicilioFiscal => CodigoEstablecimiento == CodigoDomicilioFiscal;

    public IReadOnlyList<string> Validar()
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(Nombre))
        {
            errores.Add("El nombre de la sucursal es obligatorio.");
        }

        if (CodigoEstablecimiento is not { Length: 4 } || !CodigoEstablecimiento.All(char.IsAsciiDigit))
        {
            errores.Add($"{NombreParaErrores}: el código de establecimiento debe tener 4 dígitos.");
        }

        if (Ubigeo is not null && !Comun.Ubigeo.EsCodigoValido(Ubigeo))
        {
            errores.Add($"{NombreParaErrores}: el distrito no es válido.");
        }

        return errores;
    }

    public Sucursal Clonar() => (Sucursal)MemberwiseClone();

    private string NombreParaErrores => string.IsNullOrWhiteSpace(Nombre) ? "Sucursal" : Nombre;
}
