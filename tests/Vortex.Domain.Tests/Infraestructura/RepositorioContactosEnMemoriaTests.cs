using Vortex.Domain.Contactos;
using Vortex.Infrastructure.Contactos;
using Xunit;

namespace Vortex.Domain.Tests.Infraestructura;

public class RepositorioContactosEnMemoriaTests
{
    private readonly RepositorioContactosEnMemoria repositorio = new();
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task EditarUnObjetoObtenidoNoCambiaLosDatosHastaGuardar()
    {
        var empresa = new Empresa { Ruc = "20131312955", RazonSocial = "Original" };
        await repositorio.GuardarEmpresaAsync(empresa, ct);

        var editada = (await repositorio.ObtenerEmpresaAsync(empresa.Id, ct))!;
        editada.RazonSocial = "Cambiada";

        Assert.Equal("Original", (await repositorio.ObtenerEmpresaAsync(empresa.Id, ct))!.RazonSocial);

        await repositorio.GuardarEmpresaAsync(editada, ct);
        Assert.Equal("Cambiada", (await repositorio.ObtenerEmpresaAsync(empresa.Id, ct))!.RazonSocial);
    }

    [Fact]
    public async Task ExisteRucIgnoraLaPropiaEmpresa()
    {
        var empresa = new Empresa { Ruc = "20131312955", RazonSocial = "SUNAT" };
        await repositorio.GuardarEmpresaAsync(empresa, ct);

        Assert.False(await repositorio.ExisteRucAsync("20131312955", empresa.Id, ct));
        Assert.True(await repositorio.ExisteRucAsync("20131312955", Guid.NewGuid(), ct));
    }

    [Fact]
    public async Task EliminarEmpresaDejaASusContactosSinEmpresa()
    {
        var empresa = new Empresa { Ruc = "20131312955", RazonSocial = "SUNAT" };
        var contacto = new Contacto { Nombres = "Rosa", EmpresaId = empresa.Id };
        await repositorio.GuardarEmpresaAsync(empresa, ct);
        await repositorio.GuardarContactoAsync(contacto, ct);

        await repositorio.EliminarEmpresaAsync(empresa.Id, ct);

        Assert.Null(await repositorio.ObtenerEmpresaAsync(empresa.Id, ct));
        Assert.Null((await repositorio.ObtenerContactoAsync(contacto.Id, ct))!.EmpresaId);
    }

    [Fact]
    public async Task FiltroBuscaSinDistinguirMayusculasYOrdenaPorNombre()
    {
        await repositorio.GuardarContactoAsync(new Contacto { Nombres = "Rosa", Apellidos = "Quispe", Telefono = "987654321" }, ct);
        await repositorio.GuardarContactoAsync(new Contacto { Nombres = "Ana", Apellidos = "Quispe" }, ct);
        await repositorio.GuardarContactoAsync(new Contacto { Nombres = "Pedro", Apellidos = "Flores" }, ct);

        var quispe = await repositorio.ListarContactosAsync("quispe", ct);
        Assert.Equal(["Ana Quispe", "Rosa Quispe"], quispe.Select(c => c.NombreCompleto));

        var porTelefono = await repositorio.ListarContactosAsync("987", ct);
        Assert.Single(porTelefono);
    }
}
