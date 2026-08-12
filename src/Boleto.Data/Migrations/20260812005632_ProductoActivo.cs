using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Boleto.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProductoActivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /* defaultValue: true, no false.
               El "= true" de la entidad es un default de C#: solo aplica a
               objetos nuevos, nunca llega al backfill de SQL. Con false, las
               filas que ya existen quedan inactivas y CatalogoService las
               filtra: la landing sale vacía en la primera publicación. */
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Productos",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Productos");
        }
    }
}
