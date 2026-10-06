using Bunit;
using Vortex.Domain.Comun;
using Vortex.Domain.Contactos;
using Vortex.Shared.Pages.Contactos;
using Xunit;

namespace Vortex.Shared.Tests;

public class EmpresaEditarTests : PruebaUi
{
    private const string RucSunat = "20131312955";

    private IRepositorioContactos Repositorio => Servicio<IRepositorioContactos>();

    [Fact]
    public async Task IngresarUnRucValidoCompletaLosDatosDeSunat()
    {
        var esperado = await Servicio<IConsultaDocumentos>().ConsultarRucAsync(RucSunat, Ct);
        var pagina = Render<EmpresaEditar>();

        pagina.FindAll("input")[0].Input(RucSunat);

        pagina.WaitForAssertion(() =>
            Assert.Equal(esperado!.RazonSocial, pagina.FindAll("input")[1].GetAttribute("value")));
        Assert.Contains("modo de prueba", pagina.Markup);
    }

    [Fact]
    public async Task ElRucTambienCompletaElDistritoYSeGuardaSuUbigeo()
    {
        var esperado = await Servicio<IConsultaDocumentos>().ConsultarRucAsync(RucSunat, Ct);
        var distrito = Servicio<CatalogoUbigeos>().Obtener(esperado!.Ubigeo)!;
        var pagina = Render<EmpresaEditar>();

        pagina.FindAll("input")[0].Input(RucSunat);
        pagina.WaitForAssertion(() =>
            Assert.Equal(distrito.Descripcion, pagina.Find("[data-campo-ubigeo] input").GetAttribute("value")));

        pagina.FindAll("button").First(b => b.TextContent.Contains("Guardar")).Click();

        pagina.WaitForAssertion(() =>
            Assert.Equal(distrito.Codigo, Repositorio.ListarEmpresasAsync().GetAwaiter().GetResult().Single().Ubigeo));
    }

    [Fact]
    public void ElDistritoSeBuscaPorNombreYSeEligeDeLaLista()
    {
        var pagina = Render<EmpresaEditar>();

        pagina.Find("[data-campo-ubigeo] input").Input("miraflores arequipa");
        Popovers.WaitForAssertion(() => Assert.Contains("MIRAFLORES", Popovers.Markup));
        Popovers.FindAll(".vx-ubigeo-opcion").Single().Click();

        pagina.WaitForAssertion(() =>
            Assert.Equal("MIRAFLORES - AREQUIPA - AREQUIPA", pagina.Find("[data-campo-ubigeo] input").GetAttribute("value")));
    }

    [Fact]
    public void UnRucConDigitoVerificadorIncorrectoMuestraError()
    {
        var pagina = Render<EmpresaEditar>();

        pagina.FindAll("input")[0].Input("20131312954");

        pagina.WaitForAssertion(() => Assert.Contains("El RUC no es válido", pagina.Markup));
    }

    [Fact]
    public async Task GuardarRegistraLaEmpresaYNoPermiteDuplicarElRuc()
    {
        var pagina = Render<EmpresaEditar>();
        pagina.FindAll("input")[0].Input(RucSunat);
        pagina.WaitForAssertion(() => Assert.NotEmpty(pagina.FindAll("input")[1].GetAttribute("value") ?? ""));

        pagina.FindAll("button").First(b => b.TextContent.Contains("Guardar")).Click();

        pagina.WaitForAssertion(() =>
            Assert.Single(Repositorio.ListarEmpresasAsync().GetAwaiter().GetResult()));

        // Una segunda empresa con el mismo RUC se rechaza
        var otra = Render<EmpresaEditar>();
        otra.FindAll("input")[0].Input(RucSunat);
        otra.WaitForAssertion(() => Assert.NotEmpty(otra.FindAll("input")[1].GetAttribute("value") ?? ""));
        otra.FindAll("button").First(b => b.TextContent.Contains("Guardar")).Click();

        otra.WaitForAssertion(() => Assert.Contains("Ya tienes registrada una empresa con este RUC", otra.Markup));
        Assert.Single(await Repositorio.ListarEmpresasAsync(cancellationToken: Ct));
    }
}
