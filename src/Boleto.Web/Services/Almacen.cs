using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Boleto.Web.Services;

/// <summary>
/// Guarda las imágenes que sube el panel.
///
/// Hay dos implementaciones y se elige sola: si hay cadena de conexión
/// de Blob se usa Azure; si no, disco local. El panel no se entera de
/// cuál está activa, así que en desarrollo se prueba el flujo completo
/// sin emuladores ni contenedores.
/// </summary>
public interface IAlmacen
{
    bool Disponible { get; }
    string Modo { get; }
    Task<string> GuardarAsync(IFormFile archivo, string carpeta, string nombreBase, CancellationToken ct = default);
    Task EliminarAsync(string url, CancellationToken ct = default);
}

/// <summary>
/// Validaciones comunes. La subida de archivos es la superficie de
/// ataque más frecuente, así que estas comprobaciones no dependen de
/// dónde se guarde después.
/// </summary>
public abstract class AlmacenBase
{
    /* SVG queda fuera a propósito: puede llevar JavaScript dentro y se
       serviría desde una URL de confianza. */
    protected static readonly string[] Permitidas = [".jpg", ".jpeg", ".png", ".webp"];
    protected const long MaxBytes = 2 * 1024 * 1024;   // 2 MB

    protected static async Task<string> ValidarAsync(
        IFormFile archivo, string nombreBase, CancellationToken ct)
    {
        if (archivo is null || archivo.Length == 0)
            throw new InvalidOperationException("No llegó ningún archivo.");

        if (archivo.Length > MaxBytes)
            throw new InvalidOperationException(
                $"La imagen pesa {archivo.Length / 1024} KB y el máximo es {MaxBytes / 1024} KB. " +
                "Comprímela antes de subirla.");

        var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!Permitidas.Contains(ext))
            throw new InvalidOperationException(
                $"Formato no permitido. Usa {string.Join(", ", Permitidas)}.");

        if (Sanear(nombreBase).Length == 0)
            throw new InvalidOperationException("Nombre de archivo inválido.");

        await using var flujo = archivo.OpenReadStream();
        if (!await EsImagenRealAsync(flujo, ct))
            throw new InvalidOperationException(
                "El archivo no es una imagen válida. Puede tener la extensión cambiada.");

