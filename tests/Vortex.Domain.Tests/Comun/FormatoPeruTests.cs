using Vortex.Domain.Comun;
using Xunit;

namespace Vortex.Domain.Tests.Comun;

public class FormatoPeruTests
{
    [Theory]
    [InlineData(1500, "S/ 1,500.00")]
    [InlineData(0.5, "S/ 0.50")]
    [InlineData(1234567.891, "S/ 1,234,567.89")]
    [InlineData(-20, "S/ -20.00")]
    public void SolesUsaElFormatoPeruano(decimal monto, string esperado) =>
        Assert.Equal(esperado, FormatoPeru.Soles(monto));

    [Theory]
    [InlineData(0.75, "75%")]
    [InlineData(1, "100%")]
    public void PorcentajeSinEspacio(decimal valor, string esperado) =>
        Assert.Equal(esperado, valor.ToString("P0", FormatoPeru.Cultura));

    [Fact]
    public void HoyUsaLaHoraDePeruYNoLaUtc()
    {
        // 1 de octubre a las 03:00 UTC todavía es 30 de setiembre en Lima (UTC-5)
        var reloj = new RelojFijo(new DateTimeOffset(2026, 10, 1, 3, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 9, 30), FormatoPeru.Hoy(reloj));
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
