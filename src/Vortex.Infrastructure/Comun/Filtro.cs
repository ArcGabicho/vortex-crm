namespace Vortex.Infrastructure.Comun;

internal static class Filtro
{
    /// <summary>Sin filtro coincide todo; con filtro, basta que algún campo lo contenga, sin distinguir mayúsculas.</summary>
    public static bool Coincide(string? filtro, params string?[] campos) =>
        string.IsNullOrWhiteSpace(filtro)
        || campos.Any(c => c?.Contains(filtro.Trim(), StringComparison.CurrentCultureIgnoreCase) == true);
}
