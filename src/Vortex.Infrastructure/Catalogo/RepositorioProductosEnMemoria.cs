using Vortex.Domain.Catalogo;
using Vortex.Infrastructure.Comun;

namespace Vortex.Infrastructure.Catalogo;

/// <summary>
/// Repositorio en memoria para los tests: los datos se pierden al cerrar. La app y la web
/// usan la base de datos (ver Datos/RepositoriosSql.cs).
/// </summary>
/// <remarks>Guarda y devuelve copias, igual que los demás repositorios en memoria.</remarks>
public sealed class RepositorioProductosEnMemoria : IRepositorioProductos
{
    private readonly Lock candado = new();
    private readonly Dictionary<Guid, Producto> productos = [];

    public Task<IReadOnlyList<Producto>> ListarAsync(string? filtro = null, bool incluirInactivos = false, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            IReadOnlyList<Producto> resultado = productos.Values
                .Where(p => incluirInactivos || p.Activo)
                .Where(p => Filtro.Coincide(filtro, p.Nombre, p.Codigo, p.Descripcion))
                .OrderBy(p => p.Nombre, StringComparer.CurrentCultureIgnoreCase)
                .Select(p => p.Clonar())
                .ToList();
            return Task.FromResult(resultado);
        }
    }

    public Task<Producto?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            return Task.FromResult(productos.GetValueOrDefault(id)?.Clonar());
        }
    }

    public Task<bool> ExisteCodigoAsync(string codigo, Guid? excluirId = null, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            return Task.FromResult(productos.Values.Any(p =>
                p.Id != excluirId && string.Equals(p.Codigo?.Trim(), codigo.Trim(), StringComparison.OrdinalIgnoreCase)));
        }
    }

    public Task GuardarAsync(Producto producto, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            productos[producto.Id] = producto.Clonar();
        }

        return Task.CompletedTask;
    }

    public Task EliminarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (candado)
        {
            productos.Remove(id);
        }

        return Task.CompletedTask;
    }
}
