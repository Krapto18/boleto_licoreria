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

    /// <summary>Precio, PrecioCombo o Stock.</summary>
    [MaxLength(20)] public string Campo { get; set; } = "";

    [MaxLength(20)] public string ValorAnterior { get; set; } = "";
    [MaxLength(20)] public string ValorNuevo { get; set; } = "";

    [MaxLength(120)] public string Usuario { get; set; } = "";
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
}
