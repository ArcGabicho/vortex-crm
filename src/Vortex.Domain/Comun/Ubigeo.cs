using System.Globalization;
using System.Text;

namespace Vortex.Domain.Comun;

/// <summary>
/// Un distrito del Perú con su código de ubigeo del INEI, el mismo que pide SUNAT en los
/// comprobantes: 2 dígitos de departamento, 2 de provincia y 2 de distrito (Miraflores, Lima = 150122).
/// </summary>
public sealed record Ubigeo(string Codigo, string Departamento, string Provincia, string Distrito)
{
    /// <summary>Del distrito al departamento, como se escribe después de la dirección: <c>MIRAFLORES - LIMA - LIMA</c>.</summary>
    public string Descripcion => $"{Distrito} - {Provincia} - {Departamento}";

    public static bool EsCodigoValido(string? codigo) => codigo is { Length: 6 } && codigo.All(char.IsAsciiDigit);
}

/// <summary>Los ubigeos del Perú, con búsqueda por nombre de distrito, provincia o departamento.</summary>
public sealed class CatalogoUbigeos
{
    private readonly Dictionary<string, Ubigeo> porCodigo;
    private readonly Entrada[] entradas;

    public CatalogoUbigeos(IEnumerable<Ubigeo> ubigeos)
    {
        Todos = ubigeos.OrderBy(u => u.Codigo, StringComparer.Ordinal).ToList();
        porCodigo = Todos.ToDictionary(u => u.Codigo);
        entradas = Todos
            .Select(u => new Entrada(u, Normalizar(u.Distrito), Normalizar($"{u.Distrito} {u.Provincia} {u.Departamento}")))
            .ToArray();
    }

    public IReadOnlyList<Ubigeo> Todos { get; }

    public Ubigeo? Obtener(string? codigo) => codigo is null ? null : porCodigo.GetValueOrDefault(codigo);

    /// <summary>
    /// Busca sin distinguir mayúsculas ni tildes ("brena" encuentra BREÑA). Cada palabra debe
    /// aparecer en el distrito, la provincia o el departamento, así "miraflores arequipa" separa
    /// los distritos del mismo nombre. Primero van los distritos que se llaman así o empiezan así.
    /// Con solo números busca por código.
    /// </summary>
    public IReadOnlyList<Ubigeo> Buscar(string? texto, int maximo = 20)
    {
        var consulta = Normalizar(texto ?? "");
        if (consulta.Length == 0)
        {
            return [];
        }

        if (consulta.All(char.IsAsciiDigit))
        {
            return Todos.Where(u => u.Codigo.StartsWith(consulta, StringComparison.Ordinal)).Take(maximo).ToList();
        }

        var palabras = consulta.Split(' ');
        return entradas
            .Where(e => palabras.All(p => ContienePalabra(e.Todo, p)))
            .OrderBy(e => Prioridad(e, consulta, palabras[0]))
            .ThenBy(e => e.Ubigeo.Codigo, StringComparer.Ordinal)
            .Take(maximo)
            .Select(e => e.Ubigeo)
            .ToList();
    }

    /// <summary>La dirección seguida del distrito, la provincia y el departamento, para los documentos.</summary>
    public string? DireccionCompleta(string? direccion, string? codigoUbigeo)
    {
        var ubigeo = Obtener(codigoUbigeo);
        var calle = string.IsNullOrWhiteSpace(direccion) ? null : direccion.Trim();

        return (calle, ubigeo) switch
        {
            (null, null) => null,
            (_, null) => calle,
            (null, _) => ubigeo.Descripcion,
            _ => $"{calle}, {ubigeo.Descripcion}",
        };
    }

    /// <summary>
    /// Mayúsculas sin tildes (la Ñ queda como N) y con un solo espacio entre palabras; guiones,
    /// comas y puntos separan palabras, así "MIRAFLORES - LIMA - LIMA" se busca tal cual.
    /// </summary>
    public static string Normalizar(string texto)
    {
        var limpio = new StringBuilder(texto.Length);
        foreach (var c in texto.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            limpio.Append(char.IsLetterOrDigit(c) ? char.ToUpperInvariant(c) : ' ');
        }

        return string.Join(' ', limpio.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static int Prioridad(Entrada entrada, string consulta, string primeraPalabra) =>
        entrada.Distrito == consulta ? 0
        : entrada.Distrito.StartsWith(consulta, StringComparison.Ordinal) ? 1
        : entrada.Distrito.StartsWith(primeraPalabra, StringComparison.Ordinal) ? 2
        : ContienePalabra(entrada.Distrito, primeraPalabra) ? 3
        : 4;

    /// <summary>Alguna palabra del texto empieza con <paramref name="palabra"/> ("san juan" no coincide con "LURIGANCHO").</summary>
    private static bool ContienePalabra(string texto, string palabra) =>
        texto.StartsWith(palabra, StringComparison.Ordinal) || texto.Contains(' ' + palabra, StringComparison.Ordinal);

    private sealed record Entrada(Ubigeo Ubigeo, string Distrito, string Todo);
}
