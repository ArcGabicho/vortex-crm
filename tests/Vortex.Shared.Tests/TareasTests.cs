using Bunit;
using Microsoft.AspNetCore.Components;
using Vortex.Domain.Comun;
using Vortex.Domain.Contactos;
using Vortex.Domain.Tareas;
using Vortex.Domain.Ventas;
using Vortex.Shared.Pages.Contactos;
using Vortex.Shared.Pages.Tareas;
using Vortex.Shared.Pages.Ventas;
using Xunit;

namespace Vortex.Shared.Tests;

public class TareasTests : PruebaUi
{
    private IRepositorioTareas Repositorio => Servicio<IRepositorioTareas>();

    private NavigationManager Navegacion => Servicio<NavigationManager>();

    [Fact]
    public async Task GuardarUnaTareaNuevaLaAgregaParaHoy()
    {
        var pagina = Render<TareaEditar>();

        pagina.Find("input").Change("Llamar para confirmar el pedido");
        pagina.FindAll("button").First(b => b.TextContent.Trim() == "Guardar").Click();

        var tarea = Assert.Single(await Repositorio.ListarAsync(Ct));
        Assert.Equal("Llamar para confirmar el pedido", tarea.Titulo);
        Assert.Equal(FormatoPeru.Hoy(), tarea.Fecha);
        Assert.Null(tarea.AvisarMinutosAntes);
        Assert.EndsWith("/tareas", Navegacion.Uri);
    }

    [Fact]
    public async Task SinTituloNoSeGuarda()
    {
        var pagina = Render<TareaEditar>();

        pagina.FindAll("button").First(b => b.TextContent.Trim() == "Guardar").Click();

        Assert.Contains("Escribe qué hay que hacer.", pagina.Markup);
        Assert.Empty(await Repositorio.ListarAsync(Ct));
    }

    [Fact]
    public void EnLaWebNoSeOfreceElAviso()
    {
        // Las pruebas usan SinRecordatorios, igual que la web: no hay notificaciones del sistema
        var pagina = Render<TareaEditar>();

        Assert.DoesNotContain("Avisarme", pagina.Markup);
    }

    [Fact]
    public async Task DesdeUnContactoLaTareaLlegaVinculadaConSuEmpresaYVuelveAlContacto()
    {
        var empresa = new Empresa { Ruc = "20100070970", RazonSocial = "Textiles Andinos SAC" };
        var contacto = new Contacto { Nombres = "Rosa", Apellidos = "Quispe", EmpresaId = empresa.Id };
        await Servicio<IRepositorioContactos>().GuardarEmpresaAsync(empresa, Ct);
        await Servicio<IRepositorioContactos>().GuardarContactoAsync(contacto, Ct);
        Navegacion.NavigateTo($"tareas/nueva?contacto={contacto.Id}");

        var pagina = Render<TareaEditar>();
        pagina.Find("input").Change("Llamar a Rosa");
        pagina.FindAll("button").First(b => b.TextContent.Trim() == "Guardar").Click();

        var tarea = Assert.Single(await Repositorio.ListarAsync(Ct));
        Assert.Equal(contacto.Id, tarea.ContactoId);
        Assert.Equal(empresa.Id, tarea.EmpresaId);
        Assert.EndsWith($"/contactos/{contacto.Id}", Navegacion.Uri);
    }

    [Fact]
    public void UnVolverQueNoEsDeLaAppSeIgnora()
    {
        Navegacion.NavigateTo("tareas/nueva?volver=https://ejemplo.com");

        var pagina = Render<TareaEditar>();

        var cancelar = pagina.FindAll("a").First(a => a.TextContent.Trim() == "Cancelar");
        Assert.Equal("tareas", cancelar.GetAttribute("href"));
    }

