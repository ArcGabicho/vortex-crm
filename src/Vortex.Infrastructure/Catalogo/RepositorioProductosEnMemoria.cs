using Vortex.Domain.Catalogo;

namespace Vortex.Infrastructure.Catalogo;

/// <summary>
/// Repositorio temporal en memoria: los datos se pierden al cerrar la app. Se
/// reemplazará por SQLite (app) y SQL Server (API) en la Fase 2.
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
                .Where(p => Coincide(filtro, p.Nombre, p.Codigo, p.Descripcion))
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

    private static bool Coincide(string? filtro, params string?[] campos) =>
        string.IsNullOrWhiteSpace(filtro)
        || campos.Any(c => c?.Contains(filtro.Trim(), StringComparison.CurrentCultureIgnoreCase) == true);
}
