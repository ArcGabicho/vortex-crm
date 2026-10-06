using Vortex.Domain.Comun;

namespace Vortex.Infrastructure.Comun;

/// <summary>
/// Los ubigeos del INEI que viajan dentro de la app (Recursos/Ubigeo/ubigeos-inei.csv): no hace
/// falta conexión ni base de datos para elegir un distrito.
/// </summary>
public static class UbigeosInei
{
    private const string Recurso = "Vortex.Ubigeo.ubigeos-inei.csv";

    public static CatalogoUbigeos Cargar()
    {
        using var flujo = typeof(UbigeosInei).Assembly.GetManifestResourceStream(Recurso)
            ?? throw new InvalidOperationException($"No se encontró el recurso {Recurso}.");
        using var lector = new StreamReader(flujo);

        var ubigeos = new List<Ubigeo>();
        lector.ReadLine(); // cabecera: ubigeo;departamento;provincia;distrito

        while (lector.ReadLine() is { } linea)
        {
            if (string.IsNullOrWhiteSpace(linea))
            {
                continue;
            }

            var campos = linea.Split(';');
            ubigeos.Add(new Ubigeo(campos[0], campos[1], campos[2], campos[3]));
        }

        return new CatalogoUbigeos(ubigeos);
    }
}
