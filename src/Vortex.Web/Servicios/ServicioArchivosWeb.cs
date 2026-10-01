using Microsoft.JSInterop;
using MudBlazor;
using Vortex.Shared.Servicios;

namespace Vortex.Web.Servicios;

/// <summary>En el navegador el archivo se descarga (ver <c>vortex.js</c> en Vortex.Shared).</summary>
public sealed class ServicioArchivosWeb(IJSRuntime js) : IServicioArchivos
{
    public string EtiquetaPdf => "Descargar PDF";

    public string IconoPdf => Icons.Material.Filled.Download;

    public async Task EntregarAsync(string nombreArchivo, string tipoContenido, byte[] contenido)
    {
        using var flujo = new DotNetStreamReference(new MemoryStream(contenido));
        await js.InvokeVoidAsync("vortexDescargarArchivo", nombreArchivo, tipoContenido, flujo);
    }
}
