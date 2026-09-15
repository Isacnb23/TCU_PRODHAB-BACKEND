using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prodhab.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEsSuperAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsSuperAdmin",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Backfill: en bases de datos que ya tenían usuarios antes de este campo,
            // el Admin más antiguo (menor Id) pasa a ser el superusuario protegido,
            // igual que hará DbSeeder para instalaciones nuevas.
            migrationBuilder.Sql(@"
                UPDATE Usuarios
                SET EsSuperAdmin = 1
                WHERE Id = (
                    SELECT TOP 1 Id FROM Usuarios WHERE Rol = 'Admin' ORDER BY Id ASC
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsSuperAdmin",
                table: "Usuarios");
        }
    }
}
