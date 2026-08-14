using System.Globalization;
using Boleto.Data;
using Boleto.Data.Entities;
using Boleto.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Boleto.Web.Services;

/// <summary>
/// El catálogo vive en memoria y solo se relee cuando el panel publica.
///
/// No es optimización prematura: Azure SQL Basic son 5 DTU. Si cada visita
/// golpeara la base, un sábado en la noche la satura. Además, si la base se
/// cae, la web pública sigue sirviendo el último catálogo bueno — que para
/// una tienda 24/7 es la diferencia entre vender y no vender.
/// </summary>
public class CatalogoService(
    IDbContextFactory<BoletoDbContext> factory,
    IMemoryCache cache,
    ILogger<CatalogoService> log)
{
    private const string Key = "catalogo";

    /// <summary>Orden de los filtros. Ley de Hick: 7 grupos, no 10 categorías.</summary>
    public static readonly string[] Grupos =
    [
        "Todo", "Whiskys", "Rones y piscos", "Vodka, gin y otros",
        "Vinos y espumantes", "Cervezas", "Bebidas y hielo"
    ];

    public async Task<CatalogoDto> ObtenerAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(Key, out CatalogoDto? hit) && hit is not null)
            return hit;

        await using var db = await factory.CreateDbContextAsync(ct);

        var tienda = await db.Tienda.AsNoTracking().FirstOrDefaultAsync(ct) ?? new Tienda();

        var productos = await db.Productos.AsNoTracking()
            .Where(p => p.Activo)          // los dados de baja no salen al catálogo
            .OrderBy(p => p.Orden)
            .Select(p => new ProductoDto
            {
                Id = p.Id,
                N = p.Nombre,
                V = p.Presentacion,
                C = p.Categoria,
                G = p.Grupo,
                P = p.Precio,
                Combo = p.PrecioCombo,
                Acompanante = p.ComboAcompanante,
                Hielo = p.ComboHielo,
                Promo = p.Promo,
                Stock = p.Stock,
                Col = p.Color,
                Img = p.Imagen
            })
            .ToArrayAsync(ct);

        var dto = new CatalogoDto(
            new ConfigDto
            {
                Tienda = tienda.Nombre,
                Telefono = tienda.Telefono,
                Instagram = tienda.Instagram,
                TikTok = tienda.TikTok,
                Moneda = tienda.Moneda,
                Abierto247 = tienda.Abierto247,
                Promos = tienda.Promos.Split('\n',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                Direccion = tienda.Direccion,
                Distrito = tienda.Distrito,
                Latitud = tienda.Latitud,
                Longitud = tienda.Longitud,
                Zonas = ParsearZonas(tienda.Zonas),
                Pagos = tienda.Pagos.Split('\n',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                TiempoEntrega = tienda.TiempoEntrega,
                Banners = ParsearBanners(tienda.Banners),
                Verificar18 = tienda.Verificar18,
                Ga4 = tienda.Ga4,
                MetaPixel = tienda.MetaPixel
            },
            Grupos,
            productos);

        // Sin expiración por tiempo: solo se invalida al publicar.
        cache.Set(Key, dto, new MemoryCacheEntryOptions
        {
            Priority = CacheItemPriority.NeverRemove
        });

        log.LogInformation("Catálogo recargado desde la base: {N} productos", productos.Length);
        return dto;
    }

    /// <summary>
    /// Formato por línea: Distrito|Costo|Tiempo
    ///
    /// El costo se lee con cultura invariante, igual que se escribe. Sin
    /// fijarla, el separador decimal depende de la cultura del hilo: un
    /// "10,5" guardado en es-PE se releería como 105 en un servidor
    /// invariante, y el cliente vería ese número en su total.
    /// </summary>
    private static ZonaDto[] ParsearZonas(string texto) =>
        texto.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
             .Select(l => l.Split('|'))
             .Where(p => p.Length >= 1 && !string.IsNullOrWhiteSpace(p[0]))
             .Select(p => new ZonaDto
             {
                 Nombre = p[0].Trim(),
                 Costo = p.Length > 1 && decimal.TryParse(
                     p[1].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var c) ? c : 0,
                 Tiempo = p.Length > 2 ? p[2].Trim() : ""
             })
             .ToArray();

    /// <summary>
    /// Formato por línea: ruta|alt|enlace|carrusel. El cuarto campo es el
    /// carrusel (1 arriba, 2 abajo); las líneas guardadas antes de que
    /// hubiera dos pistas no lo traen y caen en el 1, que es donde estaban.
    /// </summary>
    private static BannerDto[] ParsearBanners(string texto) =>
        texto.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
             .Select(l => l.Split('|'))
             .Where(p => !string.IsNullOrWhiteSpace(p[0]))
             .Select(p => new BannerDto
             {
                 Img = p[0].Trim(),
                 Alt = p.Length > 1 ? p[1].Trim() : "",
                 Url = p.Length > 2 ? p[2].Trim() : "",
                 Grupo = p.Length > 3 && p[3].Trim() == "2" ? 2 : 1
             })
             .ToArray();

    public void Invalidar() => cache.Remove(Key);

    /// <summary>Todos los productos, incluidos los inactivos. Solo para el panel.</summary>
    public async Task<Producto[]> TodosAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Productos.AsNoTracking().OrderBy(p => p.Orden).ToArrayAsync(ct);
    }

    /// <summary>
    /// Edita un producto. El identificador no se toca: está atado a la
    /// imagen y a la auditoría, y cambiarlo dejaría huérfanos los dos.
    /// </summary>
    public async Task ActualizarProductoAsync(
        Producto d, string usuario, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var p = await db.Productos.FirstOrDefaultAsync(x => x.Id == d.Id, ct)
                ?? throw new InvalidOperationException("Ese producto no existe.");

        if (string.IsNullOrWhiteSpace(d.Nombre))
            throw new InvalidOperationException("Falta el nombre del producto.");
        if (d.Precio <= 0)
            throw new InvalidOperationException("El precio debe ser mayor a 0.");
        if (d.PrecioCombo is { } c && c <= d.Precio)
            throw new InvalidOperationException("El combo no puede costar menos que la botella.");
        if (!Grupos.Contains(d.Grupo))
            throw new InvalidOperationException($"El grupo \"{d.Grupo}\" no existe.");

        var auditoria = new List<CambioPrecio>();
        void Auditar(string campo, string antes, string ahora)
        {
            if (antes == ahora) return;
            auditoria.Add(new CambioPrecio
            {
                ProductoId = p.Id,
                ProductoNombre = p.Nombre,
                Campo = campo,
                ValorAnterior = Corta(antes),
                ValorNuevo = Corta(ahora),
                Usuario = usuario
            });
        }

        Auditar("Nombre", p.Nombre, d.Nombre);
        Auditar("Presentacion", p.Presentacion, d.Presentacion);
        Auditar("Categoria", p.Categoria, d.Categoria);
        Auditar("Grupo", p.Grupo, d.Grupo);
        Auditar("Precio", p.Precio.ToString("0.00"), d.Precio.ToString("0.00"));
        Auditar("PrecioCombo", p.PrecioCombo?.ToString("0.00") ?? "—",
                               d.PrecioCombo?.ToString("0.00") ?? "—");
        Auditar("Acompanante", p.ComboAcompanante, d.ComboAcompanante);
        Auditar("Hielo", p.ComboHielo, d.ComboHielo);
        Auditar("Promo", p.Promo ? "sí" : "no", d.Promo ? "sí" : "no");
        Auditar("Orden", p.Orden.ToString(), d.Orden.ToString());

        p.Nombre = d.Nombre;
        p.Presentacion = d.Presentacion;
        p.Categoria = d.Categoria;
        p.Grupo = d.Grupo;
        p.Precio = d.Precio;
        p.PrecioCombo = d.PrecioCombo;
        p.ComboAcompanante = d.ComboAcompanante;
        p.ComboHielo = d.ComboHielo;
        p.Promo = d.Promo;
        p.Orden = d.Orden;
        p.ActualizadoUtc = DateTime.UtcNow;

        if (auditoria.Count > 0) db.CambiosPrecio.AddRange(auditoria);
        await db.SaveChangesAsync(ct);
        Invalidar();
        log.LogInformation("{Usuario} editó {Id}: {N} campos", usuario, p.Id, auditoria.Count);
    }

    /// <summary>Da de baja o reactiva. Nunca se borra la fila.</summary>
    public async Task CambiarActivoAsync(
        string id, bool activo, string usuario, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var p = await db.Productos.FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new InvalidOperationException("Ese producto no existe.");

        if (p.Activo == activo) return;

        db.CambiosPrecio.Add(new CambioPrecio
        {
            ProductoId = p.Id,
            ProductoNombre = p.Nombre,
            Campo = "Activo",
            ValorAnterior = p.Activo ? "activo" : "de baja",
            ValorNuevo = activo ? "activo" : "de baja",
            Usuario = usuario
        });

        p.Activo = activo;
        p.ActualizadoUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        Invalidar();
        log.LogInformation("{Usuario} {Accion} {Id}", usuario, activo ? "reactivó" : "dio de baja", id);
    }

    private static string Corta(string s) => s.Length > 20 ? s[..20] : s;

    /// <summary>Alta de un producto nuevo desde el panel.</summary>
    public async Task CrearProductoAsync(Producto p, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        p.Id = p.Id.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(p.Id) || !p.Id.All(c => char.IsLetterOrDigit(c) || c == '-'))
            throw new InvalidOperationException(
                "El identificador solo admite letras, números y guiones. Ejemplo: jw-red");

        if (await db.Productos.AnyAsync(x => x.Id == p.Id, ct))
            throw new InvalidOperationException($"Ya existe un producto con el identificador \"{p.Id}\".");

        if (string.IsNullOrWhiteSpace(p.Nombre))
            throw new InvalidOperationException("Falta el nombre del producto.");

        if (p.Precio <= 0)
            throw new InvalidOperationException("El precio debe ser mayor a 0.");

        if (p.PrecioCombo is { } c && c <= p.Precio)
            throw new InvalidOperationException("El combo no puede costar menos que la botella.");

        if (!Grupos.Contains(p.Grupo))
            throw new InvalidOperationException($"El grupo \"{p.Grupo}\" no existe.");

        // Va al final de su grupo
        var ultimo = await db.Productos.MaxAsync(x => (int?)x.Orden, ct) ?? 0;
        p.Orden = ultimo + 10;
        p.ActualizadoUtc = DateTime.UtcNow;

        db.Productos.Add(p);
        await db.SaveChangesAsync(ct);
        Invalidar();
        log.LogInformation("Producto creado: {Id}", p.Id);
    }

    /// <summary>
    /// ¿Existe el producto? Se comprueba antes de subir una imagen, para no
    /// dejar un blob huérfano cuando el identificador no corresponde a nada.
    /// </summary>
    public async Task<bool> ExisteAsync(string id, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Productos.AnyAsync(p => p.Id == id, ct);
    }

    /// <summary>Guarda la URL de la imagen de un producto.</summary>
    public async Task<string?> GuardarImagenAsync(string id, string url, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var p = await db.Productos.FirstOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new InvalidOperationException("Ese producto no existe.");

        var anterior = p.Imagen;
        p.Imagen = url;
        p.ActualizadoUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        Invalidar();
        return anterior;   // para borrar el blob viejo
    }

    /// <summary>
    /// Reemplaza las zonas de reparto. Se revalida acá porque el costo del
    /// delivery entra en el total que ve el cliente: un valor basura se
    /// convierte en un precio equivocado en el mensaje de WhatsApp.
    /// </summary>
    public async Task GuardarZonasAsync(IEnumerable<ZonaDto> zonas, CancellationToken ct = default)
    {
        var lista = zonas.Where(z => !string.IsNullOrWhiteSpace(z.Nombre)).ToList();

        foreach (var z in lista)
        {
            if (z.Costo < 0)
                throw new InvalidOperationException(
                    $"El costo de {z.Nombre} no puede ser negativo. Usa 0 para reparto gratis.");

            if (z.Costo > 500)
                throw new InvalidOperationException(
                    $"El costo de {z.Nombre} se ve equivocado: S/ {z.Costo:0.00}.");
        }

        var duplicado = lista.GroupBy(z => z.Nombre.Trim(), StringComparer.OrdinalIgnoreCase)
                             .FirstOrDefault(g => g.Count() > 1);
        if (duplicado is not null)
            throw new InvalidOperationException($"El distrito \"{duplicado.Key}\" está repetido.");

        /* El separador es '|' y el salto de línea define la fila: si el
           dueño los escribe dentro de un tiempo, romperían el formato al
           releerlo. Se neutralizan en vez de rechazar el guardado. */
        static string Limpio(string? s) =>
            (s ?? "").Replace('|', ' ').Replace('\n', ' ').Replace('\r', ' ').Trim();

        // Invariante al escribir, invariante al leer. Ver ParsearZonas.
        var texto = string.Join('\n', lista.Select(z =>
            string.Create(CultureInfo.InvariantCulture,
                $"{Limpio(z.Nombre)}|{z.Costo:0.##}|{Limpio(z.Tiempo)}")));

        if (texto.Length > 4000)
            throw new InvalidOperationException(
                "La lista de zonas es demasiado larga. Acorta los tiempos de entrega " +
                "o quita distritos a los que no repartas.");

        await using var db = await factory.CreateDbContextAsync(ct);
        var t = await db.Tienda.FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("No hay configuración de tienda.");

        t.Zonas = texto;
        await db.SaveChangesAsync(ct);
        Invalidar();
        log.LogInformation("Zonas de reparto actualizadas: {N} distritos", lista.Count);
    }

    /// <summary>Cuántos carruseles tiene la página y cuántos banners cabe en cada uno.</summary>
    public const int Carruseles = 2;
    public const int BannersPorCarrusel = 5;

    /// <summary>
    /// Reemplaza la lista de banners. Dos carruseles de cinco: el de
    /// arriba (1) y el de abajo (2).
    /// </summary>
    public async Task GuardarBannersAsync(IEnumerable<BannerDto> banners, CancellationToken ct = default)
    {
        /* Sin Take(5): truncaba en silencio y dejaba el tope de abajo como
           código muerto. Si llegan más de la cuenta, algo pasó — se avisa. */
        var lista = banners
            .Where(b => !string.IsNullOrWhiteSpace(b.Img))
            .Select(b => b with { Grupo = b.Grupo == 2 ? 2 : 1 })
            .ToList();

        foreach (var g in lista.GroupBy(b => b.Grupo))
            if (g.Count() > BannersPorCarrusel)
                throw new InvalidOperationException(
                    $"El carrusel {g.Key} admite un máximo de {BannersPorCarrusel} banners.");

        // Los saltos de línea y las barras son los separadores del formato:
        // si entran en un texto, parten la línea en campos que no existen.
        static string Limpio(string? s) =>
            (s ?? "").Replace('|', ' ').Replace('\n', ' ').Replace('\r', ' ').Trim();

        var texto = string.Join('\n', lista.Select(b =>
            $"{Limpio(b.Img)}|{Limpio(b.Alt)}|{Limpio(b.Url)}|{b.Grupo}"));

        if (texto.Length > 4000)
            throw new InvalidOperationException(
                "Los textos de los banners no entran en el espacio disponible. " +
                "Acorta las descripciones o los enlaces.");

        await using var db = await factory.CreateDbContextAsync(ct);
        var t = await db.Tienda.FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("No hay configuración de tienda.");

        t.Banners = texto;

        await db.SaveChangesAsync(ct);
        Invalidar();
        log.LogInformation("Banners actualizados: {N}", lista.Count);
    }

    /// <summary>
    /// Guarda los cambios del panel y deja rastro de quién tocó qué.
    /// Revalida en el servidor: no se confía en lo que manda el navegador.
    /// </summary>
    public async Task<int> GuardarAsync(
        IEnumerable<CambioDto> cambios, string usuario, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);

        var lista = cambios.ToList();
        var ids = lista.Select(c => c.Id).ToList();
        var productos = await db.Productos.Where(p => ids.Contains(p.Id)).ToListAsync(ct);

        var auditoria = new List<CambioPrecio>();
        var tocados = 0;

        foreach (var c in lista)
        {
            var p = productos.FirstOrDefault(x => x.Id == c.Id);
            if (p is null) continue;

            if (c.Precio <= 0)
                throw new InvalidOperationException($"El precio de {p.Nombre} debe ser mayor a 0.");

            // Misma regla que valida el panel, revalidada del lado del servidor.
            if (c.PrecioCombo is { } combo && combo <= c.Precio)
                throw new InvalidOperationException(
                    $"El combo de {p.Nombre} no puede costar menos que la botella.");

            void Auditar(string campo, string antes, string ahora) =>
                auditoria.Add(new CambioPrecio
                {
                    ProductoId = p.Id,
                    ProductoNombre = p.Nombre,
                    Campo = campo,
                    ValorAnterior = antes,
                    ValorNuevo = ahora,
                    Usuario = usuario
                });

            var cambio = false;

            if (p.Precio != c.Precio)
            {
                Auditar("Precio", p.Precio.ToString("0.00"), c.Precio.ToString("0.00"));
                p.Precio = c.Precio;
                cambio = true;
            }

            if (p.PrecioCombo is not null && p.PrecioCombo != c.PrecioCombo)
            {
                Auditar("PrecioCombo",
                    p.PrecioCombo?.ToString("0.00") ?? "",
                    c.PrecioCombo?.ToString("0.00") ?? "");
                p.PrecioCombo = c.PrecioCombo;
                cambio = true;
            }

            if (p.Stock != c.Stock)
            {
                Auditar("Stock", p.Stock ? "hay" : "agotado", c.Stock ? "hay" : "agotado");
                p.Stock = c.Stock;
                cambio = true;
            }

            if (cambio)
            {
                p.ActualizadoUtc = DateTime.UtcNow;
                tocados++;
            }
        }

        if (tocados > 0)
        {
            db.CambiosPrecio.AddRange(auditoria);
            await db.SaveChangesAsync(ct);
            Invalidar();
            log.LogInformation("{Usuario} publicó {N} cambios", usuario, tocados);
        }

        return tocados;
    }
}
