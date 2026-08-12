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

    /// <summary>Formato por línea: Distrito|Costo|Tiempo</summary>
    private static ZonaDto[] ParsearZonas(string texto) =>
        texto.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
             .Select(l => l.Split('|'))
             .Where(p => p.Length >= 1 && !string.IsNullOrWhiteSpace(p[0]))
             .Select(p => new ZonaDto
             {
                 Nombre = p[0].Trim(),
                 Costo = p.Length > 1 && decimal.TryParse(p[1].Trim(), out var c) ? c : 0,
                 Tiempo = p.Length > 2 ? p[2].Trim() : ""
             })
             .ToArray();

    /// <summary>Formato por línea: ruta|alt|enlace</summary>
    private static BannerDto[] ParsearBanners(string texto) =>
        texto.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
             .Select(l => l.Split('|'))
             .Where(p => !string.IsNullOrWhiteSpace(p[0]))
             .Select(p => new BannerDto
             {
                 Img = p[0].Trim(),
                 Alt = p.Length > 1 ? p[1].Trim() : "",
                 Url = p.Length > 2 ? p[2].Trim() : ""
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

    /// <summary>Reemplaza la lista de banners. Máximo 5.</summary>
    public async Task GuardarBannersAsync(IEnumerable<BannerDto> banners, CancellationToken ct = default)
    {
        /* Sin Take(5): truncaba en silencio y dejaba el tope de abajo como
           código muerto. Si llegan más de cinco, algo pasó — se avisa. */
        var lista = banners.Where(b => !string.IsNullOrWhiteSpace(b.Img)).ToList();
        if (lista.Count > 5)
            throw new InvalidOperationException("El carrusel admite un máximo de 5 banners.");

        await using var db = await factory.CreateDbContextAsync(ct);
        var t = await db.Tienda.FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("No hay configuración de tienda.");

        t.Banners = string.Join('\n', lista.Select(b =>
            $"{b.Img}|{b.Alt?.Replace('|', ' ')}|{b.Url?.Replace('|', ' ')}"));

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
