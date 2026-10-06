using Vortex.Domain.Comun;
using Vortex.Infrastructure.Comun;
using Vortex.Infrastructure.Contactos;
using Xunit;

namespace Vortex.Domain.Tests.Infraestructura;

public class UbigeosIneiTests
{
    private static readonly CatalogoUbigeos Catalogo = UbigeosInei.Cargar();

    [Fact]
    public void TraeTodosLosDistritosDelPeruConCodigosValidosYSinRepetir()
    {
        Assert.Equal(1892, Catalogo.Todos.Count);
        Assert.All(Catalogo.Todos, u => Assert.True(Ubigeo.EsCodigoValido(u.Codigo), u.Codigo));
        Assert.Equal(Catalogo.Todos.Count, Catalogo.Todos.Select(u => u.Codigo).Distinct().Count());
    }

    [Fact]
    public void TieneLos25DepartamentosYLas196Provincias()
    {
        Assert.Equal(25, Catalogo.Todos.Select(u => u.Codigo[..2]).Distinct().Count());
        Assert.Equal(196, Catalogo.Todos.Select(u => u.Codigo[..4]).Distinct().Count());
    }

    [Theory]
    [InlineData("150122", "LIMA", "LIMA", "MIRAFLORES")]
    [InlineData("070101", "CALLAO", "CALLAO", "CALLAO")]
    [InlineData("080108", "CUSCO", "CUSCO", "WANCHAQ")]
    [InlineData("150105", "LIMA", "LIMA", "BREÑA")]
    public void LosCodigosSonLosDelInei(string codigo, string departamento, string provincia, string distrito) =>
        Assert.Equal(new Ubigeo(codigo, departamento, provincia, distrito), Catalogo.Obtener(codigo));

    [Theory]
    [InlineData("20131312955")]
    [InlineData("20100047218")]
    [InlineData("10460278975")]
    public async Task LaConsultaDeRucDePruebaDevuelveUbigeosQueExisten(string ruc)
    {
        var datos = await new ConsultaDocumentosFalsa(TimeSpan.Zero).ConsultarRucAsync(ruc, TestContext.Current.CancellationToken);

        Assert.NotNull(Catalogo.Obtener(datos!.Ubigeo));
    }
}
