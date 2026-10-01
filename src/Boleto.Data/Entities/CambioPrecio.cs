using System.ComponentModel.DataAnnotations;

namespace Boleto.Data.Entities;

/// <summary>
/// Auditoría de cambios del panel. Sirve para responder
/// "¿quién bajó el precio del Chivas y cuándo?" sin adivinar.
/// </summary>
public class CambioPrecio
{
    public long Id { get; set; }

    [MaxLength(40)] public string ProductoId { get; set; } = "";
    [MaxLength(120)] public string ProductoNombre { get; set; } = "";

    /// <summary>Precio, PrecioCombo, Stock, ComboAditivoDescuento…</summary>
    [MaxLength(40)] public string Campo { get; set; } = "";

    /* 160 y no 20. El aditivo del combo dejó de ser un nombre suelto: ahora
       puede traer varias opciones separadas por «|», y "Coca Cola 1.5 L|
       Everest 1.5 L|Ginger Ale 1 L" son 43 caracteres. Con 20, registrar el
       cambio guardaba "Coca Cola 1.5 L|Ever" —que no sirve para auditar
       nada— y, peor, el camino de publicar escribía el valor crudo y SQL
       Server abortaba la operación entera: el panel devolvía 500 y no se
       publicaba nada. */
    [MaxLength(160)] public string ValorAnterior { get; set; } = "";
    [MaxLength(160)] public string ValorNuevo { get; set; } = "";

    [MaxLength(120)] public string Usuario { get; set; } = "";
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
}
