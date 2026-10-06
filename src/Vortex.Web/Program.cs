using System.Globalization;
using MudBlazor.Services;
using Vortex.Domain.Comun;
using Vortex.Infrastructure;
using Vortex.Infrastructure.Configuracion;
using Vortex.Infrastructure.Datos;
using Vortex.Shared.Servicios;
using Vortex.Web.Components;
using Vortex.Web.Servicios;

// Fechas, calendario y números en español de Perú, sin importar la configuración del servidor
CultureInfo.DefaultThreadCurrentCulture = FormatoPeru.Cultura;
CultureInfo.DefaultThreadCurrentUICulture = FormatoPeru.Cultura;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
// Los datos de la web van en App_Data (o en la carpeta de "Datos:Carpeta" en appsettings)
var carpetaDatos = builder.Configuration["Datos:Carpeta"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data");
var configuracion = new AlmacenConfiguracionArchivo(Path.Combine(carpetaDatos, "instalacion.json"));
builder.Services.AddVortexInfraestructura(
    OpcionesDatos.SegunConfiguracion(configuracion, Path.Combine(carpetaDatos, "vortex.db")));
builder.Services.AddScoped<IServicioArchivos, ServicioArchivosWeb>();

var app = builder.Build();
app.Services.PrepararBaseDeDatos();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Vortex.Shared._Imports).Assembly);

app.Run();
