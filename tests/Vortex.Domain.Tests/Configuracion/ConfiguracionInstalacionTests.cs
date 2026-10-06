using Vortex.Domain.Configuracion;
using Xunit;

namespace Vortex.Domain.Tests.Configuracion;

public class ConfiguracionInstalacionTests
{
    [Fact]
    public void PorDefectoEsLocalYSinClave()
    {
        var configuracion = new ConfiguracionInstalacion();

        Assert.Equal(ModoDatos.Local, configuracion.Modo);
        Assert.False(configuracion.TieneClave);
        Assert.False(configuracion.VerificarClave("cualquiera"));
    }

    [Fact]
    public void LaClaveSeVerificaSinGuardarlaEnTextoPlano()
    {
        var configuracion = new ConfiguracionInstalacion();

        configuracion.EstablecerClave("rosita2026");

        Assert.True(configuracion.TieneClave);
        Assert.DoesNotContain("rosita2026", configuracion.ClaveHash);
        Assert.True(configuracion.VerificarClave("rosita2026"));
        Assert.False(configuracion.VerificarClave("Rosita2026"));
        Assert.False(configuracion.VerificarClave(""));
    }

    [Fact]
    public void CadaClaveLlevaSuPropiaSal()
    {
        var una = new ConfiguracionInstalacion();
        var otra = new ConfiguracionInstalacion();

        una.EstablecerClave("rosita2026");
        otra.EstablecerClave("rosita2026");

        Assert.NotEqual(una.ClaveHash, otra.ClaveHash);
    }

    [Theory]
    [InlineData("12345", "12345", "La clave debe tener al menos 6 caracteres.")]
    [InlineData("123456", "123457", "Las claves no coinciden.")]
    [InlineData(null, null, "La clave debe tener al menos 6 caracteres.")]
    public void LaClaveNuevaDebeSerLargaYConfirmarse(string? clave, string? confirmacion, string error) =>
        Assert.Equal([error], ConfiguracionInstalacion.ValidarClaveNueva(clave, confirmacion));

    [Theory]
    [InlineData("https://vortex.tunegocio.pe", true)]
    [InlineData("http://192.168.1.10:5000", true)]
    [InlineData("vortex.tunegocio.pe", false)]
    [InlineData("ftp://vortex.tunegocio.pe", false)]
    [InlineData(null, false)]
    public void EnModoServidorLaDireccionDebeSerHttp(string? url, bool valida)
    {
        var configuracion = new ConfiguracionInstalacion { Modo = ModoDatos.Servidor, UrlServidor = url };

        Assert.Equal(valida, configuracion.Validar().Count == 0);
    }
}

public class SucursalTests
{
    [Fact]
    public void LaSucursalNuevaEsElDomicilioFiscal()
    {
        var sucursal = new Sucursal { Nombre = "Principal" };

        Assert.True(sucursal.EsDomicilioFiscal);
        Assert.Empty(sucursal.Validar());
    }

    [Theory]
    [InlineData("1")]
    [InlineData("00A1")]
    [InlineData("00001")]
    public void ElCodigoDeEstablecimientoTieneCuatroDigitos(string codigo) =>
        Assert.Equal(["Gamarra: el código de establecimiento debe tener 4 dígitos."],
            new Sucursal { Nombre = "Gamarra", CodigoEstablecimiento = codigo }.Validar());

    [Fact]
    public void ElNegocioNoAceptaDosSucursalesConElMismoCodigo()
    {
        var negocio = new Negocio { RazonSocial = "Bordados Rosita" };
        negocio.Sucursales.Add(new Sucursal { Nombre = "Principal" });
        negocio.Sucursales.Add(new Sucursal { Nombre = "Gamarra" });

        Assert.Equal(["Hay más de una sucursal con el código de establecimiento 0000."], negocio.Validar());
    }

    [Fact]
    public void ClonarElNegocioCopiaTambienSusSucursales()
    {
        var negocio = new Negocio { RazonSocial = "Bordados Rosita" };
        negocio.Sucursales.Add(new Sucursal { Nombre = "Principal" });

        var copia = negocio.Clonar();
        copia.Sucursales[0].Nombre = "Cambiada";
        copia.Sucursales.Add(new Sucursal { Nombre = "Otra", CodigoEstablecimiento = "0001" });

        Assert.Equal("Principal", Assert.Single(negocio.Sucursales).Nombre);
    }
}
