using Vortex.Domain.Contactos;
using Vortex.Domain.Ventas;

namespace Vortex.Domain.Configuracion;

public enum RegimenTributario
{
    /// <summary>Nuevo Régimen Único Simplificado: no emite facturas ni cobra IGV.</summary>
    NuevoRus,

    /// <summary>Régimen Especial de Renta (RER).</summary>
    Especial,

    MypeTributario,

    General,
}

/// <summary>Los datos del emprendedor que usa Vortex CRM: aparecen en sus cotizaciones.</summary>
public sealed class Negocio
{
    public string? Ruc { get; set; }

    public string RazonSocial { get; set; } = "";

    public string? NombreComercial { get; set; }

    public string? Direccion { get; set; }

    public string? Telefono { get; set; }

    public string? Email { get; set; }

    public RegimenTributario Regimen { get; set; } = RegimenTributario.MypeTributario;

    /// <summary>Condiciones que se copian en cada cotización nueva (forma de pago, entrega, etc.).</summary>
    public string? CondicionesPredeterminadas { get; set; }

    public string NombreVisible =>
        string.IsNullOrWhiteSpace(NombreComercial) ? RazonSocial : NombreComercial;

    public bool EstaConfigurado => !string.IsNullOrWhiteSpace(RazonSocial);

    /// <summary>En el Nuevo RUS no se cobra IGV; en los demás regímenes los precios suelen incluirlo.</summary>
    public ModoIgv ModoIgvPredeterminado => Regimen == RegimenTributario.NuevoRus ? ModoIgv.NoAplica : ModoIgv.Incluido;

    public IReadOnlyList<string> Validar()
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(RazonSocial))
        {
            errores.Add("El nombre o razón social es obligatorio.");
        }

        if (!string.IsNullOrWhiteSpace(Ruc) && !DocumentoIdentidad.EsRucValido(Ruc))
        {
            errores.Add("El RUC no es válido.");
        }

        return errores;
    }

    public Negocio Clonar() => (Negocio)MemberwiseClone();
}

public static class Regimenes
{
    public static IReadOnlyList<RegimenTributario> Todos { get; } = Enum.GetValues<RegimenTributario>();

    public static string Nombre(this RegimenTributario regimen) => regimen switch
    {
        RegimenTributario.NuevoRus => "Nuevo RUS",
        RegimenTributario.Especial => "Régimen Especial (RER)",
        RegimenTributario.MypeTributario => "Régimen MYPE Tributario",
        _ => "Régimen General",
    };
}

public interface IRepositorioNegocio
{
    /// <summary>Devuelve los datos del negocio; si todavía no se configuraron, vienen vacíos.</summary>
    Task<Negocio> ObtenerAsync(CancellationToken cancellationToken = default);

    Task GuardarAsync(Negocio negocio, CancellationToken cancellationToken = default);
}
