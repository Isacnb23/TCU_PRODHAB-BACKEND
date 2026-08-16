using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prodhab.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Expedientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroExpediente = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Entidad = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Anio = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PasoActual = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expedientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Expedientes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DatosFormularios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpedienteId = table.Column<int>(type: "int", nullable: false),
                    Paso = table.Column<int>(type: "int", nullable: false),
                    DatosJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Completado = table.Column<bool>(type: "bit", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatosFormularios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatosFormularios_Expedientes_ExpedienteId",
                        column: x => x.ExpedienteId,
                        principalTable: "Expedientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistorialExpedientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpedienteId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Detalle = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaCambio = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialExpedientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialExpedientes_Expedientes_ExpedienteId",
                        column: x => x.ExpedienteId,
                        principalTable: "Expedientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subsanaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpedienteId = table.Column<int>(type: "int", nullable: false),
                    Paso = table.Column<int>(type: "int", nullable: false),
                    Campo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TextoJustificacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ArchivoRuta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ArchivoNombre = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ArchivoMimeType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ArchivoExtension = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ArchivoTamanoBytes = table.Column<long>(type: "bigint", nullable: true),
                    ArchivoHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaSubsanacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subsanaciones", x => x.Id);
                    table.CheckConstraint("CK_Subsanacion_JustificacionOArchivo", "([TextoJustificacion] IS NOT NULL AND LEN([TextoJustificacion]) > 0) OR [ArchivoRuta] IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_Subsanaciones_Expedientes_ExpedienteId",
                        column: x => x.ExpedienteId,
                        principalTable: "Expedientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatosFormularios_ExpedienteId_Paso",
                table: "DatosFormularios",
                columns: new[] { "ExpedienteId", "Paso" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_NumeroExpediente",
                table: "Expedientes",
                column: "NumeroExpediente",
                unique: true,
                filter: "[NumeroExpediente] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_UsuarioId",
                table: "Expedientes",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialExpedientes_ExpedienteId",
                table: "HistorialExpedientes",
                column: "ExpedienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Subsanaciones_ExpedienteId",
                table: "Subsanaciones",
                column: "ExpedienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DatosFormularios");

            migrationBuilder.DropTable(
                name: "HistorialExpedientes");

            migrationBuilder.DropTable(
                name: "Subsanaciones");

            migrationBuilder.DropTable(
                name: "Expedientes");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
