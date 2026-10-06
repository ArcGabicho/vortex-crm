using Vortex.Domain.Contactos;
using Vortex.Infrastructure.Contactos;
using Vortex.Infrastructure.Datos;
using Xunit;

namespace Vortex.Domain.Tests.Infraestructura;

public abstract class RepositorioContactosTests
{
    private IRepositorioContactos? creado;
    private readonly CancellationToken ct = TestContext.Current.CancellationToken;

    private IRepositorioContactos Repositorio => creado ??= Crear();

    protected abstract IRepositorioContactos Crear();

    [Fact]
    public async Task EditarUnObjetoObtenidoNoCambiaLosDatosHastaGuardar()
    {
        var empresa = new Empresa { Ruc = "20131312955", RazonSocial = "Original" };
        await Repositorio.GuardarEmpresaAsync(empresa, ct);

        var editada = (await Repositorio.ObtenerEmpresaAsync(empresa.Id, ct))!;
        editada.RazonSocial = "Cambiada";

        Assert.Equal("Original", (await Repositorio.ObtenerEmpresaAsync(empresa.Id, ct))!.RazonSocial);

        await Repositorio.GuardarEmpresaAsync(editada, ct);
        Assert.Equal("Cambiada", (await Repositorio.ObtenerEmpresaAsync(empresa.Id, ct))!.RazonSocial);
    }

    [Fact]
    public async Task ExisteRucIgnoraLaPropiaEmpresa()
    {
        var empresa = new Empresa { Ruc = "20131312955", RazonSocial = "SUNAT" };
        await Repositorio.GuardarEmpresaAsync(empresa, ct);

        Assert.False(await Repositorio.ExisteRucAsync("20131312955", empresa.Id, ct));
        Assert.True(await Repositorio.ExisteRucAsync("20131312955", Guid.NewGuid(), ct));
    }

    [Fact]
    public async Task EliminarEmpresaDejaASusContactosSinEmpresa()
    {
        var empresa = new Empresa { Ruc = "20131312955", RazonSocial = "SUNAT" };
        var contacto = new Contacto { Nombres = "Rosa", EmpresaId = empresa.Id };
        await Repositorio.GuardarEmpresaAsync(empresa, ct);
        await Repositorio.GuardarContactoAsync(contacto, ct);

        await Repositorio.EliminarEmpresaAsync(empresa.Id, ct);

        Assert.Null(await Repositorio.ObtenerEmpresaAsync(empresa.Id, ct));
        Assert.Null((await Repositorio.ObtenerContactoAsync(contacto.Id, ct))!.EmpresaId);
    }

    [Fact]
    public async Task FiltroBuscaSinDistinguirMayusculasYOrdenaPorNombre()
    {
        await Repositorio.GuardarContactoAsync(new Contacto { Nombres = "Rosa", Apellidos = "Quispe", Telefono = "987654321" }, ct);
        await Repositorio.GuardarContactoAsync(new Contacto { Nombres = "Ana", Apellidos = "Quispe" }, ct);
        await Repositorio.GuardarContactoAsync(new Contacto { Nombres = "Pedro", Apellidos = "Flores" }, ct);

        var quispe = await Repositorio.ListarContactosAsync("quispe", ct);
        Assert.Equal(["Ana Quispe", "Rosa Quispe"], quispe.Select(c => c.NombreCompleto));

        var porTelefono = await Repositorio.ListarContactosAsync("987", ct);
        Assert.Single(porTelefono);
    }
}

public sealed class RepositorioContactosEnMemoriaTests : RepositorioContactosTests
{
    protected override IRepositorioContactos Crear() => new RepositorioContactosEnMemoria();
}

public sealed class RepositorioContactosSqliteTests : RepositorioContactosTests, IDisposable
{
    private readonly BaseSqliteDePrueba bd = new();

    protected override IRepositorioContactos Crear() => new RepositorioContactosSql(bd.Fabrica);

    public void Dispose() => bd.Dispose();
}
