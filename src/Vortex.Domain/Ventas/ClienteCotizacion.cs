using Vortex.Domain.Contactos;

namespace Vortex.Domain.Ventas;

/// <summary>Los datos del cliente tal como se muestran en la cotización.</summary>
public sealed record ClienteCotizacion(
    string Nombre,
    string? Documento,
    string? Direccion,
    string? Atencion,
    string? Telefono,
    string? Email)
{
    /// <summary>
    /// Arma los datos del cliente: con empresa, la cotización va a su razón social y RUC,
    /// "a la atención de" la persona de contacto; sin empresa, va a nombre de la persona.
    /// </summary>
    public static ClienteCotizacion Crear(Empresa? empresa, Contacto? contacto)
    {
        if (empresa is not null)
        {
            return new ClienteCotizacion(
                empresa.RazonSocial,
                $"RUC {empresa.Ruc}",
                empresa.Direccion,
                contacto?.NombreCompleto,
                contacto?.Telefono ?? empresa.Telefono,
                contacto?.Email ?? empresa.Email);
        }

        if (contacto is not null)
        {
            return new ClienteCotizacion(
                contacto.NombreCompleto,
                string.IsNullOrWhiteSpace(contacto.Dni) ? null : $"DNI {contacto.Dni}",
                null,
                null,
                contacto.Telefono,
                contacto.Email);
        }

        throw new ArgumentException("La cotización necesita una empresa o un contacto.");
    }
}
