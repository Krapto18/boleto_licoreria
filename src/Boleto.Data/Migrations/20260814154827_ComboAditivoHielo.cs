using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Boleto.Data.Migrations
{
    /// <inheritdoc />
    public partial class ComboAditivoHielo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ComboAcompananteActivo",
                table: "Productos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ComboAcompanantePrecio",
                table: "Productos",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "ComboHieloActivo",
                table: "Productos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ComboHieloPrecio",
                table: "Productos",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            /* Los flags nacen en false, y ese es justo el valor que apagaría
               el aditivo y el hielo de todos los combos que YA existen: el
               cliente dejaría de ver qué incluye su combo y nadie se
               enteraría hasta que alguien lo comparara con el catálogo
               impreso. Se encienden donde ya había un nombre guardado, que
               es lo que la web venía mostrando. El precio se queda en 0: es
               un dato nuevo que solo el dueño puede saber. */
            migrationBuilder.Sql(
                "UPDATE [Productos] SET [ComboAcompananteActivo] = 1 " +
                "WHERE [ComboAcompanante] IS NOT NULL AND LTRIM(RTRIM([ComboAcompanante])) <> ''");
            migrationBuilder.Sql(
                "UPDATE [Productos] SET [ComboHieloActivo] = 1 " +
                "WHERE [ComboHielo] IS NOT NULL AND LTRIM(RTRIM([ComboHielo])) <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComboAcompananteActivo",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "ComboAcompanantePrecio",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "ComboHieloActivo",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "ComboHieloPrecio",
                table: "Productos");
        }
    }
}
