using System.Text.Json;
using Boleto.Data;
using Boleto.Data.Entities;
using Boleto.Web.Models;
using Boleto.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Boleto.Web.Pages.Panel;

[Authorize]
/* El tope va en la clase: RequestSizeLimit no se aplica a los métodos
   de una página Razor, solo al modelo completo o globalmente. */
[RequestSizeLimit(2 * 1024 * 1024)]
public class IndexModel(CatalogoService svc, IAlmacen almacen, ILogger<IndexModel> log) : PageModel
{
    public string CatalogoJs { get; private set; } = "";

    public bool SubidaDisponible => almacen.Disponible;

    /// <summary>Se muestra en el panel para saber dónde se están guardando.</summary>
    public string ModoAlmacen => almacen.Modo;

    private static readonly JsonSerializerOptions Json = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Default
    };

    /// <summary>Incluye los dados de baja: el panel los muestra en gris.</summary>
    public string TodosJs { get; private set; } = "";

    /// <summary>
    /// Catálogo maestro de distritos, para poder reponer uno que se quitó
    /// sin tener que recordar cómo se escribe.
    /// </summary>
    public string DistritosJs { get; private set; } = "";

    public async Task OnGetAsync(CancellationToken ct)
    {
        DistritosJs = "const DISTRITOS_LIMA="
            + JsonSerializer.Serialize(SeedData.DistritosLima, Json) + ";";

        var todos = await svc.TodosAsync(ct);
        TodosJs = "const TODOS=" + JsonSerializer.Serialize(todos.Select(p => new
        {
            id = p.Id,
            n = p.Nombre,
            v = p.Presentacion,
            c = p.Categoria,
            g = p.Grupo,
            p = p.Precio,
            combo = p.PrecioCombo,
            aco = p.ComboAcompanante,
            acoP = p.ComboAcompanantePrecio,
            acoOn = p.ComboAcompananteActivo,
            hie = p.ComboHielo,
            hieP = p.ComboHieloPrecio,
            hieOn = p.ComboHieloActivo,
            promo = p.Promo,
            stock = p.Stock,
            activo = p.Activo,
            orden = p.Orden,
            img = p.Imagen,
            col = p.Color
        }), Json) + ";";

        var c = await svc.ObtenerAsync(ct);
        CatalogoJs =
            $"const CONFIG={JsonSerializer.Serialize(c.Config, Json)};" +
            $"const GRUPOS={JsonSerializer.Serialize(c.Grupos, Json)};" +
            $"const PRODUCTOS={JsonSerializer.Serialize(c.Productos, Json)};";
    }

    /// <summary>Publica de verdad: guarda, audita e invalida la caché.</summary>
    public async Task<IActionResult> OnPostPublicarAsync(
        [FromBody] CambioDto[] cambios, CancellationToken ct)
    {
        if (cambios is null || cambios.Length == 0)
            return BadRequest(new { error = "No llegó ningún cambio." });

        if (cambios.Length > 500)
            return BadRequest(new { error = "Demasiados cambios en una sola operación." });

        try
        {
            var usuario = User.Identity?.Name ?? "desconocido";
            var n = await svc.GuardarAsync(cambios, usuario, ct);
            return new JsonResult(new { ok = true, publicados = n });
        }
        catch (InvalidOperationException ex)
        {
            // Error de validación: es culpa del dato, no del sistema.
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al publicar cambios");
            return StatusCode(500, new { error = "No se pudo guardar. Intenta de nuevo." });
        }
    }

    /// <summary>Sube o reemplaza la foto de un producto.</summary>
    public async Task<IActionResult> OnPostImagenAsync(
        string id, IFormFile archivo, CancellationToken ct)
    {
        try
        {
            /* Se comprueba antes de subir: si el identificador no existe,
               el blob quedaría huérfano en la cuenta de storage. */
            if (!await svc.ExisteAsync(id, ct))
                return BadRequest(new { error = "Ese producto no existe." });

            var url = await almacen.GuardarAsync(archivo, "productos", id, ct);
            var anterior = await svc.GuardarImagenAsync(id, url, ct);

            /* El blob viejo se borra después de guardar el nuevo: si algo
               falla en el medio, el producto nunca queda sin imagen. */
            if (!string.IsNullOrWhiteSpace(anterior) && anterior != url)
                await almacen.EliminarAsync(anterior, ct);

            return new JsonResult(new { ok = true, url });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al subir imagen de {Id}", id);
            return StatusCode(500, new { error = "No se pudo subir la imagen." });
        }
    }

    /// <summary>Alta de producto nuevo.</summary>
    public async Task<IActionResult> OnPostNuevoAsync(
        [FromBody] NuevoProductoDto d, CancellationToken ct)
    {
        try
        {
            await svc.CrearProductoAsync(new Producto
            {
                Id = d.Id,
                Nombre = d.Nombre,
                Presentacion = d.Presentacion,
                Categoria = d.Categoria,
                Grupo = d.Grupo,
                Precio = d.Precio,
                PrecioCombo = d.PrecioCombo,
                ComboAcompanante = d.ComboAcompanante ?? "",
                ComboHielo = d.ComboHielo ?? "",
                /* Si el alta trae nombre, va incluido: nadie escribe el
                   acompañante de un combo para dejarlo apagado. El precio
                   se pone después, en la pestaña de precios. */
                ComboAcompananteActivo = !string.IsNullOrWhiteSpace(d.ComboAcompanante),
                ComboHieloActivo = !string.IsNullOrWhiteSpace(d.ComboHielo),
                Promo = d.Promo,
                Stock = true,
                Color = string.IsNullOrWhiteSpace(d.Color) ? "#8A8A8A" : d.Color
            }, ct);
            return new JsonResult(new { ok = true, id = d.Id.Trim().ToLowerInvariant() });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al crear producto");
            return StatusCode(500, new { error = "No se pudo crear el producto." });
        }
    }

    /// <summary>Sube un banner. <paramref name="grupo"/> es el carrusel: 1 arriba, 2 abajo.</summary>
    public async Task<IActionResult> OnPostBannerAsync(
        int grupo, int indice, IFormFile archivo, CancellationToken ct)
    {
        try
        {
            if (grupo is < 1 or > CatalogoService.Carruseles)
                return BadRequest(new { error = "Ese carrusel no existe." });
            if (indice < 0 || indice >= CatalogoService.BannersPorCarrusel)
                return BadRequest(new
                {
                    error = $"Cada carrusel admite un máximo de {CatalogoService.BannersPorCarrusel} banners."
                });

            // El nombre lleva el carrusel: si no, el banner 1 de abajo
            // pisaría el archivo del banner 1 de arriba.
            var url = await almacen.GuardarAsync(archivo, "banners", $"banner-{grupo}-{indice + 1}", ct);
            return new JsonResult(new { ok = true, url });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al subir banner");
            return StatusCode(500, new { error = "No se pudo subir el banner." });
        }
    }

    /// <summary>Guarda las zonas de reparto con su costo de delivery.</summary>
    public async Task<IActionResult> OnPostZonasAsync(
        [FromBody] ZonaDto[] zonas, CancellationToken ct)
    {
        try
        {
            await svc.GuardarZonasAsync(zonas ?? [], ct);
            return new JsonResult(new { ok = true });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al guardar zonas");
            return StatusCode(500, new { error = "No se pudieron guardar las zonas." });
        }
    }

    /// <summary>Guarda la lista completa de banners.</summary>
    public async Task<IActionResult> OnPostBannersAsync(
        [FromBody] BannerDto[] banners, CancellationToken ct)
    {
        try
        {
            await svc.GuardarBannersAsync(banners ?? [], ct);
            return new JsonResult(new { ok = true });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al guardar banners");
            return StatusCode(500, new { error = "No se pudieron guardar los banners." });
        }
    }

    /// <summary>Edita un producto. El identificador no se toca.</summary>
    public async Task<IActionResult> OnPostEditarAsync(
        [FromBody] EditarProductoDto d, CancellationToken ct)
    {
        try
        {
            await svc.ActualizarProductoAsync(new Producto
            {
                Id = d.Id,
                Nombre = d.Nombre,
                Presentacion = d.Presentacion,
                Categoria = d.Categoria,
                Grupo = d.Grupo,
                Precio = d.Precio,
                PrecioCombo = d.PrecioCombo,
                ComboAcompanante = d.ComboAcompanante ?? "",
                ComboHielo = d.ComboHielo ?? "",
                Promo = d.Promo,
                Orden = d.Orden
            }, User.Identity?.Name ?? "desconocido", ct);
            return new JsonResult(new { ok = true });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al editar {Id}", d.Id);
            return StatusCode(500, new { error = "No se pudo guardar el producto." });
        }
    }

    /// <summary>Da de baja o reactiva. Nunca borra.</summary>
    public async Task<IActionResult> OnPostActivoAsync(
        string id, bool activo, CancellationToken ct)
    {
        try
        {
            await svc.CambiarActivoAsync(id, activo, User.Identity?.Name ?? "desconocido", ct);
            return new JsonResult(new { ok = true });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al cambiar estado de {Id}", id);
            return StatusCode(500, new { error = "No se pudo cambiar el estado." });
        }
    }

    public record EditarProductoDto(
        string Id, string Nombre, string Presentacion, string Categoria, string Grupo,
        decimal Precio, decimal? PrecioCombo, string? ComboAcompanante, string? ComboHielo,
        bool Promo, int Orden);

    public record NuevoProductoDto(
        string Id, string Nombre, string Presentacion, string Categoria, string Grupo,
        decimal Precio, decimal? PrecioCombo, string? ComboAcompanante, string? ComboHielo,
        bool Promo, string? Color);
}
