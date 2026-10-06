using Vortex.Domain.Contactos;
using Vortex.Infrastructure.Comun;

namespace Vortex.Infrastructure.Contactos;

/// <summary>
/// Repositorio en memoria para los tests: los datos se pierden al cerrar. La app y la web
/// usan la base de datos (ver Datos/RepositoriosSql.cs).
/// </summary>
/// <remarks>
/// Guarda y devuelve copias, para que editar un objeto en un formulario no cambie
/// los datos hasta que se llame a Guardar.
/// </remarks>
public sealed class RepositorioContactosEnMemoria : IRepositorioContactos
{
    private readonly Lock candado = new();
    private readonly Dictionary<Guid, Empresa> empresas = [];
    private readonly Dictionary<Guid, Contacto> contactos = [];

    public Task<IReadOnlyList<Empresa>> ListarEmpresasAsync(string? filtro = null, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            IReadOnlyList<Empresa> resultado = empresas.Values
                .Where(e => Filtro.Coincide(filtro, e.Ruc, e.RazonSocial, e.NombreComercial))
                .OrderBy(e => e.NombreVisible, StringComparer.CurrentCultureIgnoreCase)
                .Select(e => e.Clonar())
                .ToList();
            return Task.FromResult(resultado);
        }
    }

    public Task<Empresa?> ObtenerEmpresaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            return Task.FromResult(empresas.GetValueOrDefault(id)?.Clonar());
        }
    }

    public Task<bool> ExisteRucAsync(string ruc, Guid? excluirId = null, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            return Task.FromResult(empresas.Values.Any(e => e.Ruc == ruc && e.Id != excluirId));
        }
    }

    public Task GuardarEmpresaAsync(Empresa empresa, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            empresas[empresa.Id] = empresa.Clonar();
        }

        return Task.CompletedTask;
    }

    public Task EliminarEmpresaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            empresas.Remove(id);
            foreach (var contacto in contactos.Values.Where(c => c.EmpresaId == id))
            {
                contacto.EmpresaId = null;
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Contacto>> ListarContactosAsync(string? filtro = null, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            IReadOnlyList<Contacto> resultado = contactos.Values
                .Where(c => Filtro.Coincide(filtro, c.NombreCompleto, c.Dni, c.Telefono, c.Email))
                .OrderBy(c => c.NombreCompleto, StringComparer.CurrentCultureIgnoreCase)
                .Select(c => c.Clonar())
                .ToList();
            return Task.FromResult(resultado);
        }
    }

    public Task<Contacto?> ObtenerContactoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            return Task.FromResult(contactos.GetValueOrDefault(id)?.Clonar());
        }
    }

    public Task GuardarContactoAsync(Contacto contacto, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            contactos[contacto.Id] = contacto.Clonar();
        }

        return Task.CompletedTask;
    }

    public Task EliminarContactoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            contactos.Remove(id);
        }

        return Task.CompletedTask;
    }
}
