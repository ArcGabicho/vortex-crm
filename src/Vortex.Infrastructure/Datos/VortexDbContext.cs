using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Vortex.Domain.Catalogo;
using Vortex.Domain.Configuracion;
using Vortex.Domain.Contactos;
using Vortex.Domain.Tareas;
using Vortex.Domain.Ventas;

namespace Vortex.Infrastructure.Datos;

/// <summary>
/// El modelo de la base de datos de Vortex, el mismo para SQLite (un equipo) y SQL Server (el
/// servidor de varias sucursales). Cada motor tiene su propia subclase con sus migraciones.
/// </summary>
public abstract class VortexDbContext(DbContextOptions opciones) : DbContext(opciones)
{
    /// <summary>El negocio es uno solo por base de datos: siempre se guarda con este Id.</summary>
    internal const int IdNegocio = 1;

    private static readonly JsonSerializerOptions OpcionesJson = new() { IgnoreReadOnlyProperties = true };

    public DbSet<Negocio> Negocios => Set<Negocio>();

    public DbSet<Empresa> Empresas => Set<Empresa>();

    public DbSet<Contacto> Contactos => Set<Contacto>();

    public DbSet<Oportunidad> Oportunidades => Set<Oportunidad>();

    public DbSet<Cotizacion> Cotizaciones => Set<Cotizacion>();

    public DbSet<Producto> Productos => Set<Producto>();

    public DbSet<Tarea> Tareas => Set<Tarea>();

    protected override void ConfigureConventions(ModelConfigurationBuilder convenciones)
    {
        // Montos, precios y cantidades con 4 decimales; se redondean a 2 al calcular los importes
        convenciones.Properties<decimal>().HavePrecision(18, 4);

        // Los enumerados se guardan con su nombre: la base se puede leer sin el código
        convenciones.Properties<TipoProducto>().HaveConversion<string>().HaveMaxLength(20);
        convenciones.Properties<RegimenTributario>().HaveConversion<string>().HaveMaxLength(20);
        convenciones.Properties<TipoTarea>().HaveConversion<string>().HaveMaxLength(20);
        convenciones.Properties<EstadoCotizacion>().HaveConversion<string>().HaveMaxLength(20);
        convenciones.Properties<EtapaOportunidad>().HaveConversion<string>().HaveMaxLength(20);
        convenciones.Properties<ModoIgv>().HaveConversion<string>().HaveMaxLength(20);
    }

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.Entity<Negocio>(negocio =>
        {
            negocio.ToTable("Negocio");
            negocio.Property<int>("Id").ValueGeneratedNever();
            negocio.HasKey("Id");
            negocio.Property(n => n.Ruc).HasMaxLength(11);
            negocio.Property(n => n.RazonSocial).HasMaxLength(200);
            negocio.Property(n => n.NombreComercial).HasMaxLength(200);
            negocio.Property(n => n.Direccion).HasMaxLength(300);
            negocio.Property(n => n.Ubigeo).HasMaxLength(6);
            negocio.Property(n => n.Telefono).HasMaxLength(30);
            negocio.Property(n => n.Email).HasMaxLength(200);

            negocio.OwnsMany(n => n.Sucursales, sucursal =>
            {
                sucursal.ToTable("Sucursales");
                sucursal.WithOwner().HasForeignKey("NegocioId");
                sucursal.HasKey(s => s.Id);
                sucursal.Property(s => s.Id).ValueGeneratedNever();
                sucursal.Property(s => s.Nombre).HasMaxLength(100);
                sucursal.Property(s => s.Direccion).HasMaxLength(300);
                sucursal.Property(s => s.Ubigeo).HasMaxLength(6);
                sucursal.Property(s => s.CodigoEstablecimiento).HasMaxLength(4);
            });
        });

