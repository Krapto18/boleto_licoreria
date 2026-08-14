# Boleto Licorería — .NET 10 + Azure

Sitio público y panel de administración. ASP.NET Core Razor Pages, EF Core,
Azure SQL. Un solo App Service.

## Por qué Razor Pages y no Blazor

El frontend ya existía, estaba probado y encierra todo el trabajo de IHC
(`docs/decisiones-ihc.md`). Reescribirlo en Blazor era riesgo puro sin beneficio
para el usuario. Blazor Server además exige WebSockets y un circuito SignalR
permanente: lo contrario de lo que necesita una página que debe cargar rápido
con mala señal a las 3 a.m.

Razor Pages sirve el mismo HTML/CSS/JS de siempre. Solo cambian dos cosas:

- `data.js` desapareció: el servidor inyecta `CONFIG`, `GRUPOS` y `PRODUCTOS`
  desde la base con las mismas claves, así que `app.js` siguió funcionando sin
  tocarlo.
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
    ├── Services/             CatalogoService con caché en memoria, IAlmacen
    └── wwwroot/              css, js (app.js, panel.js), assets, sw.js, manifest
infra/crear-azure.sh          provisiona todo con az CLI
tests/                        flujo de pedido en un navegador real
.github/workflows/            despliegue automático
```

## Probar el flujo de pedido

```bash
cd tests
npm install
npm test        # con la app corriendo
```

Corre el pedido completo en un navegador real —escritorio y móvil con
touch— hasta el mensaje de WhatsApp. Existe porque dos defectos reales
pasaron una verificación hecha solo con peticiones al servidor: ninguno
de los dos era visible sin un navegador. Detalle en `tests/README.md`.

## Correr en local

Contra tu instancia de SQL Server. La base `boleto` no hace falta crearla a
mano: las migraciones la levantan al arrancar.

```bash
cd src/Boleto.Web

# Instancia por defecto, autenticación de Windows
dotnet user-secrets set "ConnectionStrings:Sql" \
  "Server=localhost;Database=boleto;Trusted_Connection=True;TrustServerCertificate=True"

dotnet user-secrets set "Admin:Email" "tucorreo@ejemplo.com"
dotnet user-secrets set "Admin:Password" "UnaClaveLarga2026!"

cd ../..
dotnet tool install --global dotnet-ef        # si no lo tienes
dotnet run --project src/Boleto.Web
```

Si usas SQL Server Express, el servidor es `localhost\SQLEXPRESS`. Con usuario y
contraseña en vez de autenticación integrada:
`Server=localhost;Database=boleto;User Id=sa;Password=...;TrustServerCertificate=True`.

Las migraciones ya están en el repo y se aplican solas al arrancar. El seed corre
después y es **idempotente**: inserta lo que falta y nunca pisa precios ya
editados desde el panel.

- Sitio: `https://localhost:7181`
- Panel: `https://localhost:7181/panel`

En desarrollo no hace falta configurar Blob: las fotos que subas por el panel se
guardan en `src/Boleto.Web/media` y se sirven bajo `/media`, con la misma forma
de URL que tendrán en producción. El panel avisa en qué modo está.

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

### Fotos: Blob obligatorio en producción

`IAlmacen` tiene dos implementaciones y se elige sola según haya o no cadena de
conexión `Blob`. **Fuera de Development, la app se niega a arrancar si esa cadena
falta.** No es rigidez: el almacén en disco escribe dentro del contenido del
sitio, y el despliegue lo reemplaza — las fotos que subió el dueño
desaparecerían, y recién se notaría en el despliegue siguiente. Mejor fallar en
el arranque, que es un problema de un minuto.

`crear-azure.sh` crea la cuenta de storage y deja la cadena configurada.

## Costo mensual

Verificado en el calculador de Azure, región **East US 2**:

| Recurso | SKU | USD/mes |
|---|---|---|
| App Service Plan | B1 Linux · 1 core, 1.75 GB | $12.41 |
| Azure SQL | Single DB, DTU, Basic 5 DTU, 2 GB | $4.90 |
| Storage (fotos) | StorageV2 Standard_LRS, <1 GB | ~$0.10 |
| Certificado SSL administrado | — | $0.00 |
| Dominio .com | pago anual de $11.99 | $1.00 |
| **Total** | | **~$18.41** |

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
- **App Service, SQL y Storage en la misma región.** Separarlos agrega latencia y
  puede facturar transferencia entre regiones.
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

Por lo mismo hay dos health checks: `/health` es liveness puro y es el que pega
Always On cada pocos minutos; `/health/db` sí comprueba la conexión a SQL y se
consulta a mano. Con el chequeo de base en `/health`, el ping de mantenerse
despierto gastaba DTU las 24 horas para nada.

