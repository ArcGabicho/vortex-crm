using Vortex.Domain.Ventas;

namespace Vortex.Domain.Catalogo;

public enum TipoProducto
{
    Producto,
    Servicio,
}

/// <summary>Un producto o servicio que el negocio vende, listo para agregarse a una cotización.</summary>
public sealed class Producto
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public TipoProducto Tipo { get; set; } = TipoProducto.Producto;

    public string Nombre { get; set; } = "";

    /// <summary>Código interno o SKU, opcional.</summary>
    public string? Codigo { get; set; }

    /// <summary>Texto que va en la cotización; si está vacío se usa el nombre.</summary>
    public string? Descripcion { get; set; }

    public string Unidad { get; set; } = "UND";

    public decimal Precio { get; set; }

    /// <summary>Si el precio registrado ya incluye el IGV (lo usual al vender a personas).</summary>
    public bool PrecioIncluyeIgv { get; set; } = true;

    /// <summary>Los inactivos no aparecen al cotizar, pero se conservan.</summary>
    public bool Activo { get; set; } = true;

    public string TextoParaCotizacion => string.IsNullOrWhiteSpace(Descripcion) ? Nombre : Descripcion;

    /// <summary>
    /// El precio unitario expresado como lo espera una cotización con ese modo de IGV: se
    /// suma o se quita el 18 % según haga falta. Sin IGV (Nuevo RUS), el precio es el que es.
    /// </summary>
    public decimal PrecioPara(ModoIgv modo) => modo switch
    {
        ModoIgv.Incluido when !PrecioIncluyeIgv => Redondear(Precio * (1 + Impuestos.TasaIgv)),
        ModoIgv.Adicional when PrecioIncluyeIgv => Redondear(Precio / (1 + Impuestos.TasaIgv)),
        _ => Precio,
    };

    /// <summary>Una línea de cotización con los datos del producto copiados (no cambia si luego se edita el producto).</summary>
    public LineaCotizacion CrearLinea(ModoIgv modo, decimal cantidad = 1) => new()
    {
        ProductoId = Id,
        Descripcion = TextoParaCotizacion,
        Unidad = Unidad,
        Cantidad = cantidad,
        PrecioUnitario = PrecioPara(modo),
    };

    public IReadOnlyList<string> Validar()
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(Nombre))
        {
            errores.Add("El nombre es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(Unidad))
        {
            errores.Add("La unidad es obligatoria.");
        }

        if (Precio < 0)
        {
            errores.Add("El precio no puede ser negativo.");
        }

        return errores;
    }

    public Producto Clonar() => (Producto)MemberwiseClone();

    private static decimal Redondear(decimal monto) => Math.Round(monto, 2, MidpointRounding.AwayFromZero);
}

public sealed record UnidadMedida(string Codigo, string Nombre);

public static class Unidades
{
    public static IReadOnlyList<UnidadMedida> Comunes { get; } =
    [
        new("UND", "Unidad"),
        new("SERV", "Servicio"),
        new("HORA", "Hora"),
        new("KG", "Kilogramo"),
        new("M", "Metro"),
        new("M2", "Metro cuadrado"),
        new("L", "Litro"),
        new("CJ", "Caja"),
        new("PQ", "Paquete"),
        new("DOC", "Docena"),
        new("MLL", "Millar"),
    ];

    /// <summary>La unidad que se propone al elegir el tipo.</summary>
    public static string PredeterminadaPara(TipoProducto tipo) => tipo == TipoProducto.Servicio ? "SERV" : "UND";
}

public interface IRepositorioProductos
{
    /// <summary>
    /// Lista los productos por nombre; el filtro busca en nombre, código y descripción.
    /// Los inactivos solo aparecen si se piden.
    /// </summary>
    Task<IReadOnlyList<Producto>> ListarAsync(string? filtro = null, bool incluirInactivos = false, CancellationToken cancellationToken = default);

    Task<Producto?> ObtenerAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Indica si otro producto ya usa ese código, sin contar el producto <paramref name="excluirId"/>.</summary>
    Task<bool> ExisteCodigoAsync(string codigo, Guid? excluirId = null, CancellationToken cancellationToken = default);

    Task GuardarAsync(Producto producto, CancellationToken cancellationToken = default);

    /// <summary>Las cotizaciones guardan una copia de cada línea, así que eliminar un producto no las afecta.</summary>
    Task EliminarAsync(Guid id, CancellationToken cancellationToken = default);
}
