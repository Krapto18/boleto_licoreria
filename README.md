# Boleto Licorería — .NET 10 + Azure

Sitio público y panel de administración. ASP.NET Core Razor Pages, EF Core,
Azure SQL. Un solo App Service.

> **Sin compilar.** Este código se escribió sin SDK de .NET disponible, así que
> no pasó por `dotnet build`. Espera algún ajuste de nombres de paquete o
> firmas al primer intento. Todo lo demás (catálogo, precios, frontend) sí está
> validado.

## Por qué Razor Pages y no Blazor

El frontend ya existía, estaba probado y encierra todo el trabajo de IHC
(`docs/decisiones-ihc.md`). Reescribirlo en Blazor era riesgo puro sin beneficio
para el usuario. Blazor Server además exige WebSockets y un circuito SignalR
permanente: lo contrario de lo que necesita una página que debe cargar rápido
con mala señal a las 3 a.m.

Razor Pages sirve el mismo HTML/CSS/JS de siempre. Solo cambian dos cosas:

- `data.js` desapareció: el servidor inyecta `CONFIG`, `GRUPOS` y `PRODUCTOS`
  desde la base con las mismas claves. **`app.js` no cambió ni una línea.**
- El botón "Descargar archivo" del panel ahora hace `POST` y publica de verdad.

## Estructura

```
Boleto.sln
├── src/Boleto.Data/          EF Core: entidades, contexto, seed de 55 productos
└── src/Boleto.Web/
    ├── Program.cs            DI, Identity, health checks, API del catálogo
    ├── Pages/Index           página pública (Razor)
    ├── Pages/Panel           panel, requiere login
    ├── Pages/Cuenta          login y salir
    ├── Services/             CatalogoService con caché en memoria
    └── wwwroot/              css, js, assets, sw.js, manifest
infra/crear-azure.sh          provisiona todo con az CLI
.github/workflows/            despliegue automático
docker-compose.yml            SQL Server local para desarrollar
```

## Correr en local

```bash
docker compose up -d

cd src/Boleto.Web
dotnet user-secrets set "ConnectionStrings:Sql" \
  "Server=localhost,1433;Database=boleto;User Id=sa;Password=Boleto_Local_2026;TrustServerCertificate=True"
dotnet user-secrets set "Admin:Email" "tucorreo@ejemplo.com"
dotnet user-secrets set "Admin:Password" "UnaClaveLarga2026!"

cd ../..
dotnet tool install --global dotnet-ef        # si no lo tienes
dotnet ef migrations add Inicial -p src/Boleto.Data -s src/Boleto.Web
dotnet run --project src/Boleto.Web
```

El seed corre solo al arrancar y es **idempotente**: inserta lo que falta y
nunca pisa precios ya editados desde el panel.

- Sitio: `https://localhost:xxxx`
- Panel: `https://localhost:xxxx/panel`

## Desplegar en Azure

```bash
az login
./infra/crear-azure.sh
```

Después, para el despliegue automático:

```bash
az webapp deployment list-publishing-profiles \
  -g rg-boleto -n boleto-licoreria --xml
```

Ese XML va en el repo como secreto `AZURE_PUBLISH_PROFILE` y cada push a `main`
despliega solo.

## Costo mensual

Verificado en el calculador de Azure, región **East US 2**:

| Recurso | SKU | USD/mes |
|---|---|---|
| App Service Plan | B1 Linux · 1 core, 1.75 GB | $12.41 |
| Azure SQL | Single DB, DTU, Basic 5 DTU, 2 GB | $4.90 |
| Certificado SSL administrado | — | $0.00 |
| Dominio .com | pago anual de $11.99 | $1.00 |
| **Total** | | **$18.31** |

Unos **S/ 68/mes** a 3.7 por dólar.

**Cotización al cliente: S/ 170/mes**, desglosado en la factura como
infraestructura Azure S/ 90 y soporte S/ 80. Así, si Azure sube o crece el
tráfico, se ajusta solo esa línea sin renegociar el contrato.

La suscripción de Azure va **a nombre del cliente y con su tarjeta**. No
financies tú su infraestructura.

### Detalles de costo que importan

- **El tier Basic no admite reservas ni savings plans.** No hay descuento por
  compromiso anual. Si el proyecto crece, recién ahí evaluar Premium v3 reservado.
- **Long Term Retention del SQL se cobra aparte.** Déjalo en 0: el
  Point-in-Time Restore ya viene incluido sin costo y alcanza de sobra.
- **App Service y SQL en la misma región.** Separarlos agrega latencia y puede
  facturar transferencia entre regiones.
- **Chile Central** queda más cerca de Lima (~30 ms contra ~80 ms), pero suele
  costar más. La diferencia de latencia es imperceptible en esta landing porque
  el catálogo se sirve desde caché en memoria.

### Por qué B1 y no F1

F1 no tiene Always On (y son 60 minutos de CPU al día). App Service descarga la app tras 20 minutos sin tráfico y
el primer visitante espera el arranque en frío. En una tienda cuyo argumento es
"estamos a las 3 a.m.", ese visitante de madrugada es exactamente el cliente que
no puedes perder. Lo mismo descarta cualquier cosa con scale-to-zero.

### Por qué Basic alcanza

La web pública **no toca la base**. `CatalogoService` mantiene el catálogo en
memoria y solo lo relee cuando el panel publica. La base recibe escrituras un
par de veces al mes.

De paso: si la base se cae, el sitio público sigue sirviendo el último catálogo
bueno. Para una tienda 24/7 eso es la diferencia entre vender y no vender.

## Seguridad del panel

- Login con ASP.NET Core Identity, bloqueo tras 5 intentos.
- Cookie solo por HTTPS, sesión de 8 horas.
- Token antiforgery por cabecera en el POST de publicar.
- El servidor **revalida** todo: precio mayor a cero y combo más caro que la
  botella. No se confía en lo que manda el navegador.
- Cada cambio queda en `CambiosPrecio` con usuario y fecha.

**Después del primer arranque, borra `Admin__Password` de App Settings.** Ya
quedó el usuario creado; dejar la contraseña ahí no aporta nada.

Capa extra opcional: Access Restrictions por IP sobre `/panel`.

## Pendientes

1. **Dirección del local** → activa el JSON-LD y hace que Google muestre
   "Abierto 24 horas". Es lo de mayor retorno que falta.
2. Logo, favicon, `og.png` e íconos PWA oficiales (los actuales son placeholder).
3. Fotos de producto en Blob Storage; llenar `Imagen` en cada producto.
4. GA4 y Meta Pixel: se guardan en la tabla `Tienda` y se cargan solo si tienen
   valor.
5. Migrar `Tienda` a una pantalla del panel (hoy se edita por SQL).