## Delivery por distrito

El seed carga los **43 distritos de la provincia de Lima a S/ 10**. No incluye el
Callao: es Provincia Constitucional, no Lima Metropolitana. Si el dueño reparte
allá, los agrega desde el panel.

Solo se siembran si la tabla está vacía. Si el dueño ya editó costos, el arranque
no se los pisa.

El cliente elige su distrito en la vista previa del pedido, antes de enviarlo. El
costo se suma al total y viaja desglosado en el mensaje de WhatsApp —subtotal,
delivery y total— para que no se entere del cargo recién cuando la tienda le
responde.

En el panel, la pestaña **Delivery** edita costo y tiempo por distrito, con
"aplicar a todos" sobre el filtro activo (cuando sube la gasolina son 43 campos a
mano) y un botón para reponer los distritos que se hayan quitado. Desmarcar un
distrito lo saca de la lista que ve el cliente sin perder su costo.

El costo se persiste como texto dentro de `Tienda.Zonas` con formato
`Distrito|Costo|Tiempo`, y se escribe y se lee **con cultura invariante**. Sin
fijarla, el separador decimal depende de la cultura del hilo: un `10,5` guardado
en `es-PE` se releería como `105` en un servidor invariante, y ese número
aparecería en el total del cliente.

## Navegación, banners y hero

Cuatro cambios pedidos por el dueño. Están hechos y documentados con lo que
cuesta cada uno en `docs/decisiones-ihc.md`, nivel 6.

**Dos carruseles de cinco banners** son la sección de promoción: reemplazaron a
una tarjeta hecha a mano que anunciaba lo mismo que los banners del cliente.
Vienen con cuatro piezas suyas —artes de Instagram recortadas a 4:3— sembradas
solo si la tabla está vacía, igual que las zonas. El dueño las reemplaza desde el
panel.

Los banners van acotados a 560 px de ancho: a todo el ancho, una pieza 4:3 medía
885 px de alto y había que pasar dos afiches a scroll antes de ver un producto.
Llevan flechas además del deslizamiento, que se apagan en los extremos en vez de
quitarse — un objetivo que se mueve es un objetivo que se falla. Cuesta scroll: los diez
llenos meten 380 px entre el hero y el catálogo en un móvil de 390. Se amortigua
con carga diferida de los nueve banners que no son el primero y con el enlace
"Catálogo" del menú, que salta por encima de los dos. El panel los muestra como
"carrusel de arriba" y "carrusel de abajo", cinco ranuras cada uno.

Se persisten en `Tienda.Banners` con formato `ruta|alt|enlace|carrusel`. El
cuarto campo es nuevo: las líneas guardadas antes caen en el carrusel 1, que es
donde estaban. La columna pasó de 2000 a 4000 caracteres — diez líneas con 120
de texto alternativo y 200 de enlace no entraban.

**Buscador en el nav, en lugar del sello "Abierto ahora".** El 24/7 lo siguen
diciendo la franja roja y el hero con la hora del propio cliente, que es lo que
de verdad lo hace verificable. Hay dos campos de búsqueda —el del nav y el del
catálogo— porque en móvil el nav se pliega: son el mismo estado y se copian el
texto entre sí.

**Menú hamburguesa en móvil**, con el logo y el catálogo. Esto contradice a
Nielsen #6 y es una decisión del cliente, no una recomendación. Se hizo con
`aria-expanded`, salida con Escape y devolución del foco, cierre al elegir y 44
px de objetivo. El buscador del catálogo y el botón de WhatsApp **no** están
detrás del menú: comprar y escribir no dependen de que el cliente descubra la
hamburguesa.

**El logo del negocio en lugar del titular del hero.** Un logotipo dice quién
eres, no a qué viniste. Para que la página no se quede muda, el logo va **dentro
del `<h1>`** y su texto alternativo lleva la propuesta de valor, que es lo que
leen Google y un lector de pantalla; el párrafo de abajo la repite en pantalla.
Si el archivo no carga, un `onerror` devuelve el titular de texto: un `<h1>` con
una imagen rota es un `<h1>` vacío.

## Kit de marca

Todo lo de marca sale del kit oficial del cliente. Ya no queda ningún
placeholder: el `logo.svg` de 194 KB —un PNG en base64 dentro de un SVG— y el
sello improvisado con una "B" se fueron.

Lo que se usa vive en `wwwroot/assets/`, renombrado por para qué sirve y no por
cómo venía. El inventario completo, con qué es cada archivo y de dónde sale,
está en `wwwroot/assets/LEEME.txt`.

