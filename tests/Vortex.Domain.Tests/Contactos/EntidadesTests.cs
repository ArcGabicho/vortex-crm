using Vortex.Domain.Contactos;
using Xunit;

namespace Vortex.Domain.Tests.Contactos;

public class EntidadesTests
{
    [Fact]
    public void EmpresaValidaNoTieneErrores()
    {
        var empresa = new Empresa { Ruc = "20131312955", RazonSocial = "SUPERINTENDENCIA NACIONAL DE ADUANAS" };

        Assert.Empty(empresa.Validar());
    }

    [Fact]
    public void EmpresaSinRucValidoNiRazonSocialTieneDosErrores()
    {
        var empresa = new Empresa { Ruc = "123" };

        Assert.Equal(2, empresa.Validar().Count);
    }

    [Fact]
    public void NombreVisiblePrefiereElNombreComercial()
    {
        var empresa = new Empresa { RazonSocial = "INVERSIONES ANDINA S.A.C.", NombreComercial = "Andina" };

        Assert.Equal("Andina", empresa.NombreVisible);

        empresa.NombreComercial = " ";
        Assert.Equal("INVERSIONES ANDINA S.A.C.", empresa.NombreVisible);
    }

    [Fact]
    public void ContactoSinDniEsValido()
    {
        var contacto = new Contacto { Nombres = "Rosa", Apellidos = "Quispe" };

        Assert.Empty(contacto.Validar());
        Assert.Equal("Rosa Quispe", contacto.NombreCompleto);
    }

    [Fact]
    public void ContactoConDniMalFormadoONombreVacioTieneErrores()
    {
        var contacto = new Contacto { Dni = "123", Nombres = "" };

        Assert.Equal(2, contacto.Validar().Count);
    }

    [Fact]
    public void ClonarDevuelveUnaCopiaIndependiente()
    {
        var original = new Contacto { Nombres = "Rosa" };
        var copia = original.Clonar();

        copia.Nombres = "Lucía";

        Assert.Equal("Rosa", original.Nombres);
        Assert.Equal(original.Id, copia.Id);
    }
}
