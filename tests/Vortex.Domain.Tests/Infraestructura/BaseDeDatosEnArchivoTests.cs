using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Vortex.Domain.Configuracion;
using Vortex.Domain.Contactos;
using Vortex.Infrastructure;
using Vortex.Infrastructure.Configuracion;
using Vortex.Infrastructure.Datos;
using Xunit;

namespace Vortex.Domain.Tests.Infraestructura;

/// <summary>Lo que ve la app al cerrarse y volver a abrirse: un archivo en su carpeta de datos.</summary>
public sealed class BaseDeDatosEnArchivoTests : IDisposable
{
    private readonly string carpeta = Path.Combine(Path.GetTempPath(), "vortex-tests", Guid.NewGuid().ToString("N"));
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private string RutaBase => Path.Combine(carpeta, "datos", "vortex.db");

    private string RutaConfiguracion => Path.Combine(carpeta, "instalacion.json");

    [Fact]
    public async Task LosDatosSiguenAhiAlVolverAAbrirLaApp()
    {
        var empresa = new Empresa { Ruc = "20131312955", RazonSocial = "SUNAT", Ubigeo = "150131" };

        using (var primera = AbrirApp())
        {
            await primera.GetRequiredService<IRepositorioContactos>().GuardarEmpresaAsync(empresa, ct);
        }

        using var segunda = AbrirApp();
        var guardada = await segunda.GetRequiredService<IRepositorioContactos>().ObtenerEmpresaAsync(empresa.Id, ct);

        Assert.Equal("SUNAT", guardada!.RazonSocial);
        Assert.Equal("150131", guardada.Ubigeo);
        Assert.True(File.Exists(RutaBase));
    }

    [Fact]
    public void LaConfiguracionSeGuardaEnSuArchivoYSeRecuerdaElModoConQueArranco()
    {
        var almacen = new AlmacenConfiguracionArchivo(RutaConfiguracion);
        Assert.Equal(ModoDatos.Local, almacen.ModoEnUso);

        var configuracion = almacen.Cargar();
        configuracion.Modo = ModoDatos.Servidor;
        configuracion.UrlServidor = "https://vortex.tunegocio.pe";
        configuracion.EstablecerClave("rosita2026");
        almacen.Guardar(configuracion);

        // El cambio de modo se aplica al volver a abrir la app
        Assert.Equal(ModoDatos.Local, almacen.ModoEnUso);

        var otraVez = new AlmacenConfiguracionArchivo(RutaConfiguracion);
        Assert.Equal(ModoDatos.Servidor, otraVez.ModoEnUso);
        Assert.Equal("https://vortex.tunegocio.pe", otraVez.Cargar().UrlServidor);
        Assert.True(otraVez.Cargar().VerificarClave("rosita2026"));
        Assert.DoesNotContain("rosita2026", File.ReadAllText(RutaConfiguracion));
    }

    [Fact]
    public void UnArchivoDeConfiguracionDanadoNoImpideAbrirLaApp()
    {
        Directory.CreateDirectory(carpeta);
        File.WriteAllText(RutaConfiguracion, "{ esto no es json");

        var configuracion = new AlmacenConfiguracionArchivo(RutaConfiguracion).Cargar();

        Assert.Equal(ModoDatos.Local, configuracion.Modo);
    }

    private ServiceProvider AbrirApp()
    {
        var servicios = new ServiceCollection()
            .AddVortexInfraestructura(OpcionesDatos.SegunConfiguracion(new AlmacenConfiguracionArchivo(RutaConfiguracion), RutaBase))
            .BuildServiceProvider();
        servicios.PrepararBaseDeDatos();
        return servicios;
    }

    public void Dispose()
    {
        // SQLite deja las conexiones en un pool: hay que soltarlas para poder borrar el archivo
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(carpeta))
        {
            Directory.Delete(carpeta, recursive: true);
        }
    }
}
