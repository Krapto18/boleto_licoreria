# Prueba del flujo de pedido

Un solo archivo, un solo comando. Cubre lo único que la web tiene que
lograr: que el cliente arme su pedido, elija su distrito y el mensaje de
WhatsApp salga con el total correcto.

```bash
# En una terminal: la app
dotnet run --project src/Boleto.Web --no-launch-profile --urls http://localhost:5199

# En otra: la prueba
cd tests
npm install
npm test
```

Otro puerto: `BOLETO_URL=http://localhost:5000 npm test`

Usa el Chrome del sistema si está instalado; si no, baja el Chromium de
Playwright. Corre el flujo completo en viewport de escritorio y de móvil
con touch.

## Por qué existe

Dos defectos reales pasaron una verificación hecha solo con peticiones al
servidor:

1. **El selector de distrito cerraba la vista previa al tocarlo.** El
   `<select>` vive dentro de `.preview`, que se cierra con cualquier clic.
   El distrito quedaba inseleccionable.

2. **`animarEntrada()` no existía.** La llamada seguía en el arranque de
   `app.js` y el `ReferenceError` abortaba todo lo que venía debajo —
   incluido `serviceWorker.register()`. La PWA nunca se instalaba y el
   sitio no abría sin señal, que es justo lo que promete.

Ninguno de los dos se ve con `curl`. Los dos habrían salido al primer
`npm test`.

## Qué cubre

- La vista previa se abre sola al agregar el primer producto
- Tocar el selector de distrito **no** cierra la vista previa
- Tocar la burbuja **sí** la cierra (el gesto original sigue vivo)
- El costo del distrito entra en el total
- El mensaje desglosa subtotal y delivery, y el botón apunta a `wa.me`
- Pedido y distrito se recuperan tras recargar
- El service worker queda registrado
- Cero errores de JavaScript en consola

## Antes de correrla

La base necesita el seed aplicado: productos con stock y zonas de reparto
cargadas. Si falta alguno, la prueba lo dice en vez de fallar de forma
confusa.

Deja el pedido guardado en el `localStorage` de un perfil temporal de
navegador, así que no ensucia nada. Sí escribe en la base **solo si** se
corre contra un entorno donde se publiquen cambios — esta prueba no
publica: solo lee y arma el pedido del lado del cliente.
