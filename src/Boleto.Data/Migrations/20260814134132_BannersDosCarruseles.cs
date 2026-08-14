using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Boleto.Data.Migrations
{
    /// <inheritdoc />
    public partial class BannersDosCarruseles : Migration
    {
        /* La página pasa de un carrusel a dos, de cinco banners cada uno.
           Diez líneas de hasta ~380 caracteres no entran en 2000, así que
           la columna se ensancha. Solo ensancha: nada de lo guardado se
           pierde y el Down solo se puede aplicar si vuelve a caber. */

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Banners",
                table: "Tienda",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Banners",
                table: "Tienda",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);
        }
    }
}
