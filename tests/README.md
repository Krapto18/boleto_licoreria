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

**Flujo de pedido** (escritorio y móvil):

- La vista previa se abre sola al agregar el primer producto
- Tocar el selector de distrito **no** cierra la vista previa
- Tocar la burbuja **sí** la cierra (el gesto original sigue vivo)
- El costo del distrito entra en el total
- El mensaje desglosa subtotal y delivery, y el botón apunta a `wa.me`
- Pedido y distrito se recuperan tras recargar
- El service worker queda registrado
- Cero errores de JavaScript en consola

**Accesibilidad.** El documento de decisiones afirmaba tres cosas que al
medirlas resultaron falsas. Ahora se comprueban solas:

- Todo objetivo táctil llega a 44 px, descartando lo invisible o inerte
  y contando el área del `<label>` contenedor
- Foco visible en todos los controles del pedido, con Tab real para que
  `:focus-visible` se active como con un teclado de verdad
- El foco no se escapa de la verificación de edad en 10 tabulaciones
  (Ley N° 28681)
- Sin distrito elegido: el selector se ve sin desplegar nada, se marca
  como pendiente y el mensaje avisa que falta sumar el delivery
- `--bar-h` reserva la altura real de la barra y el pie de página no
  queda debajo de ella

**Regla de color.** El verde `--wa` se reserva para lo que abre WhatsApp:
es la única conversión que importa, y Von Restorff solo funciona si ese
color no aparece en ningún otro lado. La prueba recorre los estados por
los que pasa el cliente y falla si aparece verde en algo que no lleve a
`wa.me`, si algún estado se queda sin llamado a la vista, o si el texto
del botón baja de 4.5:1 de contraste.

**Navegación.** El sello "Abierto ahora" dejó su sitio a un buscador y en
móvil el menú se pliega. Esconder navegación cuesta, así que se comprueba
que lo plegado funcione: que el botón diga si está abierto, que Escape lo
cierre y devuelva el foco, que elegir una opción lo cierre, que el foco
entre al abrir y que el botón mida 44 px. Y que el buscador del catálogo
siga a la vista en móvil **sin** abrir el menú. Los dos campos de búsqueda
se comprueban sincronizados en los dos sentidos.

**El logo del hero.** Reemplazó al titular, así que se comprueba que siga
siendo el `<h1>`, que sea el único, que su texto alternativo mencione las
24 horas y que la imagen **cargue de verdad**. Si fallara, el encabezado
de la página quedaría vacío y nadie se enteraría desde fuera.

**Los dos carruseles.** Cada banner cae en el suyo, el segundo va debajo
del primero, cada carrusel tiene sus propios puntos, mover uno no marca
los del otro, los puntos se tocan a 44 px aunque se dibujen a 9, y el
enlace "Catálogo" sigue saltando por encima de los dos. Los banners se
**inyectan en la respuesta** porque la base todavía no tiene ninguno: sin
eso la función quedaría sin probar hasta que el dueño suba el primero, que
es tarde para enterarse.

Detalle de cada corrección en `docs/decisiones-ihc.md`, niveles 5 y 6.

## Antes de correrla

La base necesita el seed aplicado: productos con stock y zonas de reparto
cargadas. Si falta alguno, la prueba lo dice en vez de fallar de forma
confusa.

Deja el pedido guardado en el `localStorage` de un perfil temporal de
navegador, así que no ensucia nada. Sí escribe en la base **solo si** se
corre contra un entorno donde se publiquen cambios — esta prueba no
publica: solo lee y arma el pedido del lado del cliente.
