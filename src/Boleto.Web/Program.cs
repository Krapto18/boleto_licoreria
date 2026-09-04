using Boleto.Data;
using Boleto.Web.Models;
using Boleto.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// ── Base de datos ────────────────────────────────────────────────
// La cadena viene de App Settings en Azure, nunca de appsettings.json.
var cs = builder.Configuration.GetConnectionString("Sql")
         ?? throw new InvalidOperationException(
             "Falta la cadena de conexión 'Sql'. En Azure va en App Settings " +
             "como ConnectionStrings__Sql; en local, con dotnet user-secrets.");

// Factory porque el servicio de catálogo lo usa fuera del scope de una request.
builder.Services.AddDbContextFactory<BoletoDbContext>(o =>
    o.UseSqlServer(cs, sql =>
    {
        // Azure SQL corta conexiones ociosas. Sin esto se ven errores intermitentes.
        sql.EnableRetryOnFailure(maxRetryCount: 5,
                                 maxRetryDelay: TimeSpan.FromSeconds(10),
                                 errorNumbersToAdd: null);
        sql.CommandTimeout(30);
    }));

// ── Identidad del panel ──────────────────────────────────────────
builder.Services.AddDefaultIdentity<IdentityUser>(o =>
{
    o.SignIn.RequireConfirmedAccount = false;
    o.Password.RequiredLength = 12;
    o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<BoletoDbContext>();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/cuenta/login";
    o.AccessDeniedPath = "/cuenta/login";
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.SlidingExpiration = true;
    // En Development se permite http o el login no funciona en local.
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
});

// El panel manda el token por cabecera, no en un form. Sin esto, 400.
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<CatalogoService>();

/* Almacenamiento de imágenes: Blob si hay cadena configurada, disco
   local si no. Así en desarrollo se prueba el flujo completo sin
   emuladores ni contenedores, y al desplegar no cambia nada de código. */
var usaBlob = !string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Blob"));

/* Fuera de Development el disco local no es una alternativa: el despliegue
   reemplaza el contenido del sitio y se llevaría por delante las fotos que
   subió el dueño. Antes esto caía en silencio al almacén local y la pérdida
   recién se notaba al desplegar la siguiente versión. Mejor no arrancar. */
if (!usaBlob && !builder.Environment.IsDevelopment())
    throw new InvalidOperationException(
        "Falta la cadena de conexión 'Blob'. Fuera de desarrollo las imágenes " +
        "deben ir a Azure Blob Storage: en disco se pierden en cada despliegue. " +
        "Configúrala en App Settings como ConnectionStrings__Blob " +
        "(infra/crear-azure.sh la deja lista).");

if (usaBlob) builder.Services.AddSingleton<IAlmacen, AlmacenBlob>();
else builder.Services.AddSingleton<IAlmacen, AlmacenLocal>();

/* Tope de subida. Coincide con el que valida AlmacenService, para que
   un archivo grande se rechace con un mensaje claro y no con un error
   genérico del servidor. */
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 2 * 1024 * 1024;
});
builder.Services.AddResponseCompression(o => o.EnableForHttps = true);

builder.Services.AddRazorPages(o =>
{
    o.Conventions.AuthorizeFolder("/Panel");
    o.Conventions.AllowAnonymousToPage("/Cuenta/Login");
});

/* Unhealthy, no Degraded: el middleware mapea Degraded a HTTP 200, así que
   con la base caída este endpoint respondía 200 — un monitor incapaz de
   reportar la única falla que existe para detectar. */
builder.Services.AddHealthChecks()
    .AddDbContextCheck<BoletoDbContext>("sql", HealthStatus.Unhealthy, tags: ["db"]);

// App Service termina el TLS antes de llegar a Kestrel.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseResponseCompression();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.File.Name;
        // El service worker debe revalidarse siempre o el usuario queda pegado
        // a una versión vieja del sitio.
        ctx.Context.Response.Headers.CacheControl =
            path.Equals("sw.js", StringComparison.OrdinalIgnoreCase)
                ? "no-cache"
                : "public, max-age=604800";
    }
});

/* En modo disco local, las imágenes subidas se sirven bajo /media con
   la misma forma de URL que tendrían en Blob. En producción con Blob
   esta ruta no se usa. */
if (!usaBlob)
{
    var mediaDir = Path.Combine(app.Environment.ContentRootPath, "media");
    Directory.CreateDirectory(mediaDir);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(mediaDir),
        RequestPath = "/media",
        OnPrepareResponse = ctx =>
            ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000"
    });
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

// ── API del catálogo: la usa el service worker de la PWA ─────────
app.MapGet("/api/catalogo", async (CatalogoService svc, CancellationToken ct) =>
        Results.Ok(await svc.ObtenerAsync(ct)))
   .AllowAnonymous()
   .WithName("Catalogo");

/* Always On pega acá cada pocos minutos. Es liveness puro: no consulta la
   base. Con el chequeo de SQL incluido, el ping de mantenerse despierto
   gastaba DTU cada pocos minutos las 24 horas — justo lo que el diseño de
   caché en memoria evita para el tráfico real. */
app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false })
   .AllowAnonymous();

// Diagnóstico manual: este sí comprueba que la base responda.
app.MapHealthChecks("/health/db", new HealthCheckOptions { Predicate = c => c.Tags.Contains("db") })
   .AllowAnonymous();

// ── Migraciones y catálogo inicial ───────────────────────────────
// Corre en el arranque. Es idempotente: nunca pisa precios editados.
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Seed");
    var factory = sp.GetRequiredService<IDbContextFactory<BoletoDbContext>>();
    await using var db = await factory.CreateDbContextAsync();

    /* La migración no se tolera. Antes iba dentro del try junto con el
       sembrado, apoyada en que la web podía seguir sirviendo desde caché;
       pero en un arranque en frío la caché está vacía, así que sin esquema
       cada visita termina en la página de error. Y con Always On el sitio
       se queda así indefinidamente: en pie, roto, y sin que nada avise.
       Fallar acá es lo que vuelve visible el problema. */
    await SeedData.MigrarAsync(db);

    try
    {
        await SeedData.InicializarAsync(
            db,
            sp.GetRequiredService<UserManager<IdentityUser>>(),
            app.Configuration["Admin:Email"] ?? "",
            app.Configuration["Admin:Password"] ?? "");
    }
    catch (Exception ex)
    {
        /* El sembrado sí se tolera: con el esquema creado el sitio
           funciona, y lo que falte se completa desde el panel. */
        log.LogError(ex, "Falló el sembrado inicial");
    }
}

app.Run();
