using Vortex.Domain.Comun;
using Vortex.Domain.Configuracion;
using Vortex.Domain.Contactos;
using Vortex.Domain.Ventas;
using Xunit;

namespace Vortex.Domain.Tests.Ventas;

public class CotizacionTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 1, 15, 0, 0, TimeSpan.Zero);

    private static Cotizacion ConUnaLinea(ModoIgv modo, decimal cantidad = 50, decimal precio = 30)
    {
        var cotizacion = new Cotizacion { ModoIgv = modo, ContactoId = Guid.NewGuid() };
        cotizacion.Lineas.Add(new LineaCotizacion { Descripcion = "Polo bordado", Cantidad = cantidad, PrecioUnitario = precio });
        return cotizacion;
    }

    [Fact]
    public void ConIgvIncluidoSeDesglosaDelTotal()
    {
        // 1,500 / 1.18 = 1,271.186... → 1,271.19
        Assert.Equal(new TotalesCotizacion(1271.19m, 228.81m, 1500m), ConUnaLinea(ModoIgv.Incluido).Totales);
    }

    [Fact]
    public void ConIgvAdicionalSeSumaAlFinal() =>
        Assert.Equal(new TotalesCotizacion(1500m, 270m, 1770m), ConUnaLinea(ModoIgv.Adicional).Totales);

    [Fact]
    public void SinIgvElTotalEsLaSuma() =>
        Assert.Equal(new TotalesCotizacion(1500m, 0m, 1500m), ConUnaLinea(ModoIgv.NoAplica).Totales);

    [Fact]
    public void ElImporteDeCadaLineaSeRedondeaACentimos() =>
        Assert.Equal(100.00m, new LineaCotizacion { Cantidad = 3, PrecioUnitario = 33.333m }.Importe);

    [Fact]
    public void SinNumeroElCodigoDiceNuevaYConNumeroUsaElCorrelativo()
    {
        var cotizacion = new Cotizacion();
        Assert.Equal("Nueva", cotizacion.Codigo);

        cotizacion.Numero = 7;
        Assert.Equal("COT-0007", cotizacion.Codigo);
    }

    [Fact]
    public void EnviarlaPasaLaOportunidadDeProspectoACotizadoYLeDaElMonto()
    {
        var oportunidad = new Oportunidad();
        var cotizacion = ConUnaLinea(ModoIgv.Incluido);
        cotizacion.OportunidadId = oportunidad.Id;

        var cambio = cotizacion.CambiarEstado(EstadoCotizacion.Enviada, oportunidad, Ahora);

        Assert.True(cambio);
        Assert.Equal(EstadoCotizacion.Enviada, cotizacion.Estado);
        Assert.Equal(EtapaOportunidad.Cotizado, oportunidad.Etapa);
        Assert.Equal(1500m, oportunidad.Monto);
    }

    [Fact]
    public void EnviarlaNoRetrocedeUnaOportunidadQueYaEstaEnNegociacionNiCambiaSuMonto()
    {
        var oportunidad = new Oportunidad { Monto = 2000 };
        oportunidad.CambiarEtapa(EtapaOportunidad.Negociacion, Ahora);
        var cotizacion = ConUnaLinea(ModoIgv.Incluido);
        cotizacion.OportunidadId = oportunidad.Id;

        var cambio = cotizacion.CambiarEstado(EstadoCotizacion.Enviada, oportunidad, Ahora);

        Assert.False(cambio);
        Assert.Equal(EtapaOportunidad.Negociacion, oportunidad.Etapa);
        Assert.Equal(2000m, oportunidad.Monto);
    }

    [Fact]
    public void AceptarlaGanaLaOportunidad()
    {
        var oportunidad = new Oportunidad { Monto = 1500 };
        var cotizacion = ConUnaLinea(ModoIgv.Incluido);
        cotizacion.OportunidadId = oportunidad.Id;

        Assert.True(cotizacion.CambiarEstado(EstadoCotizacion.Aceptada, oportunidad, Ahora));
        Assert.Equal(EtapaOportunidad.Ganado, oportunidad.Etapa);
        Assert.Equal(Ahora, oportunidad.CerradoEn);
    }

    [Fact]
    public void RechazarlaNoPierdeLaOportunidadPorqueSePuedeVolverACotizar()
    {
        var oportunidad = new Oportunidad();
        var cotizacion = ConUnaLinea(ModoIgv.Incluido);
        cotizacion.OportunidadId = oportunidad.Id;

        Assert.False(cotizacion.CambiarEstado(EstadoCotizacion.Rechazada, oportunidad, Ahora));
        Assert.Equal(EtapaOportunidad.Prospecto, oportunidad.Etapa);
    }

    [Fact]
    public void NoAceptaUnaOportunidadQueNoEsLaSuya()
    {
        var cotizacion = ConUnaLinea(ModoIgv.Incluido);
        cotizacion.OportunidadId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => cotizacion.CambiarEstado(EstadoCotizacion.Enviada, new Oportunidad(), Ahora));
    }

    [Fact]
    public void ValidarRevisaClienteLineasYFechas()
    {
        var cotizacion = new Cotizacion { ValidaHasta = new DateOnly(2026, 1, 1), FechaEmision = new DateOnly(2026, 2, 1) };
        cotizacion.Lineas.Add(new LineaCotizacion { Descripcion = "", Cantidad = 0, PrecioUnitario = -1 });

        var errores = cotizacion.Validar();

        Assert.Contains(errores, e => e.Contains("cliente"));
        Assert.Contains(errores, e => e.Contains("falta la descripción"));
        Assert.Contains(errores, e => e.Contains("cantidad"));
        Assert.Contains(errores, e => e.Contains("precio"));
        Assert.Contains(errores, e => e.Contains("validez"));
        Assert.Empty(ConUnaLinea(ModoIgv.Incluido).Validar());
    }

    [Fact]
    public void SinLineasNoEsValida() =>
        Assert.Contains(new Cotizacion { ContactoId = Guid.NewGuid() }.Validar(), e => e.Contains("al menos un producto"));

    [Fact]
    public void ClonarCopiaTambienLasLineas()
    {
        var original = ConUnaLinea(ModoIgv.Incluido);
        var copia = original.Clonar();

        copia.Lineas[0].Cantidad = 999;
        copia.Lineas.Add(new LineaCotizacion());

        Assert.Equal(50, original.Lineas[0].Cantidad);
        Assert.Single(original.Lineas);
    }

    [Fact]
    public void ElClienteConEmpresaVaASuRazonSocialALaAtencionDelContacto()
    {
        var empresa = new Empresa { Ruc = "20131312955", RazonSocial = "TEXTILES ANDINA S.A.C.", Telefono = "014445555" };
        var contacto = new Contacto { Nombres = "Rosa", Apellidos = "Quispe", Telefono = "987654321" };

        var cliente = ClienteCotizacion.Crear(empresa, contacto);

        Assert.Equal("TEXTILES ANDINA S.A.C.", cliente.Nombre);
        Assert.Equal("RUC 20131312955", cliente.Documento);
        Assert.Equal("Rosa Quispe", cliente.Atencion);
        Assert.Equal("987654321", cliente.Telefono);
    }

    [Fact]
    public void LaDireccionDelClienteLlevaSuDistritoSiSeDaElCatalogo()
    {
        var empresa = new Empresa { Ruc = "20131312955", RazonSocial = "TEXTILES ANDINA S.A.C.", Direccion = "Av. Larco 123", Ubigeo = "150122" };
        var ubigeos = new CatalogoUbigeos([new Ubigeo("150122", "LIMA", "LIMA", "MIRAFLORES")]);

        Assert.Equal("Av. Larco 123, MIRAFLORES - LIMA - LIMA", ClienteCotizacion.Crear(empresa, null, ubigeos).Direccion);
        Assert.Equal("Av. Larco 123", ClienteCotizacion.Crear(empresa, null).Direccion);
    }

    [Fact]
    public void ElClienteSinEmpresaVaANombreDeLaPersona()
    {
        var cliente = ClienteCotizacion.Crear(null, new Contacto { Nombres = "Rosa", Apellidos = "Quispe", Dni = "46027897" });

        Assert.Equal("Rosa Quispe", cliente.Nombre);
        Assert.Equal("DNI 46027897", cliente.Documento);
        Assert.Null(cliente.Atencion);
    }

    [Fact]
    public void ElMensajeDeWhatsAppResumeLaCotizacion()
    {
        var cotizacion = ConUnaLinea(ModoIgv.Incluido);
        cotizacion.Numero = 1;
        cotizacion.ValidaHasta = new DateOnly(2026, 10, 16);

        var mensaje = MensajeWhatsApp.Cotizacion(cotizacion, new Negocio { RazonSocial = "Bordados Rosita" }, "Rosa");

        Assert.StartsWith("Hola Rosa,\n", mensaje.ReplaceLineEndings("\n"));
        Assert.Contains("cotización COT-0001 de Bordados Rosita", mensaje);
        Assert.Contains("• 50 × Polo bordado: S/ 1,500.00", mensaje);
        Assert.Contains("*Total: S/ 1,500.00* (incluye IGV)", mensaje);
        Assert.Contains("Válida hasta el 16/10/2026.", mensaje);
    }

    [Fact]
    public void ElMensajeResumeLasLineasQueSobranDelMaximo()
    {
        var cotizacion = new Cotizacion();
        for (var i = 0; i < MensajeWhatsApp.MaximoLineas + 3; i++)
        {
            cotizacion.Lineas.Add(new LineaCotizacion { Descripcion = $"Item {i}", PrecioUnitario = 1 });
        }

        Assert.Contains("… y 3 más", MensajeWhatsApp.Cotizacion(cotizacion, new Negocio(), null));
    }

    [Fact]
    public void ElEnlaceDeWhatsAppLlevaElNumeroYElTextoCodificado()
    {
        Assert.Equal("https://wa.me/51987654321?text=Hola%20Rosa", MensajeWhatsApp.Enlace("987 654 321", "Hola Rosa"));
        Assert.Null(MensajeWhatsApp.Enlace("(01) 315-3300", "Hola"));
    }

    [Fact]
    public void EnElNuevoRusLasCotizacionesSalenSinIgv()
    {
        Assert.Equal(ModoIgv.NoAplica, new Negocio { Regimen = RegimenTributario.NuevoRus }.ModoIgvPredeterminado);
        Assert.Equal(ModoIgv.Incluido, new Negocio { Regimen = RegimenTributario.MypeTributario }.ModoIgvPredeterminado);
    }
}
