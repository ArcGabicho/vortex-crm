using Vortex.Domain.Contactos;
using Vortex.Domain.Ventas;

namespace Vortex.Shared.Servicios;

public static class NombresDeRegistros
{
    /// <summary>
    /// El nombre visible de cada empresa, contacto y oportunidad, por Id, para mostrar con
    /// qué está relacionada una tarea. Los Id no se repiten entre tipos, así que van juntos.
    /// </summary>
    public static async Task<IReadOnlyDictionary<Guid, string>> CargarAsync(
        IRepositorioContactos contactos, IRepositorioOportunidades oportunidades)
    {
        var nombres = new Dictionary<Guid, string>();

        foreach (var empresa in await contactos.ListarEmpresasAsync())
        {
            nombres[empresa.Id] = empresa.NombreVisible;
        }

        foreach (var contacto in await contactos.ListarContactosAsync())
        {
            nombres[contacto.Id] = contacto.NombreCompleto;
        }

        foreach (var oportunidad in await oportunidades.ListarAsync())
        {
            nombres[oportunidad.Id] = oportunidad.Titulo;
        }

        return nombres;
    }
}
