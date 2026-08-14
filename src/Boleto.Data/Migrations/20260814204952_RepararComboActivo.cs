using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Boleto.Data.Migrations
{
    /// <inheritdoc />
    public partial class RepararComboActivo : Migration
    {
        /* Reparacion de un fallo que ya habia quedado guardado en datos.

           El editor de producto escribia el nombre del acompañante y del
           hielo pero nunca encendia su casilla —vive en la pestaña de
           precios y nace apagada—, asi que lo escrito quedaba invisible
           en la web: la auditoria lo registraba, el panel lo mostraba y
           el cliente no lo veia. Ya esta arreglado en el codigo, donde
           guardar desde el editor afirma la composicion del combo.

           Queda por arreglar lo que se guardo mientras el fallo existia.
           Se enciende donde hay nombre escrito y la casilla apagada.

           Es seguro hacerlo ahora y no mas adelante: el apagado
           deliberado —"se acabo la gaseosa"— se hace desde la pestaña de
           precios, que se estreno con esta misma tanda de cambios y aun
           no ha llegado a produccion. No hay ningun apagado a mano que
           esto pueda pisar.

           Sin Down: no se puede distinguir lo que esta reparacion
           encendio de lo que ya venia encendido. */

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE [Productos] SET [ComboAcompananteActivo] = 1 " +
                "WHERE [ComboAcompananteActivo] = 0 AND LTRIM(RTRIM([ComboAcompanante])) <> ''");
            migrationBuilder.Sql(
                "UPDATE [Productos] SET [ComboHieloActivo] = 1 " +
                "WHERE [ComboHieloActivo] = 0 AND LTRIM(RTRIM([ComboHielo])) <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
