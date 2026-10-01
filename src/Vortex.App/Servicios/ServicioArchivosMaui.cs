using MudBlazor;
using Vortex.Shared.Servicios;

namespace Vortex.Servicios;

/// <summary>
/// En la app el archivo se guarda en la caché y se abre el menú de compartir del sistema,
/// para mandarlo directo por WhatsApp, correo o Drive.
/// </summary>
public sealed class ServicioArchivosMaui : IServicioArchivos
{
    public string EtiquetaPdf => "Compartir PDF";

    public string IconoPdf => Icons.Material.Filled.Share;

    public async Task EntregarAsync(string nombreArchivo, string tipoContenido, byte[] contenido)
    {
        var ruta = Path.Combine(FileSystem.CacheDirectory, nombreArchivo);
        await File.WriteAllBytesAsync(ruta, contenido);

        await MainThread.InvokeOnMainThreadAsync(() => Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = nombreArchivo,
            File = new ShareFile(ruta, tipoContenido),
        }));
    }
}
