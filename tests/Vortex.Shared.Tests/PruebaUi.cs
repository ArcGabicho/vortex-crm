using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Vortex.Domain.Contactos;
using Vortex.Infrastructure;
using Vortex.Infrastructure.Contactos;
using Vortex.Shared.Servicios;

namespace Vortex.Shared.Tests;

/// <summary>
/// Base de los tests de pantallas: registra los mismos servicios que la app (repositorios en
/// memoria), el proveedor de RUC/DNI de prueba sin demora y un servicio de archivos que solo anota.
/// </summary>
public abstract class PruebaUi : BunitContext
{
    protected PruebaUi()
    {
        Services.AddMudServices();
        Services.AddVortexInfraestructura();
        Services.AddSingleton<IConsultaDocumentos>(new ConsultaDocumentosFalsa(TimeSpan.Zero));
        Services.AddSingleton<IServicioArchivos>(Archivos);
        JSInterop.Mode = JSRuntimeMode.Loose;
        Popovers = Render<MudPopoverProvider>();
        Dialogos = Render<MudDialogProvider>();
    }

    protected CancellationToken Ct { get; } = Xunit.TestContext.Current.CancellationToken;

    /// <summary>Donde se renderizan los menús y las listas desplegables de MudBlazor.</summary>
    protected IRenderedComponent<MudPopoverProvider> Popovers { get; }

    /// <summary>Donde se renderizan los diálogos.</summary>
    protected IRenderedComponent<MudDialogProvider> Dialogos { get; }

    protected ArchivosEntregados Archivos { get; } = new();

    protected T Servicio<T>() where T : notnull => Services.GetRequiredService<T>();
}

public sealed class ArchivosEntregados : IServicioArchivos
{
    public List<(string Nombre, string Tipo, byte[] Contenido)> Entregados { get; } = [];

    public string EtiquetaPdf => "Descargar PDF";

    public string IconoPdf => Icons.Material.Filled.Download;

    public Task EntregarAsync(string nombreArchivo, string tipoContenido, byte[] contenido)
    {
        Entregados.Add((nombreArchivo, tipoContenido, contenido));
        return Task.CompletedTask;
    }
}
