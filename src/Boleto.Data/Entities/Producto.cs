using System.ComponentModel.DataAnnotations;

namespace Boleto.Data.Entities;

public class Producto
{
    [MaxLength(40)]
    public string Id { get; set; } = "";

    [MaxLength(120)] public string Nombre { get; set; } = "";
    [MaxLength(40)] public string Presentacion { get; set; } = "";
    [MaxLength(40)] public string Categoria { get; set; } = "";
    [MaxLength(60)] public string Grupo { get; set; } = "";

    public decimal Precio { get; set; }
    public decimal? PrecioCombo { get; set; }

    /* ── COMPOSICIÓN DEL COMBO ───────────────────────────────────
       El combo es la botella más un aditivo y/o hielo. De cada uno se
       guardan tres cosas:

         Nombre   qué es — "Coca Cola 1.5 L", "Ginger 1 L", "Hielo 3 kg".
                  Es lo único que ve el cliente.
         Precio   cuánto costaría suelto. No se publica: le sirve al
                  dueño para saber qué está regalando al fijar el combo.
         Activo   si la tienda lo ofrece. Apagarlo saca el ítem del combo
                  sin perder ni el nombre ni el precio, para poder volver
                  a encenderlo cuando haya stock del aditivo.
         Descuento cuánto se le baja al combo si el CLIENTE decide no
                  llevarlo. Cero —el valor por defecto— significa que el
                  combo es un precio de paquete: quien no quiere el hielo
                  no lo recibe y paga lo mismo. Lo decide el vendedor,
                  producto por producto.

       El aditivo admite VARIAS opciones separadas por «|»:

           Coca Cola 1.5 L|Everest 1.5 L|Ginger Ale 1 L

       y el cliente elige una en la web. Quien quiere gaseosa negra y
       quien quiere Everest piden el mismo combo sin tener que escribirlo
       aparte en el WhatsApp. Una sola opción —que es el caso de siempre—
       se comporta igual que antes: no hay nada que elegir y se muestra
       como texto.

       El hielo no se elige: es uno solo. */
    [MaxLength(300)] public string ComboAcompanante { get; set; } = "";
    public decimal ComboAcompanantePrecio { get; set; }
    public bool ComboAcompananteActivo { get; set; }
    public decimal ComboAcompananteDescuento { get; set; }

    [MaxLength(60)] public string ComboHielo { get; set; } = "";
    public decimal ComboHieloPrecio { get; set; }
    public bool ComboHieloActivo { get; set; }
    public decimal ComboHieloDescuento { get; set; }

    /// <summary>Aplica a la promoción de Coca Cola gratis (whiskys y rones).</summary>
    public bool Promo { get; set; }

    /// <summary>false = agotado. No se oculta en la web: se muestra sin poder pedirse.</summary>
    public bool Stock { get; set; } = true;

    [MaxLength(9)] public string Color { get; set; } = "#888888";
    [MaxLength(200)] public string Imagen { get; set; } = "";

    /* Baja lógica, no borrado. Si se eliminara la fila se perdería la
       trazabilidad de la auditoría y el histórico de precios. Un
       producto inactivo desaparece del catálogo pero conserva su
       registro. */
    public bool Activo { get; set; } = true;

    public int Orden { get; set; }
    public DateTime ActualizadoUtc { get; set; } = DateTime.UtcNow;

    /* ── Opciones del aditivo ────────────────────────────────────
       Viven acá y no en el servicio porque las escriben tres caminos
       distintos —el editor de producto, la publicación masiva y el alta—
       y los tres tienen que guardar exactamente la misma forma. */

    /// <summary>
    /// Las opciones de aditivo ya limpias: sin vacías, sin repetidas y
    /// sin espacios de sobra. Una sola es el caso normal; varias
    /// significan que el cliente elige.
    /// </summary>
    public static string[] Opciones(string? texto) =>
        (texto ?? "")
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    /// <summary>Forma canónica para guardar en la columna.</summary>
    public static string NormalizarOpciones(string? texto) =>
        string.Join('|', Opciones(texto));
}
