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
├── Vortex.App/             App MAUI Blazor Hybrid (host nativo, plataformas)
├── Vortex.Shared/          Toda la UI: páginas, layout y componentes Razor
├── Vortex.Web/             Host web de la misma UI
├── Vortex.Domain/          Entidades y reglas de negocio (RUC, DNI, contactos, pipeline, cotizaciones, catálogo, tareas)
└── Vortex.Infrastructure/  Datos, integraciones externas y generación de PDF
tests/
├── Vortex.Domain.Tests/    Tests de dominio e infraestructura
└── Vortex.Shared.Tests/    Tests de componentes con bUnit
```

La UI se escribe **una sola vez** en `Vortex.Shared` y la usan tanto la app como la web.

> **Estado temporal:** los datos se guardan en memoria y se pierden al cerrar la app. Hasta la Fase 2 la versión web los comparte entre todos los visitantes. La consulta de RUC/DNI usa un proveedor de prueba que inventa los datos (`ConsultaDocumentosFalsa`). Para simular "no encontrado", usa un DNI que termine en `0000` o un RUC cuyos dígitos 7 a 10 sean `0000` (por ejemplo `20600000005`).

> **Recordatorios de tareas:** solo la app de Android los muestra como notificación. Pueden llegar con unos minutos de atraso (así no hace falta el permiso de alarmas exactas) y se pierden si se reinicia el celular. En la web y en Windows las tareas pendientes se ven en Inicio.

## Requisitos

- .NET SDK 10
- Workloads de MAUI: `dotnet workload install maui` (o Visual Studio con la carga de trabajo de .NET MAUI)

## Cómo ejecutar

```bash
# Web
dotnet run --project src/Vortex.Web

# App de Windows
dotnet run --project src/Vortex.App -f net10.0-windows10.0.19041.0

# App de Android (con un emulador o equipo conectado)
dotnet build src/Vortex.App -f net10.0-android -t:Run

# Tests
dotnet test tests/Vortex.Domain.Tests
dotnet test tests/Vortex.Shared.Tests
```

## Publicar en Microsoft Store

La app de escritorio se distribuye como **MSIX** a través de Microsoft Store. Los paquetes se suben **sin firmar**: la Store los firma con su propio certificado al publicarlos, así que no hace falta comprar un certificado de firma de código.

1. En [Partner Center](https://partner.microsoft.com/dashboard) reserva el nombre de la app y abre *Gestión de productos → Identidad del producto*.
2. En GitHub, en *Settings → Secrets and variables → Actions → Variables*, crea estas variables:
   - `STORE_IDENTITY_NAME`: el valor de `Package/Identity/Name`
   - `STORE_PUBLISHER`: el valor de `Package/Identity/Publisher` (`CN=...`)
   - `STORE_PUBLISHER_DISPLAY_NAME`: el valor de `Package/Properties/PublisherDisplayName`
3. Publica un tag de versión:
   ```bash
   git tag v0.1.0
   git push origin v0.1.0
   ```
   El workflow **Release Windows** genera los paquetes `x64` y `arm64`. También se puede lanzar a mano desde la pestaña *Actions*.
4. Descarga los `.msix` de los artefactos del workflow y súbelos en un envío nuevo en Partner Center.

Cada envío a la Store necesita una versión mayor que la anterior. La versión del MSIX es `major.minor.build.0`, porque la Store exige que el último número sea 0.

## Hoja de ruta

| Fase | Contenido | Estado |
|---|---|---|
| 0 | Fundaciones: Blazor Hybrid + Web, MudBlazor, CI, licencia, ícono | ✅ |
| 1 | MVP: contactos (RUC/DNI) ✅, pipeline ✅, cotizaciones con IGV, PDF y WhatsApp ✅, catálogo ✅, tareas y recordatorios ✅, ubigeo | 🚧 |
| 2 | API, SQL Server, multiempresa, autenticación, sincronización offline | ⏳ |
| 3 | Facturación electrónica SUNAT (vía PSE/OSE), WhatsApp Business, cobros | ⏳ |
| 4 | IA: resúmenes de conversaciones, lead scoring, redacción de mensajes | ⏳ |
| 5 | Lanzamiento: Play Store, web, Windows | ⏳ |

## Licencia

Distribuido bajo la **GNU Affero General Public License v3.0 o posterior**. Consulta [LICENSE](LICENSE).

## Componentes de terceros

- [MudBlazor](https://mudblazor.com/) (MIT): componentes de interfaz
- [PDFsharp / MigraDoc](https://www.pdfsharp.com/) (MIT): PDF de las cotizaciones, en código .NET sin dependencias nativas, así que funciona igual en Android, Windows y el servidor
- [Open Sans](https://github.com/googlefonts/opensans) (SIL Open Font License, ver `src/Vortex.Infrastructure/Recursos/Fuentes/OFL.txt`): fuente de los PDF
