using System.Text.Json;
using Boleto.Web.Models;
using Boleto.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Boleto.Web.Pages;

public class IndexModel(CatalogoService svc, IConfiguration cfg) : PageModel
{
    public CatalogoDto Catalogo { get; private set; } = default!;

    /// <summary>
    /// Dirección canónica absoluta de la página. Sin ella la misma portada
    /// vive en tres direcciones —la raíz, www y azurewebsites.net— y los
    /// buscadores reparten el posicionamiento entre las tres.
    /// </summary>
    public string UrlCanonica { get; private set; } = "";

    /// <summary>
    /// og:image absoluta. WhatsApp y Facebook no resuelven rutas relativas:
    /// con una ruta relativa el enlace compartido viaja sin vista previa, que
    /// para este negocio es el canal de venta.
    /// </summary>
    public string UrlImagen { get; private set; } = "";

    /// <summary>
    /// El catálogo se inyecta como un bloque script con exactamente las mismas
    /// variables globales que declaraba data.js (CONFIG, GRUPOS, PRODUCTOS).
    /// Por eso app.js no necesita ni un cambio.
    /// </summary>
    public string CatalogoJs { get; private set; } = "";

    /// <summary>JSON-LD. Solo se emite si hay dirección confirmada.</summary>
    public string? SchemaJson { get; private set; }

    private static readonly JsonSerializerOptions Json = new()
    {
        // Escapa < > & para que no se pueda cerrar la etiqueta script desde los datos.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Default
    };

    public async Task OnGetAsync(CancellationToken ct)
    {
        Catalogo = await svc.ObtenerAsync(ct);

        /* Base pública del sitio. Si no hay dominio configurado se usa el de
           la request, para que en local y en azurewebsites.net siga saliendo
           una URL válida sin tener que configurar nada. */
        var baseUrl = (cfg["Sitio:Url"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
        UrlCanonica = baseUrl + Request.Path;   // sin query: la canónica es una sola
        UrlImagen = baseUrl + "/assets/og.png";

        CatalogoJs =
            $"const CONFIG={JsonSerializer.Serialize(Catalogo.Config, Json)};" +
            $"const GRUPOS={JsonSerializer.Serialize(Catalogo.Grupos, Json)};" +
            $"const PRODUCTOS={JsonSerializer.Serialize(Catalogo.Productos, Json)};";

        var c = Catalogo.Config;
        if (string.IsNullOrWhiteSpace(c.Direccion)) return;

        // LiquorStore abierto 00:00–23:59 los siete días: es lo que hace que
        // Google muestre "Abierto 24 horas" en el mapa.
        object schema = new
        {
            context = "https://schema.org",
            type = "LiquorStore",
            name = c.Tienda,
            telephone = "+" + c.Telefono,
            address = new
            {
                type = "PostalAddress",
                streetAddress = c.Direccion,
                addressLocality = c.Distrito,
                addressRegion = "Lima",
                addressCountry = "PE"
            },
            openingHoursSpecification = new[]
            {
                new
                {
                    type = "OpeningHoursSpecification",
                    dayOfWeek = new[] { "Monday","Tuesday","Wednesday","Thursday","Friday","Saturday","Sunday" },
                    opens = "00:00",
                    closes = "23:59"
                }
            },
            sameAs = new[]
            {
                $"https://instagram.com/{c.Instagram}",
                $"https://tiktok.com/@{c.TikTok}"
            }
        };

        // System.Text.Json no permite propiedades que empiecen con @, así que
        // se serializa con nombres normales y se corrigen acá.
        SchemaJson = JsonSerializer.Serialize(schema, Json)
            .Replace("\"context\":", "\"@context\":")
            .Replace("\"type\":", "\"@type\":");
    }
}
