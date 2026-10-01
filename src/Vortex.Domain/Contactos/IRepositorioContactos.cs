namespace Vortex.Domain.Contactos;

/// <summary>Almacenamiento de empresas y contactos.</summary>
public interface IRepositorioContactos
{
    /// <summary>Lista las empresas; el filtro busca en RUC, razón social y nombre comercial.</summary>
    Task<IReadOnlyList<Empresa>> ListarEmpresasAsync(string? filtro = null, CancellationToken cancellationToken = default);

    Task<Empresa?> ObtenerEmpresaAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Indica si ya hay otra empresa con ese RUC, sin contar la empresa <paramref name="excluirId"/>.</summary>
    Task<bool> ExisteRucAsync(string ruc, Guid? excluirId = null, CancellationToken cancellationToken = default);

    /// <summary>Crea la empresa o, si ya existe una con el mismo Id, la actualiza.</summary>
    Task GuardarEmpresaAsync(Empresa empresa, CancellationToken cancellationToken = default);

    /// <summary>Elimina la empresa y desvincula a sus contactos (los contactos no se borran).</summary>
    Task EliminarEmpresaAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Lista los contactos; el filtro busca en nombre, DNI, teléfono y correo.</summary>
    Task<IReadOnlyList<Contacto>> ListarContactosAsync(string? filtro = null, CancellationToken cancellationToken = default);

    Task<Contacto?> ObtenerContactoAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Crea el contacto o, si ya existe uno con el mismo Id, lo actualiza.</summary>
    Task GuardarContactoAsync(Contacto contacto, CancellationToken cancellationToken = default);

    Task EliminarContactoAsync(Guid id, CancellationToken cancellationToken = default);
}
