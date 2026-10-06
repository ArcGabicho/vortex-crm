using Bunit;
using Vortex.Domain.Configuracion;
using Vortex.Shared.Pages.Configuracion;
using Xunit;

namespace Vortex.Shared.Tests;

public class DatosYSucursalesTests : PruebaUi
{
    private const string Clave = "rosita2026";

    private IAlmacenConfiguracion Almacen => Servicio<IAlmacenConfiguracion>();

    private IRepositorioNegocio Negocios => Servicio<IRepositorioNegocio>();

    [Fact]
    public void LaPrimeraVezPideCrearLaClaveYLuegoMuestraLaConfiguracion()
    {
        var pagina = Render<DatosYSucursales>();
        Assert.NotNull(pagina.Find("[data-crear-clave]"));

        var campos = pagina.FindAll("input[type=password]");
        campos[0].Change(Clave);
        campos[1].Change(Clave);
        Boton(pagina, "Crear clave").Click();

        pagina.WaitForAssertion(() => Assert.NotNull(pagina.Find("[data-modo]")));
        Assert.True(Almacen.Cargar().VerificarClave(Clave));
    }

    [Fact]
    public void UnaClaveCortaOQueNoCoincideNoSeAcepta()
    {
        var pagina = Render<DatosYSucursales>();

        var campos = pagina.FindAll("input[type=password]");
        campos[0].Change("123");
        campos[1].Change("123");
        Boton(pagina, "Crear clave").Click();

        Assert.Contains("al menos 6 caracteres", pagina.Find("[data-errores]").TextContent);
        Assert.False(Almacen.Cargar().TieneClave);
    }

    [Fact]
    public void ConClaveSoloEntraQuienLaSabe()
    {
        ConClave();
        var pagina = Render<DatosYSucursales>();

        pagina.Find("[data-desbloquear] input").Input("equivocada");
        Boton(pagina, "Entrar").Click();
        Assert.Contains("La clave no es correcta", pagina.Markup);
        Assert.Empty(pagina.FindAll("[data-modo]"));

        pagina.Find("[data-desbloquear] input").Input(Clave);
        Boton(pagina, "Entrar").Click();
        pagina.WaitForAssertion(() => Assert.NotNull(pagina.Find("[data-modo]")));
    }

    [Fact]
    public async Task AgregarSucursalesYAsignarElEquipoAUna()
    {
        await Negocios.GuardarAsync(new Negocio { RazonSocial = "Bordados Rosita", Direccion = "Av. Larco 345", Ubigeo = "150122" }, Ct);
        ConClave();
        var pagina = Entrar();

        Boton(pagina, "Agregar sucursal").Click();
        Boton(pagina, "Agregar sucursal").Click();
        var segunda = pagina.FindAll("[data-sucursal]")[1];
        segunda.QuerySelector("input")!.Change("Gamarra");
        Boton(pagina, "Guardar").Click();

        var negocio = await Negocios.ObtenerAsync(Ct);
        Assert.Equal(["Principal", "Gamarra"], negocio.Sucursales.Select(s => s.Nombre));
        Assert.Equal(["0000", "0001"], negocio.Sucursales.Select(s => s.CodigoEstablecimiento));
        Assert.Equal("150122", negocio.Sucursales[0].Ubigeo); // la principal toma la dirección de Mi negocio

        // Este equipo pasa a ser de Gamarra
        pagina.Find("[data-sucursal-del-equipo] .mud-input-control.mud-select").MouseDown();
        Popovers.WaitForAssertion(() => Assert.Contains("Gamarra (0001)", Popovers.Markup));
        Popovers.FindAll(".mud-list-item").First(i => i.TextContent.Contains("Gamarra")).Click();
        Boton(pagina, "Guardar").Click();

        pagina.WaitForAssertion(() => Assert.Equal(negocio.Sucursales[1].Id, Almacen.Cargar().SucursalId));
    }

    [Fact]
    public async Task NoGuardaSucursalesConElMismoCodigo()
    {
        await Negocios.GuardarAsync(new Negocio { RazonSocial = "Bordados Rosita" }, Ct);
        ConClave();
        var pagina = Entrar();

        Boton(pagina, "Agregar sucursal").Click();
        Boton(pagina, "Agregar sucursal").Click();
        var segunda = pagina.FindAll("[data-sucursal]")[1];
        segunda.QuerySelector("input")!.Change("Gamarra");
        segunda.QuerySelectorAll("input")[1].Change("0000");
        Boton(pagina, "Guardar").Click();

        Assert.Contains("más de una sucursal con el código de establecimiento 0000", pagina.Find("[data-errores]").TextContent);
        Assert.Empty((await Negocios.ObtenerAsync(Ct)).Sucursales);
    }

    [Fact]
    public void ElModoServidorTodaviaNoSePuedeElegir()
    {
        ConClave();
        var pagina = Entrar();

        var radios = pagina.FindAll("[data-modo] input[type=radio]");
        Assert.Equal(2, radios.Count);
        Assert.True(radios[1].HasAttribute("disabled"));
        Assert.Equal(ModoDatos.Local, Almacen.Cargar().Modo);
    }

    private void ConClave()
    {
        var configuracion = Almacen.Cargar();
        configuracion.EstablecerClave(Clave);
        Almacen.Guardar(configuracion);
    }

    private IRenderedComponent<DatosYSucursales> Entrar()
    {
        var pagina = Render<DatosYSucursales>();
        pagina.Find("[data-desbloquear] input").Input(Clave);
        Boton(pagina, "Entrar").Click();
        pagina.WaitForAssertion(() => Assert.NotNull(pagina.Find("[data-modo]")));
        return pagina;
    }

    private static AngleSharp.Dom.IElement Boton(IRenderedComponent<DatosYSucursales> pagina, string texto) =>
        pagina.FindAll("button").First(b => b.TextContent.Contains(texto));
}
