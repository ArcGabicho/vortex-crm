using MudBlazor;

namespace Vortex.Shared.Layout;

/// <summary>Tema visual de Vortex CRM, compartido por la app nativa y la web.</summary>
public static class VortexTheme
{
    public static readonly MudTheme Default = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#4F46E5",
            Secondary = "#0EA5E9",
            AppbarBackground = "#4F46E5",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#818CF8",
            Secondary = "#38BDF8",
        },
    };
}
