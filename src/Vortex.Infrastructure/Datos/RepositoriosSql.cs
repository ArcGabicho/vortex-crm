using Microsoft.EntityFrameworkCore;
using Vortex.Domain.Catalogo;
using Vortex.Domain.Configuracion;
using Vortex.Domain.Contactos;
using Vortex.Domain.Tareas;
using Vortex.Domain.Ventas;
using Vortex.Infrastructure.Comun;

namespace Vortex.Infrastructure.Datos;

// Repositorios sobre la base de datos (SQLite o SQL Server). Se comportan igual que los de
// memoria: devuelven objetos nuevos, así que editar uno en un formulario no cambia nada hasta
// Guardar. Las búsquedas por texto y los órdenes por nombre se hacen en memoria, con las reglas
// del español, para que den lo mismo en cualquier motor; los volúmenes de un negocio lo permiten.

internal static class Guardado
{
    /// <summary>Inserta la entidad o, si ya existe, copia sus valores sobre la guardada.</summary>
    public static async Task GuardarAsync<T>(this VortexDbContext db, T entidad, Guid id, CancellationToken cancellationToken)
        where T : class
    {
        var existente = await db.Set<T>().FindAsync([id], cancellationToken);
        if (existente is null)
        {
            db.Add(entidad);
        }
        else
        {
            db.Entry(existente).CurrentValues.SetValues(entidad);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class RepositorioNegocioSql(IFabricaContexto fabrica) : IRepositorioNegocio
{
    public async Task<Negocio> ObtenerAsync(CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        var negocio = await db.Negocios.AsNoTracking().FirstOrDefaultAsync(cancellationToken) ?? new Negocio();
        negocio.OrdenarSucursales();
        return negocio;
    }

    public async Task GuardarAsync(Negocio negocio, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        var copia = negocio.Clonar();
        var existente = await db.Negocios.FirstOrDefaultAsync(cancellationToken);

        if (existente is null)
        {
            db.Add(copia);
            db.Entry(copia).Property("Id").CurrentValue = VortexDbContext.IdNegocio;
        }
        else
        {
            db.Entry(existente).CurrentValues.SetValues(copia);
            SincronizarSucursales(db, existente.Sucursales, copia.Sucursales);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Actualiza las sucursales una por una: EF no permite cambiar una por otra con el mismo Id.</summary>
    private static void SincronizarSucursales(VortexDbContext db, List<Sucursal> guardadas, List<Sucursal> nuevas)
    {
        guardadas.RemoveAll(g => nuevas.All(n => n.Id != g.Id));

        foreach (var nueva in nuevas)
        {
            var guardada = guardadas.Find(g => g.Id == nueva.Id);
            if (guardada is null)
            {
                guardadas.Add(nueva);
            }
            else
            {
                db.Entry(guardada).CurrentValues.SetValues(nueva);
            }
        }
    }
}

public sealed class RepositorioContactosSql(IFabricaContexto fabrica) : IRepositorioContactos
{
    public async Task<IReadOnlyList<Empresa>> ListarEmpresasAsync(string? filtro = null, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        var empresas = await db.Empresas.AsNoTracking().ToListAsync(cancellationToken);
        return empresas
            .Where(e => Filtro.Coincide(filtro, e.Ruc, e.RazonSocial, e.NombreComercial))
            .OrderBy(e => e.NombreVisible, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<Empresa?> ObtenerEmpresaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        return await db.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<bool> ExisteRucAsync(string ruc, Guid? excluirId = null, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        return await db.Empresas.AnyAsync(e => e.Ruc == ruc && e.Id != excluirId, cancellationToken);
    }

    public async Task GuardarEmpresaAsync(Empresa empresa, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        await db.GuardarAsync(empresa.Clonar(), empresa.Id, cancellationToken);
    }

    public async Task EliminarEmpresaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);

        await db.Contactos.Where(c => c.EmpresaId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.EmpresaId, (Guid?)null), cancellationToken);
        await db.Empresas.Where(e => e.Id == id).ExecuteDeleteAsync(cancellationToken);

        await transaccion.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Contacto>> ListarContactosAsync(string? filtro = null, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        var contactos = await db.Contactos.AsNoTracking().ToListAsync(cancellationToken);
        return contactos
            .Where(c => Filtro.Coincide(filtro, c.NombreCompleto, c.Dni, c.Telefono, c.Email))
            .OrderBy(c => c.NombreCompleto, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<Contacto?> ObtenerContactoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        return await db.Contactos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task GuardarContactoAsync(Contacto contacto, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        await db.GuardarAsync(contacto.Clonar(), contacto.Id, cancellationToken);
    }

    public async Task EliminarContactoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        await db.Contactos.Where(c => c.Id == id).ExecuteDeleteAsync(cancellationToken);
    }
}

public sealed class RepositorioOportunidadesSql(IFabricaContexto fabrica) : IRepositorioOportunidades
{
    public async Task<IReadOnlyList<Oportunidad>> ListarAsync(CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        var oportunidades = await db.Oportunidades.AsNoTracking().ToListAsync(cancellationToken);
        return oportunidades
            .OrderBy(o => o.FechaCierreEstimada is null)
            .ThenBy(o => o.FechaCierreEstimada)
            .ThenBy(o => o.CreadoEn)
            .ToList();
    }

    public async Task<Oportunidad?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        return await db.Oportunidades.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task GuardarAsync(Oportunidad oportunidad, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        await db.GuardarAsync(oportunidad.Clonar(), oportunidad.Id, cancellationToken);
    }

    public async Task EliminarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        await db.Oportunidades.Where(o => o.Id == id).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> ContarDeClienteAsync(Guid clienteId, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        return await db.Oportunidades.CountAsync(o => o.EmpresaId == clienteId || o.ContactoId == clienteId, cancellationToken);
    }
}

public sealed class RepositorioCotizacionesSql(IFabricaContexto fabrica) : IRepositorioCotizaciones
{
    public async Task<IReadOnlyList<Cotizacion>> ListarAsync(CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        return await db.Cotizaciones.AsNoTracking().OrderByDescending(c => c.Numero).ToListAsync(cancellationToken);
    }

    public async Task<Cotizacion?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        return await db.Cotizaciones.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task GuardarAsync(Cotizacion cotizacion, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);

        if (cotizacion.Numero == 0)
        {
            cotizacion.Numero = (await db.Cotizaciones.MaxAsync(c => (int?)c.Numero, cancellationToken) ?? 0) + 1;
        }

        await db.GuardarAsync(cotizacion.Clonar(), cotizacion.Id, cancellationToken);
        await transaccion.CommitAsync(cancellationToken);
    }

    public async Task EliminarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        await db.Cotizaciones.Where(c => c.Id == id).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> ContarDeClienteAsync(Guid clienteId, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        return await db.Cotizaciones.CountAsync(c => c.EmpresaId == clienteId || c.ContactoId == clienteId, cancellationToken);
    }

    public async Task DesvincularOportunidadAsync(Guid oportunidadId, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        await db.Cotizaciones.Where(c => c.OportunidadId == oportunidadId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.OportunidadId, (Guid?)null), cancellationToken);
    }
}

public sealed class RepositorioProductosSql(IFabricaContexto fabrica) : IRepositorioProductos
{
    public async Task<IReadOnlyList<Producto>> ListarAsync(string? filtro = null, bool incluirInactivos = false, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        var productos = await db.Productos.AsNoTracking()
            .Where(p => incluirInactivos || p.Activo)
            .ToListAsync(cancellationToken);
        return productos
            .Where(p => Filtro.Coincide(filtro, p.Nombre, p.Codigo, p.Descripcion))
            .OrderBy(p => p.Nombre, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<Producto?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        return await db.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<bool> ExisteCodigoAsync(string codigo, Guid? excluirId = null, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        var codigos = await db.Productos.Where(p => p.Id != excluirId && p.Codigo != null)
            .Select(p => p.Codigo!)
            .ToListAsync(cancellationToken);
        return codigos.Any(c => string.Equals(c.Trim(), codigo.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task GuardarAsync(Producto producto, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        await db.GuardarAsync(producto.Clonar(), producto.Id, cancellationToken);
    }

    public async Task EliminarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        await db.Productos.Where(p => p.Id == id).ExecuteDeleteAsync(cancellationToken);
    }
}

public sealed class RepositorioTareasSql(IFabricaContexto fabrica) : IRepositorioTareas
{
    public async Task<IReadOnlyList<Tarea>> ListarAsync(CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        return (await db.Tareas.AsNoTracking().ToListAsync(cancellationToken)).EnOrden().ToList();
    }

    public async Task<IReadOnlyList<Tarea>> ListarDeAsync(Guid relacionadoId, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        var tareas = await db.Tareas.AsNoTracking()
            .Where(t => t.EmpresaId == relacionadoId || t.ContactoId == relacionadoId || t.OportunidadId == relacionadoId)
            .ToListAsync(cancellationToken);
        return tareas.EnOrden().ToList();
    }

    public async Task<Tarea?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        return await db.Tareas.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task GuardarAsync(Tarea tarea, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        await db.GuardarAsync(tarea.Clonar(), tarea.Id, cancellationToken);
    }

    public async Task EliminarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        await db.Tareas.Where(t => t.Id == id).ExecuteDeleteAsync(cancellationToken);
    }

    public async Task DesvincularAsync(Guid relacionadoId, CancellationToken cancellationToken = default)
    {
        await using var db = fabrica.Crear();
        await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);

        await db.Tareas.Where(t => t.EmpresaId == relacionadoId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.EmpresaId, (Guid?)null), cancellationToken);
        await db.Tareas.Where(t => t.ContactoId == relacionadoId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ContactoId, (Guid?)null), cancellationToken);
        await db.Tareas.Where(t => t.OportunidadId == relacionadoId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.OportunidadId, (Guid?)null), cancellationToken);

        await transaccion.CommitAsync(cancellationToken);
    }
}
