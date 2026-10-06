using Vortex.Domain.Comun;
using Xunit;

namespace Vortex.Domain.Tests.Comun;

public class UbigeoTests
{
    private static readonly CatalogoUbigeos Catalogo = new(
    [
        new("150122", "LIMA", "LIMA", "MIRAFLORES"),
        new("040110", "AREQUIPA", "AREQUIPA", "MIRAFLORES"),
        new("150133", "LIMA", "LIMA", "SAN JUAN DE MIRAFLORES"),
        new("150105", "LIMA", "LIMA", "BREÑA"),
        new("150102", "LIMA", "LIMA", "ANCON"),
        new("150132", "LIMA", "LIMA", "SAN JUAN DE LURIGANCHO"),
        new("080918", "CUSCO", "LA CONVENCION", "UNION ASHÁNINKA"),
    ]);

    [Theory]
    [InlineData("150122", true)]
    [InlineData("15012", false)]
    [InlineData("15012A", false)]
    [InlineData(null, false)]
    public void ElCodigoTieneSeisDigitos(string? codigo, bool valido) => Assert.Equal(valido, Ubigeo.EsCodigoValido(codigo));

    [Fact]
    public void LaDescripcionVaDelDistritoAlDepartamento() =>
        Assert.Equal("MIRAFLORES - LIMA - LIMA", Catalogo.Obtener("150122")!.Descripcion);

    [Theory]
    [InlineData("brena", "150105")]
    [InlineData("Ancón", "150102")]
    [InlineData("  san   juan de lurig ", "150132")]
    [InlineData("ashaninka", "080918")]
    public void BuscaSinDistinguirMayusculasTildesNiEspacios(string texto, string codigo) =>
        Assert.Equal(codigo, Catalogo.Buscar(texto)[0].Codigo);

    [Fact]
    public void PrimeroVanLosQueSeLlamanAsiYLuegoLosQueLoContienen()
    {
        var codigos = Catalogo.Buscar("miraflores").Select(u => u.Codigo);

        Assert.Equal(["040110", "150122", "150133"], codigos);
    }

    [Fact]
    public void LaProvinciaOElDepartamentoSeparanLosDistritosDelMismoNombre()
    {
        Assert.Equal("040110", Catalogo.Buscar("miraflores arequipa").Single().Codigo);
        Assert.Equal("150122", Catalogo.Buscar("miraflores lima")[0].Codigo);
    }

    [Fact]
    public void LaDescripcionDeUnDistritoLoEncuentraDeNuevo() =>
        Assert.Equal("150122", Catalogo.Buscar("MIRAFLORES - LIMA - LIMA")[0].Codigo);

    [Fact]
    public void LasPalabrasDebenEmpezarIgualNoBastaQueAparezcanEnMedio() =>
        Assert.Empty(Catalogo.Buscar("flores"));

    [Fact]
    public void ConNumerosBuscaPorCodigo() =>
        Assert.Equal(["150102", "150105"], Catalogo.Buscar("15010").Select(u => u.Codigo));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SinTextoNoDevuelveNada(string? texto) => Assert.Empty(Catalogo.Buscar(texto));

    [Fact]
    public void RespetaElMaximoDeResultados() => Assert.Equal(2, Catalogo.Buscar("lima", maximo: 2).Count);

    [Theory]
    [InlineData("Av. Larco 123", "150122", "Av. Larco 123, MIRAFLORES - LIMA - LIMA")]
    [InlineData("Av. Larco 123", null, "Av. Larco 123")]
    [InlineData("Av. Larco 123", "999999", "Av. Larco 123")]
    [InlineData(" ", "150122", "MIRAFLORES - LIMA - LIMA")]
    [InlineData(null, null, null)]
    public void LaDireccionCompletaAgregaElDistritoSiLoHay(string? direccion, string? ubigeo, string? esperada) =>
        Assert.Equal(esperada, Catalogo.DireccionCompleta(direccion, ubigeo));
}
