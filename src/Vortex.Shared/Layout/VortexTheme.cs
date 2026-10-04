using MudBlazor;

namespace Vortex.Shared.Layout;

/// <summary>Tema visual de Vortex CRM, compartido por la app nativa y la web: oscuro, con acento naranja.</summary>
public static class VortexTheme
{
    public static readonly MudTheme Default = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#E8631F",
            Secondary = "#6B7280",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#1F1F1F",
            DrawerBackground = "#FFFFFF",
            Background = "#F4F4F5",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#F2733A",
            Secondary = "#9A9A9A",
            Background = "#101010",
            BackgroundGray = "#232323",
            Surface = "#1B1B1B",
            AppbarBackground = "#101010",
            AppbarText = "#EDEDED",
            DrawerBackground = "#151515",
            DrawerText = "#B5B5B5",
            DrawerIcon = "#B5B5B5",
            TextPrimary = "#F2F2F2",
            TextSecondary = "#9A9A9A",
            ActionDefault = "#B5B5B5",
            Divider = "#2A2A2A",
            DividerLight = "#222222",
            LinesDefault = "#2A2A2A",
            TableLines = "#2A2A2A",
            HoverOpacity = 0.08,
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px",
        },
    };
}
