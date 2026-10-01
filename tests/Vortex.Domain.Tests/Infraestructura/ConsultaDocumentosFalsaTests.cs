using Vortex.Infrastructure.Contactos;
using Xunit;

namespace Vortex.Domain.Tests.Infraestructura;

public class ConsultaDocumentosFalsaTests
{
    private readonly ConsultaDocumentosFalsa consulta = new(TimeSpan.Zero);

    [Fact]
    public async Task RucDeEmpresaDevuelveRazonSocialSacYEsSiempreIgual()
    {
        var primera = await consulta.ConsultarRucAsync("20131312955", TestContext.Current.CancellationToken);
        var segunda = await consulta.ConsultarRucAsync("20131312955", TestContext.Current.CancellationToken);

        Assert.NotNull(primera);
        Assert.EndsWith("S.A.C.", primera.RazonSocial);
        Assert.Equal("ACTIVO", primera.Estado);
        Assert.Equal(primera, segunda);
    }

    [Fact]
    public async Task RucDePersonaNaturalUsaElNombreDelDniQueContiene()
    {
        var ruc = await consulta.ConsultarRucAsync("10460278975", TestContext.Current.CancellationToken);
        var dni = await consulta.ConsultarDniAsync("46027897", TestContext.Current.CancellationToken);

        Assert.NotNull(ruc);
        Assert.NotNull(dni);
        Assert.Equal($"{dni.Apellidos} {dni.Nombres}", ruc.RazonSocial);
    }

    [Theory]
    [InlineData("20600000005")] // dígitos 7 a 10 en 0000: "no encontrado"
    [InlineData("20131312954")] // RUC inválido
    public async Task RucNoEncontradoOInvalidoDevuelveNull(string ruc) =>
        Assert.Null(await consulta.ConsultarRucAsync(ruc, TestContext.Current.CancellationToken));

    [Theory]
    [InlineData("46020000")] // termina en 0000: "no encontrado"
    [InlineData("123")]
    public async Task DniNoEncontradoOInvalidoDevuelveNull(string dni) =>
        Assert.Null(await consulta.ConsultarDniAsync(dni, TestContext.Current.CancellationToken));

    [Fact]
    public void SeIdentificaComoProveedorDePrueba() => Assert.True(consulta.EsDePrueba);
}
