using Boleto.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Boleto.Data;

/// <summary>
/// Aplica migraciones y carga el catálogo inicial.
/// Es idempotente: solo inserta lo que falta, nunca pisa precios editados
/// desde el panel. Se puede correr en cada arranque sin miedo.
/// </summary>
public static class SeedData
{
    public static async Task InicializarAsync(
        BoletoDbContext db,
        UserManager<IdentityUser> users,
        string adminEmail,
        string adminPassword,
        CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (!await db.Tienda.AnyAsync(ct))
        {
            /* Zonas, pagos y tiempo de entrega nacen vacíos a propósito:
               mientras el cliente no los confirme, esas secciones no
               aparecen en la web. Se llenan desde el panel. */
            db.Tienda.Add(new Tienda { Id = 1 });
            await db.SaveChangesAsync(ct);
        }

        var existentes = await db.Productos.Select(p => p.Id).ToListAsync(ct);
        var faltantes = Catalogo.Where(p => !existentes.Contains(p.Id)).ToList();
        if (faltantes.Count > 0)
        {
            db.Productos.AddRange(faltantes);
            await db.SaveChangesAsync(ct);
        }

        // Usuario del panel. La contraseña viene de configuración, nunca del código.
        if (!string.IsNullOrWhiteSpace(adminEmail) &&
            !string.IsNullOrWhiteSpace(adminPassword) &&
            await users.FindByEmailAsync(adminEmail) is null)
        {
            var u = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var r = await users.CreateAsync(u, adminPassword);
            if (!r.Succeeded)
                throw new InvalidOperationException(
                    "No se pudo crear el usuario del panel: " +
                    string.Join("; ", r.Errors.Select(e => e.Description)));
        }
    }

