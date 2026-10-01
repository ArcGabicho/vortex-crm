namespace Vortex.Shared.Servicios;

/// <summary>
/// Entrega un archivo generado al usuario: en la web se descarga; en la app se abre el menú
/// de compartir del sistema (WhatsApp, correo, Drive...).
/// </summary>
public interface IServicioArchivos
{
    /// <summary>Texto del botón: "Descargar PDF" o "Compartir PDF".</summary>
    string EtiquetaPdf { get; }

    string IconoPdf { get; }

    Task EntregarAsync(string nombreArchivo, string tipoContenido, byte[] contenido);
}