        return ext;
    }

    /// <summary>Solo letras, números y guiones. Sin rutas ni acentos.</summary>
    protected static string Sanear(string s) =>
        new(s.ToLowerInvariant()
             .Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '-')
             .ToArray());

    protected static string TipoMime(string ext) => ext switch
    {
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "image/jpeg"
    };

    /// <summary>
    /// Comprueba los bytes iniciales. La extensión se puede renombrar;
    /// la firma del archivo no.
    /// </summary>
    private static async Task<bool> EsImagenRealAsync(Stream s, CancellationToken ct)
    {
        var b = new byte[12];
        if (await s.ReadAsync(b.AsMemory(0, 12), ct) < 12) return false;

        if (b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return true;                    // JPEG
        if (b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return true;    // PNG
        if (b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 &&               // WebP
            b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50) return true;

        return false;
    }
}

/// <summary>
/// Desarrollo: guarda en una carpeta local servida bajo /media.
/// Las URLs quedan con la misma forma que en producción, así que el
/// resto del código no cambia al desplegar.
/// </summary>
public class AlmacenLocal : AlmacenBase, IAlmacen
{
    private readonly string _raiz;
    private readonly ILogger<AlmacenLocal> _log;

    public bool Disponible => true;
    public string Modo => "disco local";
    public string RutaFisica => _raiz;

    public AlmacenLocal(IWebHostEnvironment env, ILogger<AlmacenLocal> log)
    {
        _log = log;
        _raiz = Path.Combine(env.ContentRootPath, "media");
        Directory.CreateDirectory(_raiz);
        _log.LogInformation("Almacenamiento local en {Ruta}", _raiz);
    }

    public async Task<string> GuardarAsync(
        IFormFile archivo, string carpeta, string nombreBase, CancellationToken ct = default)
    {
        var ext = await ValidarAsync(archivo, nombreBase, ct);
        var limpio = Sanear(nombreBase);
        var carpetaLimpia = Sanear(carpeta);

        var destinoDir = Path.Combine(_raiz, carpetaLimpia);
        Directory.CreateDirectory(destinoDir);

        // Se borran las versiones en otros formatos del mismo producto
        foreach (var e in Permitidas.Where(e => e != ext))
        {
            var viejo = Path.Combine(destinoDir, limpio + e);
            if (File.Exists(viejo)) File.Delete(viejo);
        }

        await using var flujo = archivo.OpenReadStream();
        await using (var salida = File.Create(Path.Combine(destinoDir, limpio + ext)))
            await flujo.CopyToAsync(salida, ct);

        _log.LogInformation("Imagen guardada en disco: {Archivo}", limpio + ext);
        return $"/media/{carpetaLimpia}/{limpio}{ext}?v={DateTime.UtcNow.Ticks}";
    }

    public Task EliminarAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("/media/")) return Task.CompletedTask;

        var rel = url.Split('?')[0][7..].Replace('/', Path.DirectorySeparatorChar);
        var fisica = Path.GetFullPath(Path.Combine(_raiz, rel));

        // Nunca borrar fuera de la carpeta del almacén
        if (!fisica.StartsWith(Path.GetFullPath(_raiz), StringComparison.Ordinal))
            return Task.CompletedTask;

        if (File.Exists(fisica)) File.Delete(fisica);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Producción: Azure Blob Storage.
///
/// No se usa wwwroot a propósito: esa carpeta se reemplaza completa en
/// cada despliegue y las fotos que suba el dueño se perderían.
/// </summary>
public class AlmacenBlob : AlmacenBase, IAlmacen
{
    private readonly BlobContainerClient _contenedor;
    private readonly ILogger<AlmacenBlob> _log;

    public bool Disponible => true;
    public string Modo => "Azure Blob Storage";

    public AlmacenBlob(IConfiguration cfg, ILogger<AlmacenBlob> log)
    {
        _log = log;
        var cs = cfg.GetConnectionString("Blob")!;
        var nombre = cfg["Blob:Contenedor"] ?? "media";
        _contenedor = new BlobContainerClient(cs, nombre);
        _contenedor.CreateIfNotExists(PublicAccessType.Blob);
        _log.LogInformation("Almacenamiento en Blob: contenedor {Nombre}", nombre);
    }

    public async Task<string> GuardarAsync(
        IFormFile archivo, string carpeta, string nombreBase, CancellationToken ct = default)
    {
        var ext = await ValidarAsync(archivo, nombreBase, ct);
        var limpio = Sanear(nombreBase);
        var carpetaLimpia = Sanear(carpeta);

        foreach (var e in Permitidas.Where(e => e != ext))
            await _contenedor.DeleteBlobIfExistsAsync($"{carpetaLimpia}/{limpio}{e}", cancellationToken: ct);

        var blob = _contenedor.GetBlobClient($"{carpetaLimpia}/{limpio}{ext}");

        await using var flujo = archivo.OpenReadStream();
        await blob.UploadAsync(flujo, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = TipoMime(ext),
                CacheControl = "public, max-age=31536000"
            }
        }, ct);

        _log.LogInformation("Imagen subida a Blob: {Url}", blob.Uri);
        return $"{blob.Uri}?v={DateTime.UtcNow.Ticks}";
    }

    public async Task EliminarAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        var baseUri = _contenedor.Uri.ToString().TrimEnd('/');
        if (!url.StartsWith(baseUri, StringComparison.OrdinalIgnoreCase)) return;

        var nombre = url.Split('?')[0][(baseUri.Length + 1)..];
        await _contenedor.DeleteBlobIfExistsAsync(nombre, cancellationToken: ct);
    }
}