    [Fact]
    public async Task LaListaSeparaVencidasHoyYProximas()
    {
        var hoy = FormatoPeru.Hoy();
        await Repositorio.GuardarAsync(new Tarea { Titulo = "Atrasada", Fecha = hoy.AddDays(-1) }, Ct);
        await Repositorio.GuardarAsync(new Tarea { Titulo = "De hoy", Fecha = hoy, Hora = new TimeOnly(10, 30) }, Ct);
        await Repositorio.GuardarAsync(new Tarea { Titulo = "Luego", Fecha = hoy.AddDays(5) }, Ct);

        var pagina = Render<Tareas>();

        Assert.NotNull(pagina.Find("[data-seccion=vencidas]").QuerySelector("[data-tarea='Atrasada']"));
        var deHoy = pagina.Find("[data-seccion=hoy]").QuerySelector("[data-tarea='De hoy']")!;
        Assert.Contains("Hoy, 10:30 a. m.", deHoy.QuerySelector("[data-cuando]")!.TextContent);
        Assert.NotNull(pagina.Find("[data-seccion=proximas]").QuerySelector("[data-tarea='Luego']"));
    }

    [Fact]
    public async Task MarcarUnaTareaLaPasaAHechas()
    {
        var tarea = new Tarea { Titulo = "Llamar a Rosa" };
        await Repositorio.GuardarAsync(tarea, Ct);
        var pagina = Render<Tareas>();

        pagina.Find("[data-tarea='Llamar a Rosa'] input[type=checkbox]").Change(true);

        Assert.True((await Repositorio.ObtenerAsync(tarea.Id, Ct))!.Completada);
        pagina.WaitForAssertion(() => Assert.NotNull(pagina.Find("[data-seccion=completadas]")));
        Assert.Contains("No tienes tareas para hoy.", pagina.Find("[data-seccion=hoy]").TextContent);
    }

    [Fact]
    public async Task LaOportunidadMuestraSusTareasPendientes()
    {
        var contacto = new Contacto { Nombres = "Rosa" };
        await Servicio<IRepositorioContactos>().GuardarContactoAsync(contacto, Ct);
        var oportunidad = new Oportunidad { Titulo = "50 polos", ContactoId = contacto.Id };
        await Servicio<IRepositorioOportunidades>().GuardarAsync(oportunidad, Ct);
        await Repositorio.GuardarAsync(new Tarea { Titulo = "Enviar muestras", OportunidadId = oportunidad.Id }, Ct);
        await Repositorio.GuardarAsync(new Tarea { Titulo = "Otra cosa" }, Ct);

        var pagina = Render<OportunidadEditar>(p => p.Add(x => x.Id, oportunidad.Id));

        var panel = pagina.Find("[data-tareas-de]");
        Assert.NotNull(panel.QuerySelector("[data-tarea='Enviar muestras']"));
        Assert.Null(panel.QuerySelector("[data-tarea='Otra cosa']"));
        Assert.Contains($"tareas/nueva?oportunidad={oportunidad.Id}", panel.InnerHtml);
    }

    [Fact]
    public async Task EliminarUnContactoConservaSusTareasSinElVinculo()
    {
        var contacto = new Contacto { Nombres = "Rosa" };
        await Servicio<IRepositorioContactos>().GuardarContactoAsync(contacto, Ct);
        var tarea = new Tarea { Titulo = "Llamar a Rosa", ContactoId = contacto.Id };
        await Repositorio.GuardarAsync(tarea, Ct);

        var pagina = Render<ContactoEditar>(p => p.Add(x => x.Id, contacto.Id));
        pagina.FindAll("button").First(b => b.TextContent.Trim() == "Eliminar").Click();
        Dialogos.WaitForAssertion(() => Assert.NotEmpty(Dialogos.FindAll("button")));
        Dialogos.FindAll("button").First(b => b.TextContent.Trim() == "Eliminar").Click();

        pagina.WaitForAssertion(() => Assert.Null((Repositorio.ObtenerAsync(tarea.Id, Ct).Result)!.ContactoId));
    }
}
