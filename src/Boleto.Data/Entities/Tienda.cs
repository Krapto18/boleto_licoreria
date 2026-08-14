using System.ComponentModel.DataAnnotations;

namespace Boleto.Data.Entities;

/// <summary>Configuración de la tienda. Una sola fila (Id = 1).</summary>
public class Tienda
{
    public int Id { get; set; } = 1;

    [MaxLength(80)] public string Nombre { get; set; } = "Boleto Licorería";
    [MaxLength(20)] public string Telefono { get; set; } = "51933851832";
    [MaxLength(60)] public string Instagram { get; set; } = "boletolicoreria";
    [MaxLength(60)] public string TikTok { get; set; } = "boletolicoreria";
    [MaxLength(5)]  public string Moneda { get; set; } = "S/";

    /// <summary>El diferenciador: tienda y WhatsApp 24 horas, los 7 días.</summary>
    public bool Abierto247 { get; set; } = true;

    /// <summary>Frases de la franja roja, una por línea.</summary>
    [MaxLength(1000)] public string Promos { get; set; } =
        "Tienda y WhatsApp abiertos 24 horas\n" +
        "Coca Cola 1.5 L gratis con cualquier whisky o ron\n" +
        "Combos con gaseosa 1.5 L + hielo\n" +
        "No cerramos. Ni feriados.";

    /* ── ZONAS DE REPARTO ────────────────────────────────────────
       Una por línea, con el formato:  Distrito|Costo|Tiempo
       Ejemplo:  Santiago de Surco|0|30-45 min
       Costo 0 = gratis. Vacío = la sección no aparece en la web.

       4000 y no 2000: son los 43 distritos de Lima Metropolitana y el
       dueño puede escribir un tiempo por cada uno. Con 2000 el límite
       quedaba a un par de distritos de distancia. */
    [MaxLength(4000)] public string Zonas { get; set; } = "";

    /* ── MÉTODOS DE PAGO ─────────────────────────────────────────
       Uno por línea. Vacío = la sección no aparece. */
    [MaxLength(400)] public string Pagos { get; set; } = "";

    /* Texto del tiempo de entrega. Va en la sección de delivery,
       no en el titular: es un compromiso operativo del cliente. */
    [MaxLength(80)] public string TiempoEntrega { get; set; } = "";

    /* ── BANNERS PROMOCIONALES ───────────────────────────────────
       Uno por línea:  ruta|texto alternativo|enlace opcional|carrusel
       Ejemplo:  assets/banners/promo1.webp|Combo fiestero|#catalogo|1

       El cuarto campo dice en cuál de los dos carruseles va: 1 es el de
       arriba (antes del catálogo) y 2 el de abajo. Sin cuarto campo se
       asume 1, que es donde estaban los banners de una sola pista.

       4000 y no 2000: son diez banners y cada línea admite 120 de texto
       alternativo más 200 de enlace. Con 2000 el dueño podía llenar el
       segundo carrusel y que el guardado reventara contra la columna. */
    [MaxLength(4000)] public string Banners { get; set; } = "";

    /* ── LOGO DE LA PORTADA ──────────────────────────────────────
       El que ocupa el titular del hero. Formato: ruta|ancho|alto
       Ejemplo:  /media/marca/logo-portada.webp|1000|700

       Vacío = se usa el logo oficial del kit, que viene con la app.

       El ancho y el alto se guardan porque esa imagen es el elemento
       más grande de la primera pantalla: sin declarar su tamaño en el
       HTML, el hero salta cuando termina de cargar. Se miden al subir,
       leyendo la cabecera del archivo — no se le piden al dueño. */
    [MaxLength(220)] public string LogoHero { get; set; } = "";

    /* Verificación de edad (Ley N° 28681). */
    public bool Verificar18 { get; set; } = true;

    // Pendientes de confirmar. Vacío = no se publica y el JSON-LD no se emite.
    [MaxLength(200)] public string Direccion { get; set; } = "";
    [MaxLength(80)]  public string Distrito  { get; set; } = "";
    [MaxLength(20)]  public string Latitud   { get; set; } = "";
    [MaxLength(20)]  public string Longitud  { get; set; } = "";

    // Medición. Se cargan solo si tienen valor.
    [MaxLength(30)] public string Ga4       { get; set; } = "";
    [MaxLength(30)] public string MetaPixel { get; set; } = "";
}
