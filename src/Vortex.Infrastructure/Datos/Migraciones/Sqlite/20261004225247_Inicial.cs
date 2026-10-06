using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vortex.Infrastructure.Datos.Migraciones.Sqlite
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contactos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Dni = table.Column<string>(type: "TEXT", maxLength: 8, nullable: true),
                    Nombres = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Apellidos = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Cargo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    EmpresaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreadoEn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contactos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cotizaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ContactoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OportunidadId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FechaEmision = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ValidaHasta = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ModoIgv = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Lineas = table.Column<string>(type: "TEXT", nullable: false),
                    Condiciones = table.Column<string>(type: "TEXT", nullable: true),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreadoEn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cotizaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ruc = table.Column<string>(type: "TEXT", maxLength: 11, nullable: false),
                    RazonSocial = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    NombreComercial = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Direccion = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Ubigeo = table.Column<string>(type: "TEXT", maxLength: 6, nullable: true),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    EstadoSunat = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CondicionSunat = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreadoEn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Negocio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Ruc = table.Column<string>(type: "TEXT", maxLength: 11, nullable: true),
                    RazonSocial = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    NombreComercial = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Direccion = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Ubigeo = table.Column<string>(type: "TEXT", maxLength: 6, nullable: true),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Regimen = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CondicionesPredeterminadas = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Negocio", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Oportunidades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    EmpresaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ContactoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Monto = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Etapa = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FechaCierreEstimada = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    MotivoPerdida = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notas = table.Column<string>(type: "TEXT", nullable: true),
                    CreadoEn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CerradoEn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Oportunidades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: true),
                    Unidad = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Precio = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    PrecioIncluyeIgv = table.Column<bool>(type: "INTEGER", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tareas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Hora = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                    AvisarMinutosAntes = table.Column<int>(type: "INTEGER", nullable: true),
                    EmpresaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ContactoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OportunidadId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Notas = table.Column<string>(type: "TEXT", nullable: true),
                    CreadoEn = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CompletadaEn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tareas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sucursales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Direccion = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Ubigeo = table.Column<string>(type: "TEXT", maxLength: 6, nullable: true),
                    CodigoEstablecimiento = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    Activa = table.Column<bool>(type: "INTEGER", nullable: false),
                    NegocioId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sucursales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sucursales_Negocio_NegocioId",
                        column: x => x.NegocioId,
                        principalTable: "Negocio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contactos_EmpresaId",
                table: "Contactos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Cotizaciones_ContactoId",
                table: "Cotizaciones",
                column: "ContactoId");

            migrationBuilder.CreateIndex(
                name: "IX_Cotizaciones_EmpresaId",
                table: "Cotizaciones",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Cotizaciones_Numero",
                table: "Cotizaciones",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cotizaciones_OportunidadId",
                table: "Cotizaciones",
                column: "OportunidadId");

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_Ruc",
                table: "Empresas",
                column: "Ruc");

            migrationBuilder.CreateIndex(
                name: "IX_Oportunidades_ContactoId",
                table: "Oportunidades",
                column: "ContactoId");

            migrationBuilder.CreateIndex(
                name: "IX_Oportunidades_EmpresaId",
                table: "Oportunidades",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Sucursales_NegocioId",
                table: "Sucursales",
                column: "NegocioId");

            migrationBuilder.CreateIndex(
                name: "IX_Tareas_ContactoId",
                table: "Tareas",
                column: "ContactoId");

            migrationBuilder.CreateIndex(
                name: "IX_Tareas_EmpresaId",
                table: "Tareas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Tareas_OportunidadId",
                table: "Tareas",
                column: "OportunidadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contactos");

            migrationBuilder.DropTable(
                name: "Cotizaciones");

            migrationBuilder.DropTable(
                name: "Empresas");

            migrationBuilder.DropTable(
                name: "Oportunidades");

            migrationBuilder.DropTable(
                name: "Productos");

            migrationBuilder.DropTable(
                name: "Sucursales");

            migrationBuilder.DropTable(
                name: "Tareas");

            migrationBuilder.DropTable(
                name: "Negocio");
        }
    }
}
