using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Vortex.Domain.Contactos;
using Vortex.Domain.Ventas;
using Vortex.Infrastructure.Contactos;
using Vortex.Infrastructure.Ventas;
using Vortex.Shared.Pages.Contactos;
using Xunit;

namespace Vortex.Shared.Tests;

public class EmpresaEditarTests : BunitContext
{
    private const string RucSunat = "20131312955";

    private readonly RepositorioContactosEnMemoria repositorio = new();
    private readonly ConsultaDocumentosFalsa consulta = new(TimeSpan.Zero);

    public EmpresaEditarTests()
    {
        Services.AddMudServices();
        Services.AddSingleton<IRepositorioContactos>(repositorio);
        Services.AddSingleton<IConsultaDocumentos>(consulta);
        Services.AddSingleton<IRepositorioOportunidades>(new RepositorioOportunidadesEnMemoria());
        JSInterop.Mode = JSRuntimeMode.Loose;
        Render<MudPopoverProvider>();
    }

    [Fact]
    public async Task IngresarUnRucValidoCompletaLosDatosDeSunat()
    {
        var esperado = await consulta.ConsultarRucAsync(RucSunat, Xunit.TestContext.Current.CancellationToken);
        var pagina = Render<EmpresaEditar>();

        pagina.FindAll("input")[0].Input(RucSunat);

        pagina.WaitForAssertion(() =>
            Assert.Equal(esperado!.RazonSocial, pagina.FindAll("input")[1].GetAttribute("value")));
        Assert.Contains("modo de prueba", pagina.Markup);
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
            Assert.Single(repositorio.ListarEmpresasAsync().GetAwaiter().GetResult()));

        // Una segunda empresa con el mismo RUC se rechaza
        var otra = Render<EmpresaEditar>();
        otra.FindAll("input")[0].Input(RucSunat);
        otra.WaitForAssertion(() => Assert.NotEmpty(otra.FindAll("input")[1].GetAttribute("value") ?? ""));
        otra.FindAll("button").First(b => b.TextContent.Contains("Guardar")).Click();

        otra.WaitForAssertion(() => Assert.Contains("Ya tienes registrada una empresa con este RUC", otra.Markup));
        Assert.Single(await repositorio.ListarEmpresasAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken));
    }
}
