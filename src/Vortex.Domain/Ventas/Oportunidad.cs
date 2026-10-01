namespace Vortex.Domain.Ventas;

/// <summary>Una venta posible con un cliente, que avanza por las etapas del pipeline.</summary>
public sealed class Oportunidad
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Titulo { get; set; } = "";

    /// <summary>Empresa cliente, en ventas a negocios.</summary>
    public Guid? EmpresaId { get; set; }

    /// <summary>Persona de contacto; en ventas a personas es el único cliente.</summary>
    public Guid? ContactoId { get; set; }

    /// <summary>Monto estimado de la venta en soles, con IGV incluido.</summary>
    public decimal Monto { get; set; }

    public EtapaOportunidad Etapa { get; private set; } = EtapaOportunidad.Prospecto;

    public DateOnly? FechaCierreEstimada { get; set; }

    /// <summary>Por qué se perdió la venta. Solo aplica en la etapa Perdido.</summary>
    public string? MotivoPerdida { get; set; }

    public string? Notas { get; set; }

    public DateTimeOffset CreadoEn { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Cuándo pasó a Ganado o Perdido; vuelve a <c>null</c> si se reabre.</summary>
    public DateTimeOffset? CerradoEn { get; private set; }

    public void CambiarEtapa(EtapaOportunidad nueva, DateTimeOffset ahora)
    {
        if (nueva == Etapa)
        {
            return;
        }

        Etapa = nueva;
        CerradoEn = nueva.EstaAbierta() ? null : ahora;

        if (nueva != EtapaOportunidad.Perdido)
        {
            MotivoPerdida = null;
        }
    }

    /// <summary>Sigue abierta y ya pasó la fecha estimada de cierre.</summary>
    public bool EstaVencida(DateOnly hoy) => Etapa.EstaAbierta() && FechaCierreEstimada < hoy;

    public IReadOnlyList<string> Validar()
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(Titulo))
        {
            errores.Add("El título es obligatorio.");
        }

        if (EmpresaId is null && ContactoId is null)
        {
            errores.Add("Elige un cliente: una empresa, un contacto o ambos.");
        }

        if (Monto < 0)
        {
            errores.Add("El monto no puede ser negativo.");
        }

        return errores;
    }

    public Oportunidad Clonar() => (Oportunidad)MemberwiseClone();
}
