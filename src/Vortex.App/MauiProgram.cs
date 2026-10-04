using Microsoft.Extensions.Logging;
using System.Globalization;
using MudBlazor.Services;
using Vortex.Domain.Comun;
using Vortex.Domain.Tareas;
using Vortex.Infrastructure;
using Vortex.Servicios;
using Vortex.Shared.Servicios;

namespace Vortex;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Fechas, calendario y números en español de Perú en toda la app
        CultureInfo.DefaultThreadCurrentCulture = FormatoPeru.Cultura;
        CultureInfo.DefaultThreadCurrentUICulture = FormatoPeru.Cultura;

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddMudServices();
        builder.Services.AddVortexInfraestructura();
        builder.Services.AddSingleton<IServicioArchivos, ServicioArchivosMaui>();
#if ANDROID
        // Reemplaza a SinRecordatorios: en Android los avisos de tareas son notificaciones del sistema
        builder.Services.AddSingleton<IServicioRecordatorios, ServicioRecordatoriosAndroid>();
#endif

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