    /// <summary>Catálogo del cliente. 55 productos, precios del PDF original.</summary>
    private static readonly Producto[] Catalogo =
    [
        new() { Id = "jw-red", Nombre = "Johnnie Walker Red Label", Presentacion = "750 ml", Categoria = "Whisky", Grupo = "Whiskys", Precio = 45.00m, PrecioCombo = 51.00m, ComboAcompanante = "Gaseosa 1.5 L", ComboHielo = "Hielo 3 kg", Promo = true, Stock = true, Color = "#A8631F", Orden = 10 },
        new() { Id = "jw-dblack", Nombre = "Johnnie Walker Doble Black", Presentacion = "750 ml", Categoria = "Whisky", Grupo = "Whiskys", Precio = 138.60m, PrecioCombo = 144.00m, ComboAcompanante = "Gaseosa 1.5 L", ComboHielo = "Hielo 3 kg", Promo = true, Stock = true, Color = "#4A3520", Orden = 20 },
        new() { Id = "jw-black", Nombre = "Johnnie Walker Black Label", Presentacion = "750 ml", Categoria = "Whisky", Grupo = "Whiskys", Precio = 99.90m, PrecioCombo = 106.90m, ComboAcompanante = "Gaseosa 1.5 L", ComboHielo = "Hielo 3 kg", Promo = true, Stock = true, Color = "#6B4A22", Orden = 30 },
        new() { Id = "jd-trad", Nombre = "Jack Daniel's Tradicional", Presentacion = "700 ml", Categoria = "Whisky", Grupo = "Whiskys", Precio = 97.90m, PrecioCombo = 104.90m, ComboAcompanante = "Gaseosa 1.5 L", ComboHielo = "Hielo 3 kg", Promo = true, Stock = true, Color = "#3A2E22", Orden = 40 },
        new() { Id = "jd-fire", Nombre = "Jack Daniel's Fire", Presentacion = "700 ml", Categoria = "Whisky", Grupo = "Whiskys", Precio = 109.90m, PrecioCombo = 116.90m, ComboAcompanante = "Gaseosa 1.5 L", ComboHielo = "Hielo 3 kg", Promo = true, Stock = true, Color = "#8C2B1E", Orden = 50 },
        new() { Id = "jd-apple", Nombre = "Jack Daniel's Apple", Presentacion = "700 ml", Categoria = "Whisky", Grupo = "Whiskys", Precio = 109.90m, PrecioCombo = 116.90m, ComboAcompanante = "Gaseosa 1.5 L", ComboHielo = "Hielo 3 kg", Promo = true, Stock = true, Color = "#5E7A32", Orden = 60 },
        new() { Id = "old-parr", Nombre = "Old Parr 12 años", Presentacion = "700 ml", Categoria = "Whisky", Grupo = "Whiskys", Precio = 78.90m, PrecioCombo = 84.90m, ComboAcompanante = "Gaseosa 1.5 L", ComboHielo = "Hielo 3 kg", Promo = true, Stock = true, Color = "#9C7534", Orden = 70 },
        new() { Id = "something", Nombre = "Something Special", Presentacion = "700 ml", Categoria = "Whisky", Grupo = "Whiskys", Precio = 60.90m, PrecioCombo = 66.90m, ComboAcompanante = "Gaseosa 1.5 L", ComboHielo = "Hielo 3 kg", Promo = true, Stock = true, Color = "#B08744", Orden = 80 },
        new() { Id = "ballantines", Nombre = "Ballantine's Finest", Presentacion = "700 ml", Categoria = "Whisky", Grupo = "Whiskys", Precio = 57.90m, PrecioCombo = 63.90m, ComboAcompanante = "Gaseosa 1.5 L", ComboHielo = "Hielo 3 kg", Promo = true, Stock = true, Color = "#A67C3D", Orden = 90 },
        new() { Id = "chivas-12", Nombre = "Chivas Regal 12 años", Presentacion = "700 ml", Categoria = "Whisky", Grupo = "Whiskys", Precio = 89.90m, PrecioCombo = 95.90m, ComboAcompanante = "Gaseosa 1.5 L", ComboHielo = "Hielo 3 kg", Promo = true, Stock = true, Color = "#B87333", Orden = 100 },
        new() { Id = "chivas-gin", Nombre = "Chivas Regal + Gin", Presentacion = "Pack", Categoria = "Whisky", Grupo = "Whiskys", Precio = 199.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = true, Stock = true, Color = "#8E5A28", Orden = 110 },
        new() { Id = "hc-7", Nombre = "Havana Club 7 años", Presentacion = "700 ml", Categoria = "Ron", Grupo = "Rones y piscos", Precio = 75.00m, PrecioCombo = 81.00m, ComboAcompanante = "Gaseosa 1.5 L", ComboHielo = "Hielo 3 kg", Promo = true, Stock = true, Color = "#6B3B22", Orden = 120 },
        new() { Id = "hc-esp", Nombre = "Havana Club Especial", Presentacion = "700 ml", Categoria = "Ron", Grupo = "Rones y piscos", Precio = 37.90m, PrecioCombo = 44.90m, ComboAcompanante = "Gaseosa 1.5 L", ComboHielo = "Hielo 3 kg", Promo = true, Stock = true, Color = "#C1541E", Orden = 130 },
        new() { Id = "flor-oro", Nombre = "Flor de Caña Oro", Presentacion = "750 ml", Categoria = "Ron", Grupo = "Rones y piscos", Precio = 48.90m, PrecioCombo = 54.90m, ComboAcompanante = "Gaseosa 1.5 L", ComboHielo = "Hielo 3 kg", Promo = true, Stock = true, Color = "#B5892F", Orden = 140 },
        new() { Id = "barcelo", Nombre = "Ron Barceló", Presentacion = "1.75 L", Categoria = "Ron", Grupo = "Rones y piscos", Precio = 98.00m, PrecioCombo = 104.90m, ComboAcompanante = "Gaseosa 1.5 L", ComboHielo = "Hielo 3 kg", Promo = true, Stock = true, Color = "#7A4A2B", Orden = 150 },
        new() { Id = "pisco-queb", Nombre = "Pisco Quebranta", Presentacion = "Botella", Categoria = "Pisco", Grupo = "Rones y piscos", Precio = 35.90m, PrecioCombo = 43.90m, ComboAcompanante = "Ginger ale 1.5 L", ComboHielo = "Hielo 3 kg", Promo = false, Stock = true, Color = "#B8C4CE", Orden = 160 },
        new() { Id = "pisco-acho", Nombre = "Pisco Acholado", Presentacion = "Botella", Categoria = "Pisco", Grupo = "Rones y piscos", Precio = 38.90m, PrecioCombo = 43.90m, ComboAcompanante = "Ginger ale 1.5 L", ComboHielo = "Hielo 3 kg", Promo = false, Stock = true, Color = "#A9B7C4", Orden = 170 },
        new() { Id = "pisco-750", Nombre = "Pisco Acholado", Presentacion = "750 ml", Categoria = "Pisco", Grupo = "Rones y piscos", Precio = 28.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#C0CBD6", Orden = 180 },
        new() { Id = "smirnoff-man", Nombre = "Smirnoff Manzana", Presentacion = "700 ml", Categoria = "Vodka", Grupo = "Vodka, gin y otros", Precio = 32.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#8FBF5A", Orden = 190 },
        new() { Id = "smirnoff-roj", Nombre = "Smirnoff Rojo", Presentacion = "700 ml", Categoria = "Vodka", Grupo = "Vodka, gin y otros", Precio = 32.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#C43B3B", Orden = 200 },
        new() { Id = "absolut", Nombre = "Absolut Vodka", Presentacion = "750 ml", Categoria = "Vodka", Grupo = "Vodka, gin y otros", Precio = 46.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#B9C6D4", Orden = 210 },
        new() { Id = "absolut-sab", Nombre = "Absolut Sabores", Presentacion = "750 ml", Categoria = "Vodka", Grupo = "Vodka, gin y otros", Precio = 52.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#C98FC0", Orden = 220 },
        new() { Id = "skyy", Nombre = "Skyy Vodka", Presentacion = "1 L", Categoria = "Vodka", Grupo = "Vodka, gin y otros", Precio = 59.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#3B6FC4", Orden = 230 },
        new() { Id = "gin-bombay", Nombre = "Gin Bombay Sapphire", Presentacion = "750 ml", Categoria = "Gin", Grupo = "Vodka, gin y otros", Precio = 85.00m, PrecioCombo = 98.90m, ComboAcompanante = "Tónica 1.5 L", ComboHielo = "Hielo 3 kg", Promo = false, Stock = true, Color = "#2C6BB5", Orden = 240 },
        new() { Id = "gin-tanq", Nombre = "Gin Tanqueray", Presentacion = "700 ml", Categoria = "Gin", Grupo = "Vodka, gin y otros", Precio = 94.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#2E6B4F", Orden = 250 },
        new() { Id = "gin-beef", Nombre = "Gin Beefeater", Presentacion = "750 ml", Categoria = "Gin", Grupo = "Vodka, gin y otros", Precio = 59.90m, PrecioCombo = 69.90m, ComboAcompanante = "Tónica 1.5 L", ComboHielo = "Hielo 3 kg", Promo = false, Stock = true, Color = "#B23A3A", Orden = 260 },
        new() { Id = "gin-beef-pk", Nombre = "Gin Beefeater", Presentacion = "Pack", Categoria = "Gin", Grupo = "Vodka, gin y otros", Precio = 65.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#A83232", Orden = 270 },
        new() { Id = "gin-beef-pnk", Nombre = "Gin Beefeater Pink", Presentacion = "Pack", Categoria = "Gin", Grupo = "Vodka, gin y otros", Precio = 87.50m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#D96A8A", Orden = 280 },
        new() { Id = "jager", Nombre = "Jägermeister", Presentacion = "700 ml", Categoria = "Licor", Grupo = "Vodka, gin y otros", Precio = 79.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#2E4A1F", Orden = 290 },
        new() { Id = "crema", Nombre = "Licor de crema", Presentacion = "750 ml", Categoria = "Licor", Grupo = "Vodka, gin y otros", Precio = 72.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#C8A87A", Orden = 300 },
        new() { Id = "don-julio", Nombre = "Tequila Don Julio", Presentacion = "750 ml", Categoria = "Tequila", Grupo = "Vodka, gin y otros", Precio = 199.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#D9CFA8", Orden = 310 },
        new() { Id = "aguardiente", Nombre = "Aguardiente", Presentacion = "700 ml", Categoria = "Licor", Grupo = "Vodka, gin y otros", Precio = 35.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#DCD6C4", Orden = 320 },
        new() { Id = "v-tannat", Nombre = "Vino Tannat", Presentacion = "750 ml", Categoria = "Vino", Grupo = "Vinos y espumantes", Precio = 28.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#5E1B2C", Orden = 330 },
        new() { Id = "v-syrah", Nombre = "Vino Syrah", Presentacion = "750 ml", Categoria = "Vino", Grupo = "Vinos y espumantes", Precio = 28.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#6B2233", Orden = 340 },
        new() { Id = "v-malbec", Nombre = "Vino Malbec", Presentacion = "750 ml", Categoria = "Vino", Grupo = "Vinos y espumantes", Precio = 28.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#732536", Orden = 350 },
        new() { Id = "v-cabernet", Nombre = "Vino Cabernet / Petit Verdot", Presentacion = "750 ml", Categoria = "Vino", Grupo = "Vinos y espumantes", Precio = 39.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#571A29", Orden = 360 },
        new() { Id = "v-malmer", Nombre = "Vino Malbec Merlot", Presentacion = "750 ml", Categoria = "Vino", Grupo = "Vinos y espumantes", Precio = 48.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#7C2B3E", Orden = 370 },
        new() { Id = "v-rose-2", Nombre = "Tabernero Rosé — 2 vinos", Presentacion = "Pack x2", Categoria = "Vino", Grupo = "Vinos y espumantes", Precio = 39.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#D48A9B", Orden = 380 },
        new() { Id = "v-borg-2", Nombre = "Tabernero Borgoña — 2 vinos", Presentacion = "Pack x2", Categoria = "Vino", Grupo = "Vinos y espumantes", Precio = 39.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#6B2233", Orden = 390 },
        new() { Id = "esp-rose", Nombre = "Espumante Rosé", Presentacion = "750 ml", Categoria = "Espumante", Grupo = "Vinos y espumantes", Precio = 64.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#E0A0AE", Orden = 400 },
        new() { Id = "esp-prosecco", Nombre = "Espumante Prosecco", Presentacion = "750 ml", Categoria = "Espumante", Grupo = "Vinos y espumantes", Precio = 64.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#D9C98A", Orden = 410 },
        new() { Id = "esp-asti", Nombre = "Espumante Asti", Presentacion = "750 ml", Categoria = "Espumante", Grupo = "Vinos y espumantes", Precio = 60.50m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#CFBE7A", Orden = 420 },
        new() { Id = "pilsen-6-473", Nombre = "Six pack Pilsen", Presentacion = "473 ml", Categoria = "Cerveza", Grupo = "Cervezas", Precio = 34.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#1E6B3A", Orden = 430 },
        new() { Id = "pilsen-6-355", Nombre = "Six pack Pilsen", Presentacion = "355 ml", Categoria = "Cerveza", Grupo = "Cervezas", Precio = 28.00m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#248046", Orden = 440 },
        new() { Id = "pilsen-12", Nombre = "Twelve pack Pilsen", Presentacion = "355 ml", Categoria = "Cerveza", Grupo = "Cervezas", Precio = 56.00m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#1B5C33", Orden = 450 },
        new() { Id = "hielo", Nombre = "Hielo en bolsa", Presentacion = "3 kg", Categoria = "Hielo", Grupo = "Bebidas y hielo", Precio = 6.00m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#7FB4DC", Orden = 460 },
        new() { Id = "coca", Nombre = "Coca Cola", Presentacion = "1.5 L", Categoria = "Gaseosa", Grupo = "Bebidas y hielo", Precio = 8.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#8C2B2B", Orden = 470 },
        new() { Id = "ever-ginger", Nombre = "Evervess Ginger Ale", Presentacion = "1.5 L", Categoria = "Gaseosa", Grupo = "Bebidas y hielo", Precio = 8.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#C8A96A", Orden = 480 },
        new() { Id = "ever-tonica", Nombre = "Evervess Tónica", Presentacion = "1.5 L", Categoria = "Gaseosa", Grupo = "Bebidas y hielo", Precio = 8.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#D8D2BE", Orden = 490 },
        new() { Id = "schweppes", Nombre = "Schweppes Citrus", Presentacion = "1.5 L", Categoria = "Gaseosa", Grupo = "Bebidas y hielo", Precio = 8.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#C9C24A", Orden = 500 },
        new() { Id = "redbull", Nombre = "Red Bull", Presentacion = "255 ml", Categoria = "Energizante", Grupo = "Bebidas y hielo", Precio = 6.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#3B5EA8", Orden = 510 },
        new() { Id = "mikes", Nombre = "Mike's Hard", Presentacion = "355 ml", Categoria = "Listo", Grupo = "Bebidas y hielo", Precio = 6.00m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#D9A93C", Orden = 520 },
        new() { Id = "chil-porton", Nombre = "Chilcano Portón", Presentacion = "355 ml", Categoria = "Listo", Grupo = "Bebidas y hielo", Precio = 7.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#B8C86A", Orden = 530 },
        new() { Id = "chil-tab", Nombre = "Chilcano Tabernero", Presentacion = "275 ml", Categoria = "Listo", Grupo = "Bebidas y hielo", Precio = 6.50m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#A9BF5E", Orden = 540 },
        new() { Id = "vina-vieja", Nombre = "Viña Vieja", Presentacion = "473 ml", Categoria = "Listo", Grupo = "Bebidas y hielo", Precio = 8.90m, PrecioCombo = null, ComboAcompanante = "", ComboHielo = "", Promo = false, Stock = true, Color = "#8A3A4A", Orden = 550 }
    ];
}
