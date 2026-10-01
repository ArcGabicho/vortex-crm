# Vortex CRM

El CRM para emprendedores peruanos: clientes, ventas, cotizaciones y seguimiento en un solo lugar, en el celular, en la PC y en el navegador.

---

**Vortex CRM** es una plataforma de gestión de relaciones con clientes pensada para el emprendedor peruano: búsqueda por RUC/DNI, cotizaciones en soles con IGV, facturación electrónica SUNAT, WhatsApp como canal principal de venta y funcionamiento sin conexión.

## Stack tecnológico

- **App nativa:** .NET MAUI Blazor Hybrid (.NET 10): Android, iOS, Windows y macOS
- **Web:** Blazor Web App, que reutiliza los mismos componentes que la app
- **UI:** [MudBlazor](https://mudblazor.com/)
- **Backend:** ASP.NET Core Minimal API *(Fase 2)*
- **Base de datos:** SQL Server con EF Core *(Fase 2)*; SQLite local para el modo offline
- **Tests:** xUnit + bUnit

## Estructura

```
src/
├── Vortex.App/      App MAUI Blazor Hybrid (host nativo, plataformas)
├── Vortex.Shared/   Toda la UI: páginas, layout y componentes Razor
└── Vortex.Web/      Host web de la misma UI
tests/
└── Vortex.Shared.Tests/   Tests de componentes con bUnit
```

La UI se escribe **una sola vez** en `Vortex.Shared` y la usan tanto la app como la web.

## Requisitos

- .NET SDK 10
- Workloads de MAUI: `dotnet workload install maui` (o Visual Studio con la carga de trabajo de .NET MAUI)

## Cómo ejecutar

```bash
# Web
dotnet run --project src/Vortex.Web

# App de Windows
dotnet build src/Vortex.App -f net10.0-windows10.0.19041.0 -t:Run

# App de Android (con un emulador o equipo conectado)
dotnet build src/Vortex.App -f net10.0-android -t:Run

# Tests
dotnet test tests/Vortex.Shared.Tests
```

## Hoja de ruta

| Fase | Contenido | Estado |
|---|---|---|
| 0 | Fundaciones: Blazor Hybrid + Web, MudBlazor, CI, licencia | ✅ |
| 1 | MVP: contactos (RUC/DNI), pipeline, cotizaciones en PEN con IGV, tareas | ⏳ |
| 2 | API, SQL Server, multiempresa, autenticación, sincronización offline | ⏳ |
| 3 | Facturación electrónica SUNAT (vía PSE/OSE), WhatsApp Business, cobros | ⏳ |
| 4 | IA: resúmenes de conversaciones, lead scoring, redacción de mensajes | ⏳ |
| 5 | Lanzamiento: Play Store, web, Windows | ⏳ |

## Licencia

Distribuido bajo la **GNU Affero General Public License v3.0 o posterior**. Consulta [LICENSE](LICENSE).
