// Descarga en el navegador un archivo generado en .NET (por ejemplo, el PDF de una cotización)
window.vortexDescargarArchivo = async (nombre, tipo, flujo) => {
    const contenido = await flujo.arrayBuffer();
    const url = URL.createObjectURL(new Blob([contenido], { type: tipo }));
    const enlace = document.createElement('a');
    enlace.href = url;
    enlace.download = nombre;
    document.body.appendChild(enlace);
    enlace.click();
    enlace.remove();
    URL.revokeObjectURL(url);
};
