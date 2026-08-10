using System.ComponentModel.DataAnnotations;

namespace Boleto.Data.Entities;

public class Producto
{
    [MaxLength(40)]
    public string Id { get; set; } = "";

    [MaxLength(120)] public string Nombre { get; set; } = "";
    [MaxLength(40)]  public string Presentacion { get; set; } = "";
    [MaxLength(40)]  public string Categoria { get; set; } = "";
    [MaxLength(60)]  public string Grupo { get; set; } = "";

    public decimal Precio { get; set; }
    public decimal? PrecioCombo { get; set; }

    /* Desglose del combo. Antes iba todo junto en un texto; separado
       permite mostrarlo y enviarlo como dos ítems distintos. */
    [MaxLength(60)] public string ComboAcompanante { get; set; } = "";
    [MaxLength(60)] public string ComboHielo { get; set; } = "";

    /// <summary>Aplica a la promoción de Coca Cola gratis (whiskys y rones).</summary>
    public bool Promo { get; set; }

    /// <summary>false = agotado. No se oculta en la web: se muestra sin poder pedirse.</summary>
    public bool Stock { get; set; } = true;

    [MaxLength(9)]   public string Color { get; set; } = "#888888";
    [MaxLength(200)] public string Imagen { get; set; } = "";

    public int Orden { get; set; }
    public DateTime ActualizadoUtc { get; set; } = DateTime.UtcNow;
}