| Dónde | Pieza |
|---|---|
| Hero y verificación de edad | `marca/logo-oscuro.svg` — la versión azul, porque el fondo es crema |
| Nav, pie, panel y login | `marca/isotipo-claro.svg` — el sello circular |
| Favicon e íconos PWA | el sello sobre cuadrado azul, rasterizado desde `marca/isotipo-pleno.svg` |
| Al compartir por WhatsApp | `og.png`, el logo marfil sobre azul |
| Resumen del pedido | `iconos/carrito.svg` |
| Sección de cierre | `iconos/hielo.svg` |
| Promoción | `iconos/acompanante.svg` — una botella con un más, que es de lo que trata |

**Los iconos se pintan como máscara CSS**, no como `<img>`. Así heredan el color
del texto que acompañan y el mismo archivo sirve sobre la chapa crema y sobre el
fondo noche. Si el navegador no soporta máscaras, el icono no se dibuja — no
aparece un cuadrado de color donde debería haber una silueta.

**Al logo se le ciñó el `viewBox`.** El arte ocupa 909×624 dentro de un lienzo de
1046×1030: el 40 % del alto era vacío, y en el hero eso salía como un bache entre
el logo y la filigrana.

**La paleta pasó a la oficial**, leída de los propios SVG del kit: azul
`#000625` y rojo `#CF2026`. Antes eran `#000725` y `#C8102E`, aproximaciones de
cuando no había manual. El rojo de marca sobre crema da 4.80:1, por encima del
4.5 que pide AA. **El crema del hero se mantiene** en `#F7F1E3`: el marfil de la
marca (`#FEFCEC`) es casi blanco y aclararía toda la chapa, que no es lo que se
pidió — está a un cambio de variable si el cliente lo prefiere.

**El service worker subió a `boleto-v2`.** Sin subir la versión, quien ya visitó
el sitio seguiría viendo el logo anterior desde su caché.

## El combo y de qué está hecho

Un combo es la botella más un **aditivo** —gaseosa, ginger, cualquier
acompañante— y/o **hielo**. La pestaña de precios define los dos: qué es, cuánto
costaría suelto y una casilla de si va incluido.

- **Apagar no borra.** Se acaba la gaseosa, se desmarca; cuando llega, se vuelve
  a marcar. El nombre y el precio se conservan.
- **El precio de cada parte no se publica.** Sirve para la cuenta que el panel
  muestra al lado: `botella + aditivo + hielo = S/ X · el combo cobra S/ Y menos`.
  Es lo que el dueño necesita saber al fijar un combo.
- **Lo apagado no viaja al navegador.** El catálogo manda el nombre vacío en vez
  de mandarlo y esconderlo con JavaScript.
- **Marcado sin nombre no se publica.** Un combo que anuncia algo sin decir qué
  termina en el mensaje de WhatsApp del cliente. Lo revisan el panel y el
  servidor.

La migración `ComboAditivoHielo` **enciende las casillas donde ya había un
nombre**. Con el `defaultValue: false` que EF genera solo, los 18 combos que ya
existen habrían dejado de decir qué incluyen sin que nadie se enterara — el mismo
error que casi se cuela con `ProductoActivo`.

## Seguridad del panel

- Login con ASP.NET Core Identity, bloqueo tras 5 intentos.
- Cookie solo por HTTPS, sesión de 8 horas.
- Token antiforgery por cabecera en el POST de publicar.
- El servidor **revalida** todo: precio mayor a cero y combo más caro que la
  botella. No se confía en lo que manda el navegador.
- Las imágenes se validan por firma de archivo, no por extensión. SVG queda
  fuera a propósito: puede llevar JavaScript y se serviría desde una URL de
  confianza.
- Cada cambio queda en `CambiosPrecio` con usuario y fecha.

**Después del primer arranque, borra `Admin__Password` de App Settings.** Ya
quedó el usuario creado; dejar la contraseña ahí no aporta nada.

Capa extra opcional: Access Restrictions por IP sobre `/panel`.

## Pendientes

1. **Dirección del local** → activa el JSON-LD y hace que Google muestre
   "Abierto 24 horas". Es lo de mayor retorno que falta.
2. Logo, favicon, `og.png` e íconos PWA oficiales (los actuales son placeholder).
3. Subir las fotos de producto desde el panel una vez desplegado. Las que están
   en `wwwroot/assets/productos` son de desarrollo; en Azure van al Blob.
4. GA4 y Meta Pixel: se guardan en la tabla `Tienda` y se cargan solo si tienen
   valor.
5. Confirmar con el dueño el costo real por distrito. Los S/ 10 uniformes son el
   punto de partida, no un precio acordado zona por zona.
6. Llevar el resto de `Tienda` al panel — teléfono, redes, promos, métodos de
   pago. Las zonas de reparto ya tienen su pantalla; lo demás sigue editándose
   por SQL.