        modelo.Entity<Empresa>(empresa =>
        {
            empresa.ToTable("Empresas");
            empresa.Property(e => e.Id).ValueGeneratedNever();
            empresa.Property(e => e.Ruc).HasMaxLength(11);
            empresa.HasIndex(e => e.Ruc);
            empresa.Property(e => e.RazonSocial).HasMaxLength(200);
            empresa.Property(e => e.NombreComercial).HasMaxLength(200);
            empresa.Property(e => e.Direccion).HasMaxLength(300);
            empresa.Property(e => e.Ubigeo).HasMaxLength(6);
            empresa.Property(e => e.Telefono).HasMaxLength(30);
            empresa.Property(e => e.Email).HasMaxLength(200);
            empresa.Property(e => e.EstadoSunat).HasMaxLength(50);
            empresa.Property(e => e.CondicionSunat).HasMaxLength(50);
        });

        modelo.Entity<Contacto>(contacto =>
        {
            contacto.ToTable("Contactos");
            contacto.Property(c => c.Id).ValueGeneratedNever();
            contacto.Property(c => c.Dni).HasMaxLength(8);
            contacto.Property(c => c.Nombres).HasMaxLength(100);
            contacto.Property(c => c.Apellidos).HasMaxLength(100);
            contacto.Property(c => c.Telefono).HasMaxLength(30);
            contacto.Property(c => c.Email).HasMaxLength(200);
            contacto.Property(c => c.Cargo).HasMaxLength(100);
            contacto.HasIndex(c => c.EmpresaId);
        });

        modelo.Entity<Oportunidad>(oportunidad =>
        {
            oportunidad.ToTable("Oportunidades");
            oportunidad.Property(o => o.Id).ValueGeneratedNever();
            oportunidad.Property(o => o.Titulo).HasMaxLength(200);
            oportunidad.Property(o => o.MotivoPerdida).HasMaxLength(500);
            oportunidad.HasIndex(o => o.EmpresaId);
            oportunidad.HasIndex(o => o.ContactoId);
        });

        modelo.Entity<Cotizacion>(cotizacion =>
        {
            cotizacion.ToTable("Cotizaciones");
            cotizacion.Property(c => c.Id).ValueGeneratedNever();
            cotizacion.HasIndex(c => c.Numero).IsUnique();
            cotizacion.HasIndex(c => c.EmpresaId);
            cotizacion.HasIndex(c => c.ContactoId);
            cotizacion.HasIndex(c => c.OportunidadId);

            // Las líneas van juntas, en orden, como JSON: siempre se leen y se guardan con su cotización
            cotizacion.Property(c => c.Lineas)
                .HasConversion(
                    lineas => JsonSerializer.Serialize(lineas, OpcionesJson),
                    json => JsonSerializer.Deserialize<List<LineaCotizacion>>(json, OpcionesJson) ?? new List<LineaCotizacion>(),
                    new ValueComparer<List<LineaCotizacion>>(
                        (a, b) => JsonSerializer.Serialize(a, OpcionesJson) == JsonSerializer.Serialize(b, OpcionesJson),
                        lineas => JsonSerializer.Serialize(lineas, OpcionesJson).GetHashCode(),
                        lineas => lineas.Select(l => l.Clonar()).ToList()));
        });

        modelo.Entity<Producto>(producto =>
        {
            producto.ToTable("Productos");
            producto.Property(p => p.Id).ValueGeneratedNever();
            producto.Property(p => p.Nombre).HasMaxLength(200);
            producto.Property(p => p.Codigo).HasMaxLength(50);
            producto.Property(p => p.Unidad).HasMaxLength(10);
        });

        modelo.Entity<Tarea>(tarea =>
        {
            tarea.ToTable("Tareas");
            tarea.Property(t => t.Id).ValueGeneratedNever();
            tarea.Property(t => t.Titulo).HasMaxLength(200);
            tarea.HasIndex(t => t.EmpresaId);
            tarea.HasIndex(t => t.ContactoId);
            tarea.HasIndex(t => t.OportunidadId);
        });
    }
}

/// <summary>La base de datos SQLite de una instalación de un solo equipo. Sus migraciones están en Datos/Migraciones/Sqlite.</summary>
public sealed class VortexDbContextSqlite(DbContextOptions<VortexDbContextSqlite> opciones) : VortexDbContext(opciones);
