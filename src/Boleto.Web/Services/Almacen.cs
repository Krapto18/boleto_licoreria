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
    /// Ancho y alto leídos de la cabecera del archivo, sin decodificar
    /// la imagen ni depender de una librería de imágenes.
    ///
    /// Se usa para el logo de la portada: es el elemento más grande de
    /// la primera pantalla, y sin declarar su tamaño en el HTML el hero
    /// salta cuando la imagen termina de cargar. Devuelve (0, 0) si el
    /// formato no se reconoce — quien llama decide qué hacer con eso.
    /// </summary>
    public static async Task<(int Ancho, int Alto)> MedirAsync(
        Stream s, CancellationToken ct = default)
    {
        // 64 KB alcanzan de sobra: en los tres formatos el tamaño está
        // en los primeros bytes, salvo JPEG, donde hay que saltar
        // segmentos hasta el SOF. Un EXIF enorme podría empujarlo más
        // allá; en ese caso se devuelve (0,0) y no se declara tamaño.
        var buf = new byte[64 * 1024];
        var n = 0;
        while (n < buf.Length)
        {
            var leidos = await s.ReadAsync(buf.AsMemory(n, buf.Length - n), ct);
            if (leidos == 0) break;
            n += leidos;
        }
        if (n < 16) return (0, 0);
        var b = buf.AsSpan(0, n);

        static int Be16(ReadOnlySpan<byte> d, int i) => (d[i] << 8) | d[i + 1];
        static int Be32(ReadOnlySpan<byte> d, int i) =>
            (d[i] << 24) | (d[i + 1] << 16) | (d[i + 2] << 8) | d[i + 3];
        static int Le16(ReadOnlySpan<byte> d, int i) => d[i] | (d[i + 1] << 8);

        // ── PNG: la cabecera IHDR va siempre en el mismo sitio ──
        if (b[0] == 0x89 && b[1] == 0x50 && n >= 24)
            return (Be32(b, 16), Be32(b, 20));

        // ── WebP: tres codificaciones, tres sitios distintos ──
        if (n >= 30 && b[0] == 0x52 && b[8] == 0x57 && b[9] == 0x45)
        {
            var tipo = System.Text.Encoding.ASCII.GetString(b.Slice(12, 4));
            if (tipo == "VP8 ")                       // con pérdida
                return (Le16(b, 26) & 0x3FFF, Le16(b, 28) & 0x3FFF);
            if (tipo == "VP8L")                       // sin pérdida
            {
                var bits = b[21] | (b[22] << 8) | (b[23] << 16) | (b[24] << 24);
                return ((bits & 0x3FFF) + 1, ((bits >> 14) & 0x3FFF) + 1);
            }
            if (tipo == "VP8X")                       // extendido
                return ((b[24] | (b[25] << 8) | (b[26] << 16)) + 1,
                        (b[27] | (b[28] << 8) | (b[29] << 16)) + 1);
        }

        // ── JPEG: hay que recorrer segmentos hasta el marcador SOF ──
        if (b[0] == 0xFF && b[1] == 0xD8)
        {
            var i = 2;
            while (i + 9 < n)
            {
                if (b[i] != 0xFF) { i++; continue; }
                var m = b[i + 1];
                if (m is 0xD8 or 0x01 or >= 0xD0 and <= 0xD7) { i += 2; continue; }
                var largo = Be16(b, i + 2);
                if (largo < 2) break;
                // SOF0-3, 5-7, 9-11, 13-15. Se excluyen DHT/JPG/DAC.
                if (m is >= 0xC0 and <= 0xCF && m is not (0xC4 or 0xC8 or 0xCC))
                    return (Be16(b, i + 7), Be16(b, i + 5));
                i += 2 + largo;
            }
        }

        return (0, 0);
    }

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
