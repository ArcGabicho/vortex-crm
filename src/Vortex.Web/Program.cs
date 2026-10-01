using System.Globalization;
using MudBlazor.Services;
using Vortex.Domain.Comun;
using Vortex.Infrastructure;
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
builder.Services.AddVortexInfraestructura();
builder.Services.AddScoped<IServicioArchivos, ServicioArchivosWeb>();

var app = builder.Build();

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
