using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prodhab.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCampoAObservacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Campo",
                table: "Observaciones",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Campo",
                table: "Observaciones");
        }
    }
}
