using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Boleto.Data.Migrations
{
    /// <inheritdoc />
    public partial class PanelCompleto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComboTexto",
                table: "Productos");

            migrationBuilder.AddColumn<string>(
                name: "Banners",
                table: "Tienda",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ComboAcompanante",
                table: "Productos",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ComboHielo",
                table: "Productos",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Banners",
                table: "Tienda");

            migrationBuilder.DropColumn(
                name: "ComboAcompanante",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "ComboHielo",
                table: "Productos");

            migrationBuilder.AddColumn<string>(
                name: "ComboTexto",
                table: "Productos",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");
        }
    }
}
