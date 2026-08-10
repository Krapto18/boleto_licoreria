using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Boleto.Data.Migrations
{
    /// <inheritdoc />
    public partial class ZonasPagosEdad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Pagos",
                table: "Tienda",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TiempoEntrega",
                table: "Tienda",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Verificar18",
                table: "Tienda",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Zonas",
                table: "Tienda",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Pagos",
                table: "Tienda");

            migrationBuilder.DropColumn(
                name: "TiempoEntrega",
                table: "Tienda");

            migrationBuilder.DropColumn(
                name: "Verificar18",
                table: "Tienda");

            migrationBuilder.DropColumn(
                name: "Zonas",
                table: "Tienda");
        }
    }
}
