using System.Text.Json.Serialization;

namespace Boleto.Web.Models;

/// <summary>
/// Se serializa con las mismas claves cortas que usaba data.js.
/// Así el JavaScript del front no cambia ni una línea.
/// </summary>
public record ProductoDto
{
    [JsonPropertyName("id")]    public string Id { get; init; } = "";
    [JsonPropertyName("n")]     public string N { get; init; } = "";
    [JsonPropertyName("v")]     public string V { get; init; } = "";
    [JsonPropertyName("c")]     public string C { get; init; } = "";
    [JsonPropertyName("g")]     public string G { get; init; } = "";
    [JsonPropertyName("p")]     public decimal P { get; init; }
    [JsonPropertyName("combo")] public decimal? Combo { get; init; }
    [JsonPropertyName("aco")]   public string Acompanante { get; init; } = "";
    [JsonPropertyName("hie")]   public string Hielo { get; init; } = "";
    [JsonPropertyName("promo")] public bool Promo { get; init; }
    [JsonPropertyName("stock")] public bool Stock { get; init; }
    [JsonPropertyName("col")]   public string Col { get; init; } = "";
    [JsonPropertyName("img")]   public string Img { get; init; } = "";
}

public record ConfigDto
{
    [JsonPropertyName("tienda")]     public string Tienda { get; init; } = "";
    [JsonPropertyName("telefono")]   public string Telefono { get; init; } = "";
    [JsonPropertyName("instagram")]  public string Instagram { get; init; } = "";
    [JsonPropertyName("tiktok")]     public string TikTok { get; init; } = "";
    [JsonPropertyName("moneda")]     public string Moneda { get; init; } = "S/";
    [JsonPropertyName("abierto247")] public bool Abierto247 { get; init; }
    [JsonPropertyName("promos")]     public string[] Promos { get; init; } = [];
    [JsonPropertyName("direccion")]  public string Direccion { get; init; } = "";
    [JsonPropertyName("distrito")]   public string Distrito { get; init; } = "";
    [JsonPropertyName("latitud")]    public string Latitud { get; init; } = "";
    [JsonPropertyName("longitud")]   public string Longitud { get; init; } = "";
    [JsonPropertyName("zonas")]      public ZonaDto[] Zonas { get; init; } = [];
    [JsonPropertyName("pagos")]      public string[] Pagos { get; init; } = [];
    [JsonPropertyName("tiempoEntrega")] public string TiempoEntrega { get; init; } = "";
    [JsonPropertyName("banners")]    public BannerDto[] Banners { get; init; } = [];
    [JsonPropertyName("verificar18")]   public bool Verificar18 { get; init; }
    [JsonPropertyName("ga4")]        public string Ga4 { get; init; } = "";
    [JsonPropertyName("metaPixel")]  public string MetaPixel { get; init; } = "";
}

public record ZonaDto
{
    [JsonPropertyName("n")] public string Nombre { get; init; } = "";
    [JsonPropertyName("c")] public decimal Costo { get; init; }
    [JsonPropertyName("t")] public string Tiempo { get; init; } = "";
}

public record BannerDto
{
    [JsonPropertyName("img")] public string Img { get; init; } = "";
    [JsonPropertyName("alt")] public string Alt { get; init; } = "";
    [JsonPropertyName("url")] public string Url { get; init; } = "";

    /// <summary>Carrusel al que pertenece: 1 arriba, 2 abajo.</summary>
    [JsonPropertyName("g")]   public int Grupo { get; init; } = 1;
}

public record CatalogoDto(ConfigDto Config, string[] Grupos, ProductoDto[] Productos);

/// <summary>Lo que el panel manda al publicar.</summary>
public record CambioDto(string Id, decimal Precio, decimal? PrecioCombo, bool Stock);
