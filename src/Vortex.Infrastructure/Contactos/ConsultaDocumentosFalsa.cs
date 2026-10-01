using Vortex.Domain.Contactos;

namespace Vortex.Infrastructure.Contactos;

/// <summary>
/// Proveedor de prueba de RUC/DNI: inventa datos a partir del número, siempre los
/// mismos para el mismo número. Se reemplazará por un proveedor real (apis.net.pe,
/// Decolecta, etc.) cuando haya una cuenta.
/// </summary>
/// <remarks>
/// Para probar el caso "no encontrado": un DNI que termina en 0000, o un RUC cuyos
/// dígitos 7 a 10 son 0000.
/// </remarks>
public sealed class ConsultaDocumentosFalsa(TimeSpan? latencia = null) : IConsultaDocumentos
{
    private static readonly string[] Nombres =
        ["MARÍA ELENA", "JOSÉ LUIS", "ROSA", "CARLOS ALBERTO", "LUCÍA", "JUAN CARLOS", "ANA SOFÍA", "PEDRO"];

    private static readonly string[] Apellidos =
        ["QUISPE", "FLORES", "MAMANI", "RODRÍGUEZ", "HUAMÁN", "GARCÍA", "CHÁVEZ", "TORRES", "RAMOS", "VARGAS"];

    private static readonly string[] Rubros =
        ["COMERCIAL", "INVERSIONES", "DISTRIBUIDORA", "SERVICIOS GENERALES", "CORPORACIÓN", "IMPORTACIONES"];

    private static readonly string[] Marcas =
        ["ANDINA", "DEL SUR", "PACÍFICO", "AMAZÓNICA", "LOS INCAS", "MISTI", "CHAVÍN", "NAZCA"];

    private static readonly string[] Direcciones =
        ["AV. JAVIER PRADO ESTE", "JR. DE LA UNIÓN", "AV. AREQUIPA", "CALLE MERCADERES", "AV. LA MARINA", "JR. GAMARRA"];

    private static readonly string[] Distritos =
        ["LIMA - LIMA - SAN ISIDRO", "LIMA - LIMA - LA VICTORIA", "AREQUIPA - AREQUIPA - CERCADO", "CUSCO - CUSCO - WANCHAQ", "LA LIBERTAD - TRUJILLO - TRUJILLO"];

    private readonly TimeSpan latencia = latencia ?? TimeSpan.FromMilliseconds(400);

    public bool EsDePrueba => true;

    public async Task<DatosRuc?> ConsultarRucAsync(string ruc, CancellationToken cancellationToken = default)
    {
        await Task.Delay(latencia, cancellationToken);

        if (!DocumentoIdentidad.EsRucValido(ruc) || ruc[6..10] == "0000")
        {
            return null;
        }

        var semilla = long.Parse(ruc);
        var direccion = $"{Elegir(Direcciones, semilla)} {100 + semilla % 900} {Elegir(Distritos, semilla / 7)}";

        if (DocumentoIdentidad.EsRucDePersonaJuridica(ruc))
        {
            var marca = Elegir(Marcas, semilla / 3);
            var razonSocial = $"{Elegir(Rubros, semilla)} {marca} S.A.C.";
            return new DatosRuc(ruc, razonSocial, marca, direccion, "ACTIVO", "HABIDO");
        }

        // Persona natural con negocio: el RUC 10 contiene el DNI en los dígitos 3 a 10
        var persona = CrearPersona(ruc[2..10]);
        return new DatosRuc(ruc, $"{persona.Apellidos} {persona.Nombres}", null, direccion, "ACTIVO", "HABIDO");
    }

    public async Task<DatosDni?> ConsultarDniAsync(string dni, CancellationToken cancellationToken = default)
    {
        await Task.Delay(latencia, cancellationToken);

        if (!DocumentoIdentidad.EsDniValido(dni) || dni.EndsWith("0000", StringComparison.Ordinal))
        {
            return null;
        }

        return CrearPersona(dni);
    }

    private static DatosDni CrearPersona(string dni)
    {
        var semilla = long.Parse(dni);
        return new DatosDni(
            dni,
            Elegir(Nombres, semilla),
            Elegir(Apellidos, semilla / 11),
            Elegir(Apellidos, semilla / 13));
    }

    private static string Elegir(string[] opciones, long semilla) => opciones[semilla % opciones.Length];
}
