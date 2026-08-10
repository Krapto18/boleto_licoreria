using System.Text.Json;
using Boleto.Data.Entities;
using Boleto.Web.Models;
using Boleto.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Boleto.Web.Pages.Panel;

[Authorize]
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

    public async Task OnGetAsync(CancellationToken ct)
    {
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
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> OnPostImagenAsync(
        string id, IFormFile archivo, CancellationToken ct)
    {
        try
        {
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
                Id = d.Id, Nombre = d.Nombre, Presentacion = d.Presentacion,
                Categoria = d.Categoria, Grupo = d.Grupo,
                Precio = d.Precio, PrecioCombo = d.PrecioCombo,
                ComboAcompanante = d.ComboAcompanante ?? "", ComboHielo = d.ComboHielo ?? "",
                Promo = d.Promo, Stock = true,
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

    /// <summary>Sube un banner del carrusel.</summary>
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> OnPostBannerAsync(
        int indice, IFormFile archivo, CancellationToken ct)
    {
        try
        {
            if (indice is < 0 or > 4)
                return BadRequest(new { error = "El carrusel admite un máximo de 5 banners." });

            var url = await almacen.GuardarAsync(archivo, "banners", $"banner-{indice + 1}", ct);
            return new JsonResult(new { ok = true, url });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex)
        {
            log.LogError(ex, "Error al subir banner");
            return StatusCode(500, new { error = "No se pudo subir el banner." });
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

    public record NuevoProductoDto(
        string Id, string Nombre, string Presentacion, string Categoria, string Grupo,
        decimal Precio, decimal? PrecioCombo, string? ComboAcompanante, string? ComboHielo,
        bool Promo, string? Color);
}
