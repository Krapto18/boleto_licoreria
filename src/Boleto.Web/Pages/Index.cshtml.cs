using System.Text.Json;
using Boleto.Web.Models;
using Boleto.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Boleto.Web.Pages;

public class IndexModel(CatalogoService svc) : PageModel
{
    public CatalogoDto Catalogo { get; private set; } = default!;

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
