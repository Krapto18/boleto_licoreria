# Decisiones de diseño — Boleto Licorería

Cada decisión de esta landing responde a un principio de Interacción
Humano-Computadora. No es decoración: la página tiene **una sola tarea**
y todo lo demás se subordina a ella.

**Tarea única:** que el usuario presione WhatsApp con su pedido armado.
**Diferenciador:** la tienda atiende 24/7.

---

## 1. Ley de Fitts — el destino principal

> `T = a + b · log₂(2D/W)`
> El tiempo para alcanzar un objetivo crece con la distancia y baja con el tamaño.

| Decisión | Aplicación |
|---|---|
| Barra de pedido pegada al borde inferior | El borde de pantalla es un objetivo de **ancho infinito**: el dedo no puede pasarse de largo. |
| Botón "Enviar pedido" de 58 px de alto | Por encima del mínimo de 48 px (WCAG 2.5.5). |
| Botón flotante de 62 px, esquina inferior derecha | Zona natural del pulgar en móvil (Hoober). |
| En móvil el CTA ocupa el ancho completo | Maximiza `W`, minimiza `T`. |
| `+` / `−` de cantidad a 44 px | Evita el error de tocar el botón contiguo. |

Los controles secundarios (buscador, filtros) quedan arriba, lejos del pulgar,
**a propósito**: no compiten con la acción principal.

---

## 2. Ley de Hick — reducir la decisión

> `T = b · log₂(n+1)`
> Cada opción extra retrasa la elección.

- **De 10 categorías a 7 grupos.** Vodka, gin y licores se fusionaron. La
  categoría fina sigue visible en cada tarjeta, así que no se pierde información,
  solo se reduce la carga de la decisión inicial.
- **Un solo CTA primario por pantalla.** El hero antes tenía dos botones del
  mismo peso; ahora hay uno verde y debajo un enlace de texto discreto.
- **Se eliminaron las secciones de horarios, zonas y pagos.** Además de no estar
  confirmadas, eran tres bloques de lectura entre el usuario y el pedido.

---

## 3. Efecto Von Restorff (aislamiento) — el foco

Lo que se distingue del resto es lo que se recuerda y lo que se toca.

**Regla de color del proyecto: el verde `--wa` se usa únicamente en elementos
que abren WhatsApp.** Ningún otro botón, borde ni texto puede ser verde.

Antes había más de 60 elementos clicables compitiendo. Ahora la jerarquía es:

1. **Verde** → abre WhatsApp. Cuatro instancias en toda la página.
2. **Azul marino** → agregar al pedido (acción intermedia).
3. **Rojo** → marca y filtros activos.
4. **Texto subrayado** → navegación secundaria.

Si el verde apareciera en cinco sitios distintos, dejaría de significar algo.

---

## 4. Nielsen #1 — visibilidad del estado del sistema

El diferenciador es "estamos 24/7", así que el estado del sistema **es** la
propuesta de valor.

- Píldora verde permanente en el nav: `● Abierto ahora — 3:42 a.m.`
- El reloj se actualiza cada 30 segundos con la hora real del dispositivo.

El reloj no es adorno: convierte una afirmación en algo **verificable**. El
usuario ve la hora en la que efectivamente está mirando la página y la web le
confirma que hay alguien atendiendo. Una tienda cerrada no puede imitar eso.

La versión anterior calculaba un horario y a las 3 a.m. mostraba "Cerrado" —
exactamente en el momento de mayor ventaja competitiva del negocio.

---

## 5. Nielsen #6 — reconocer antes que recordar

La barra inferior no muestra solo un total: muestra **el mensaje literal que se
va a enviar**, en una burbuja con el aspecto de WhatsApp.

El usuario no tiene que recordar qué agregó ni confiar en un número abstracto.
Ve el artefacto real antes de enviarlo. La vista previa se abre sola la primera
vez que agrega algo, porque ese es el momento en que se entiende cómo funciona
todo el sitio.

---

## 6. Nielsen #3 — control y libertad del usuario

- Cada acción muestra un aviso con **Deshacer** durante 4 segundos.
- No hay diálogos de confirmación: se permite el error y se ofrece la salida.
- El selector botella/combo se puede cambiar cuantas veces se quiera antes de agregar.

---

## 7. Nielsen #2 — correspondencia con el mundo real

- Los precios se muestran en **chapas circulares**, igual que en el catálogo
  impreso del cliente. El usuario que ya vio el PDF reconoce el patrón.
- El vocabulario es el de la tienda: "combo", "six pack", "solo botella".
- El pedido se expresa en el formato en que un cliente peruano ya escribe por
  WhatsApp, no como un carrito de e-commerce.

---

## 8. Ley de Jakob

> Los usuarios pasan la mayor parte del tiempo en otros sitios.

La barra inferior con total y botón de acción replica el patrón de Rappi y
PedidosYa. No se inventó una interacción nueva: se reutilizó el modelo mental
que el usuario ya tiene instalado.

---

## 9. Carga cognitiva y agrupamiento

- El resumen del pedido y el botón de desplegar son **un solo control**, no dos
  objetos separados.
- La tarjeta agrupa la información en el orden en que se necesita:
  categoría → nombre → presentación → precio → decisión → acción.
- El contador de resultados ("38 productos") da retroalimentación inmediata al
  filtrar, sin que el usuario tenga que contar.

---

## 10. Accesibilidad

- Contraste: crema sobre marino ≈ 14:1; marino sobre crema ≈ 14:1. Supera AAA.
- Todos los objetivos táctiles ≥ 44 px.
- Foco visible en rojo con `outline-offset`, nunca suprimido.
- `aria-pressed` en filtros y selectores, `aria-expanded` en la vista previa,
  `role="status"` en el contador y en el aviso de deshacer.
- Enlace "Ir al catálogo" para salto de teclado.
- `prefers-reduced-motion` respetado: se anulan animaciones y scroll suave.

---

## Lo que se eliminó y por qué

| Elemento | Motivo |
|---|---|
| Horario detallado | Inventado, y además **contradecía** el 24/7. Un horario hace ver el servicio como condicional. |
| Zonas y costos de delivery | Inventados. Publicarlos es comprometer al cliente con precios que no acordó. |
| Formas de pago | Inventadas. |
| "Delivery en 30 minutos" | Inventado. Prometer un tiempo que no se controla genera reclamos. |
| Segundo CTA en el hero | Competía con el botón de WhatsApp (Hick + Von Restorff). |
| Botón "Vaciar" en la barra | Reemplazado por Deshacer, que es reversible. |

**Criterio general:** si el dato no está confirmado por el cliente, no aparece en
la web. Una landing que promete lo que la tienda no cumple genera reclamos que
el dueño va a atribuir al sitio.

---

## Pendiente de confirmar con el cliente

1. ¿El 24/7 es de la tienda o solo del WhatsApp?
2. Dirección exacta del local.
3. Distritos de reparto y costo de cada uno.
4. Formas de pago aceptadas.
5. Tiempo real promedio de entrega.

Los cinco están comentados en `js/data.js` y no se publican hasta tenerlos.

---
---

# Nivel 3 — decisiones añadidas

## 11. El 24/7 confirmado como estado del sistema

Tienda física **y** WhatsApp atienden 24 horas, los 7 días. Confirmado por el
cliente, así que ahora se afirma sin matices:

- Titular: "A la hora que sea".
- Subtítulo con reloj vivo: "Son las 3:42 a.m. y estamos atendiendo".
- Franja: "No cerramos. Ni feriados."
- Cierre: "¿Se acabó el hielo a las 3 de la mañana?"

Ya no existe ninguna función de cálculo de horario en el código. No hay forma de
que la web diga "Cerrado": sería mentir sobre la única ventaja real del negocio.

**JSON-LD `LiquorStore`** con `opens: 00:00 / closes: 23:59` los siete días.
Es lo que hace que Google muestre **"Abierto 24 horas"** en el mapa. Se genera
solo si `CONFIG.direccion` tiene valor — misma regla de no inventar datos.

## 12. Stock — Nielsen #1 y #5

Un producto agotado **no se oculta**: se muestra en gris, con la etiqueta
"Agotado", el precio atenuado y sin controles de cantidad.

- **Visibilidad (#1):** el usuario sabe qué pasa, no le desaparecen productos.
- **Prevención de errores (#5):** no se puede agregar al pedido algo que no hay.
  El error se hace imposible en vez de corregirse después.
- **Salida productiva:** el botón pasa a "Avísame cuando llegue", que abre
  WhatsApp con el mensaje armado. Un callejón sin salida se convierte en contacto.
- Los agotados se ordenan **al final** de la grilla. No se elimina información,
  se prioriza lo accionable.
- Si un producto del pedido guardado se agota, se cae solo al recuperar.

## 13. Persistencia del pedido

El pedido se guarda en `localStorage`. Si el usuario cierra y vuelve, sigue ahí y
se le avisa. Reduce la carga de reconstruir el trabajo perdido; en una compra
nocturna, con interrupciones constantes, esto es la diferencia entre el pedido y
el abandono.

## 14. Instalación en pantalla de inicio (PWA)

Para una tienda 24/7, un ícono en la pantalla de inicio es el atajo de las 3 a.m.
El aviso aparece a los 8 segundos, no de entrada, y se puede rechazar para
siempre — no se vuelve a mostrar.

El service worker cachea el catálogo: **con mala señal la web igual abre**.
El panel se excluye del caché a propósito: los precios deben venir frescos.

## 15. El panel — IHC aplicado a una herramienta

El panel no es una landing. Cambia el criterio: acá manda la eficiencia, no la
seducción. Se asume que el dueño lo usa **desde el celular, en la tienda**.

| Principio | Aplicación |
|---|---|
| **Nielsen #1 · visibilidad** | Píldora ámbar permanente: "3 sin publicar". Nunca se puede creer que la web ya cambió. |
| **Nielsen #6 · reconocer** | Bajo cada precio editado aparece "antes 45.00". No hay que recordar el valor anterior. |
| **Nielsen #3 · control** | Deshacer en cambios de stock y en ajustes masivos. |
| **Nielsen #5 · prevención** | El combo no puede costar menos que la botella: se marca en rojo y bloquea la exportación. Aviso del navegador si intenta cerrar con cambios. |
| **Nielsen #9 · errores** | El error se señala en el campo exacto, no en un mensaje global. |
| **Fitts** | Inputs de 44 px, switches de 64×34, barra de acciones en el borde inferior. |
| **Confirmación solo donde duele** | "Descargar" es directo; "Descartar" pide confirmación porque es irreversible. |
| **Resumen antes de publicar** | Lista de cambios con precio anterior y nuevo. Se revisa antes de exportar, no después. |

**Ajuste masivo (+1% / −1%)** sobre el filtro activo: cuando sube el precio del
proveedor, el dueño no edita 55 campos uno por uno. Respeta el filtro, así que
puede subir solo whiskys.

## 16. Medición

El clic en WhatsApp se dispara como evento con **origen** (hero, cierre,
flotante, footer, pedido armado, aviso de stock), monto y cantidad de productos.

Eso permite responder la pregunta que justifica el precio del trabajo: no solo
*cuántos* pedidos trajo la web, sino **desde qué parte de la página** salieron y
por cuánto. GA4 y Meta Pixel se cargan solo si tienen ID configurado.

## 17. Límite honesto del panel

El panel edita un borrador local y **exporta `js/data.js`**. No publica solo,
porque no hay servidor: el sitio es estático.

Esto está dicho explícitamente en la interfaz —"todavía no están en la web"— en
vez de dejar que el dueño asuma que ya cambió. Prometer publicación automática
sin backend sería el peor error de diseño posible: pérdida de confianza en la
herramienta.

El siguiente escalón real es un backend (Supabase o una función en Azure) para
que el panel publique directo. Es otro proyecto y otro presupuesto.

---
---

# Nivel 4 — decisiones añadidas

## 18. Por qué NO hay efecto vidrio (liquid glass) en las tarjetas

Se evaluó y se descartó. El motivo no es estético: rompe tres cosas medibles.

**Contraste.** La tarjeta es `--crema` sólido con texto `--tx-oscuro`: el 14:1
documentado en el punto 10. El vidrio sustituye ese fondo por translucidez, así
que el contraste del nombre y del precio pasa a depender de lo que quede detrás
—y **cambia con el scroll**. Un 14:1 fijo se convierte en una variable. Es WCAG
1.4.3 incumplido de forma intermitente, que es la peor manera de incumplirlo:
no se detecta en una captura de pantalla.

**Rendimiento.** `backdrop-filter` obliga al compositor a re-samplear el fondo en
cada frame. Con el filtro en "Todo" son 55 tarjetas a la vez. El público de una
licorería de barrio a las 3 a.m. no está en un tope de gama.

> El proyecto ya usa `backdrop-filter` en el nav, la barra de pedido y la
> pantalla de edad. Son elementos **únicos**. Uno es gratis; 55 repetidos en un
> grid, no. Por eso no sirven como precedente.

**Von Restorff.** Meter atractivo visual en 55 elementos que compiten con el
único CTA que importa va en contra de toda la sección 3.

**Y Nielsen #2:** la tarjeta imita el cartel impreso del cliente. El vidrio
esmerilado es lenguaje de sistema operativo, no de cartelería de licorería.

**Lo que sí se puso:** un barrido de brillo en hover. Un gradiente que cruza la
tarjeta — `transform` y `opacity`, que la GPU compone barato — sin translucidez
bajo el texto. La misma sensación premium, cero costo de contraste.

## 19. La tarjeta crece al apuntarla — y por qué solo un 3%

`scale(1.03)`, no más. La tarjeta **contiene el botón "Agregar"**: al escalar, el
botón se desplaza. Si crece demasiado, el objetivo se aleja del cursor mientras
se apunta (Fitts al revés) y el borde puede salirse de debajo del puntero,
encendiendo y apagando el hover en bucle. A 1.03 el desplazamiento es de unos
3 px: se percibe, no estorba.

Va detrás de `@media (hover:hover) and (pointer:fine)`. En táctil el `:hover` se
queda pegado tras el tap: la tarjeta quedaría agrandada hasta tocar otra. El
`:hover` anterior no tenía esa protección — con el escalado el defecto habría
pasado de invisible a evidente.

`prefers-reduced-motion` tiene bloque propio. El global solo acorta la
transición, así que el salto de tamaño ocurriría igual, de golpe — justo lo que
molesta con sensibilidad vestibular. Ahí se anula el transform y el brillo, y
queda la sombra como única señal.

## 20. El costo de delivery entra antes de enviar, no después

El cliente elige su distrito en la vista previa del pedido. El costo se suma al
total y viaja desglosado en el mensaje —subtotal, delivery, total.

**Nielsen #1:** el cargo se ve antes de enviar. La alternativa es que el cliente
mande el pedido creyendo que cuesta S/ 80 y la tienda le responda "más S/ 10 de
envío". Eso no es un detalle de precio: es la primera respuesta de la tienda
convertida en una mala noticia.

**Nielsen #6:** el distrito elegido queda en `localStorage`. En la siguiente
visita ya está puesto.

**Salida productiva:** "Otro distrito" no bloquea nada — avisa que se coordina
por WhatsApp. Un distrito fuera de cobertura sigue siendo un contacto.

## 21. Hick sobre la lista de zonas

43 distritos al mismo precio son 43 filas que dicen lo mismo. La sección de la
landing los colapsa en una línea —"Reparto a 43 distritos de Lima Metropolitana ·
S/ 10"— y vuelve a la lista sola en cuanto el dueño diferencia algún precio.

La lista completa sigue estando en el selector del pedido, que es donde el
usuario de verdad la necesita: ahí busca **su** distrito, no compara los 43.

## 22. El panel de delivery asume 43 filas en un celular

- **Buscador arriba:** llegar a "Surco" sin recorrer los 43.
- **"Aplicar a todos" respeta el filtro activo,** igual que el ajuste de precios.
  Cuando sube la gasolina, el costo sube parejo; sin esto son 43 campos a mano y
  el dueño termina no actualizando ninguno.
- **Desmarcar no borra:** el distrito se atenúa y conserva su costo. Se puede
  reactivar sin recordar cuánto cobraba.
- **Fila compacta, sin tarjetas ni sombras.** Es una herramienta: manda la
  densidad.
- **Nielsen #9:** el costo inválido se marca en su propio campo, y Guardar avisa
  en vez de mandar datos malos al servidor.

El servidor revalida todo igual —negativos, duplicados, montos absurdos— porque
este número termina en el total que ve el cliente.

---
---

# Nivel 5 — auditoría y correcciones

Este bloque nace de medir lo que los niveles anteriores **afirmaban**. Tres
afirmaciones del punto 10 resultaron falsas al comprobarlas en un navegador.
Todas están corregidas y ahora se verifican solas en `tests/`.

## 23. El distrito sale de la vista previa

El selector vivía dentro de `.preview`, que arranca colapsada y se abre sola
**una sola vez por sesión**. Si el cliente la cerraba, el botón "Enviar pedido"
seguía activo y el mensaje salía sin delivery: total S/ 45.00 medido, sin línea
de envío.

Es exactamente el escenario que el punto 20 dice haber resuelto. El documento
describía una intención; el código no la garantizaba.

Ahora el selector es una fila propia de la barra de pedido, visible siempre que
haya algo en el carrito. Y mientras no se elija:

- La fila se tiñe de rojo tenue y la etiqueta dice **"Elige tu distrito"**.
- El mensaje lleva **"(falta sumar el delivery)"** junto al total.

**No se bloquea el botón de WhatsApp.** Nielsen #3: el control es del usuario.
Alguien puede querer preguntar antes de decidir su zona, y la tienda prefiere
ese mensaje incompleto a ningún mensaje. Se informa, no se impide.

## 24. Objetivos táctiles: el documento decía 44 px y no era verdad

Medido en móvil, con el área real del `<label>` contenedor y descartando lo
invisible o inerte:

| Elemento | Antes | Ahora |
|---|---|---|
| Filtros `.chip` | 42 px | 44 px |
| Cantidad `+` / `−` | 42 px | 44 px |
| Botella / Combo | 34 px | 44 px |
| Enlaces del nav | 21 px | 44 px |
| Teléfono del footer | 22 px | 44 px |
| "o mira el catálogo…" | 22 px | 44 px |
| Deshacer del aviso | 36 px | 44 px |
| Logo del nav | 40 px | 44 px |

En los enlaces de texto el área crece por `padding`, **no por tipografía**: si
"o mira el catálogo…" creciera de tamaño competiría con el CTA verde, y eso
rompería Hick y Von Restorff para arreglar Fitts.

**Falso positivo que conviene registrar:** el botón flotante mide 39 px cuando el
hero está a la vista, pero ahí tiene `opacity: 0` y `pointer-events: none`. No es
un objetivo pequeño: no es un objetivo. Una auditoría que no descarta lo inerte
inventa defectos.

## 25. Foco visible: dos supresiones sin reemplazo

El punto 10 decía "nunca suprimido". Había dos reglas que lo suprimían:

- `.search input { outline: none }` — incondicional, y ganaba por especificidad
  al `:focus-visible` global. El buscador no daba **ninguna** señal de foco.
- `.distrito select:focus { outline: none; border-color: rojo }` — cambiaba un
  borde de 1 px como única señal, en el control que define el total del pedido.

Las dos ahora usan `:focus-visible` con el outline de 3 px del resto del sitio.

## 26. La verificación de edad no contenía el foco

Tenía `aria-modal="true"` y bloqueo de Escape, pero **el foco salía en la segunda
tabulación**: con la pantalla de edad encima se llegaba al catálogo de atrás.

`aria-modal` es una promesa a la tecnología asistiva, no un mecanismo. Ahora hay
contención real, cíclica en ambos sentidos. Tratándose de la Ley N° 28681, la
promesa tenía que ser verdad y no solo un atributo.

## 27. Contraste del panel: la opacidad también cuenta

El distrito desmarcado usaba `opacity: .45` sobre un texto de 14.48:1. La
opacidad mezcla el color con el fondo: el contraste efectivo caía a **4.03:1**,
bajo el 4.5 de AA. A `.7` queda en **7.85:1** y se sigue leyendo como apagado.

Un ratio nominal alto no dice nada si algo lo atenúa después.

## 28. Por qué esto se verifica solo

Los defectos de los puntos 23 a 27 son invisibles desde el servidor: `curl`
devuelve el mismo HTML con y sin ellos. `tests/` los comprueba en un navegador
real, en escritorio y en móvil con touch, y falla si vuelven.

Una afirmación de accesibilidad que nadie mide se convierte en falsa sin que
nadie se entere.

## 29. La barra fija medía mal el espacio que ocupa

`--bar-h` es la altura que la barra de pedido le quita al contenido. La usan el
`padding-bottom` del body, el aviso de deshacer y el botón flotante. Estaba
escrita a mano como `82px` en JavaScript.

Al mover el distrito a la barra, la altura real pasó a 136 px. El número no se
enteró, y en móvil eso se veía así:

- El pie de página quedaba **debajo** de la barra: el texto legal de la Ley
  N° 28681 y el copyright no se podían leer.
- El aviso de deshacer se dibujaba **encima** de la barra en vez de sobre ella,
  tapando por completo la fila del distrito durante sus cuatro segundos. El
  control que define el total quedaba invisible justo después de agregar algo.

Ahora se mide: la barra completa menos la vista previa, redondeando hacia
arriba una sola vez. Sumar las filas por separado dejaba fuera el borde superior
de la barra y perdía un píxel; medir por diferencia sigue siendo correcto si
mañana se le agrega otra fila.

Se remide al girar el teléfono y al terminar de plegarse la vista previa: en
plena transición se descuenta una altura intermedia y la reserva sale holgada.

**Una constante que describe una medida del layout se vuelve mentira en cuanto
alguien toca el layout.** Se mide o no se pone.

De paso, en pantallas chicas: el total se partía en dos renglones —"S/" arriba,
"45.00" abajo— y la fila del distrito se compactó para no comerse la pantalla.

## 30. El panel decía siempre "Precios y stock"

El encabezado no cambiaba con la pestaña: estando en Delivery seguía anunciando
"Precios y stock" y explicando que los cambios "recién aparecen cuando le das a
Publicar" — un botón que en esa pantalla no existe.

Ahí se escondía una asimetría real: **solo Precios usa borrador.** Productos,
Delivery y Banners publican al guardar. Nunca se había dicho, así que el dueño
podía quedarse esperando un paso de publicación que ya había ocurrido, o creer
que un cambio seguía en borrador cuando ya estaba en la web.

Cada pestaña ahora dice qué es y cuándo sale a la web.

**Sobre el verde del panel:** el `accent-color` de las casillas de reparto usa
`--verde`, que ya marcaba el switch de stock (verde sí / rojo no). No contradice
la regla de Von Restorff del punto 3: esa reserva el verde para WhatsApp en la
**landing**, y el panel no comparte hoja de estilos ni tiene botones de WhatsApp.
Dentro del panel, verde significa activo, y significa lo mismo en los dos sitios
donde aparece.

## 31. La regla del verde, ahora verificada y no solo declarada

El punto 3 dice que el verde `--wa` se reserva para lo que abre WhatsApp. Era una
declaración en un comentario de CSS: nada impedía romperla.

Medido en el navegador, recorriendo cada elemento pintado en los estados por los
que pasa el cliente —hero, catálogo, con pedido, pie de página— en móvil y
escritorio:

| Estado | Verde en pantalla | Área |
|---|---|---|
| Hero, sin pedido | `#heroWa` | 17 096 px² |
| Catálogo, sin pedido | `#float` | 3 136 px² |
| Catálogo, con pedido | `#barWa` | 12 688 px² |
| Pie, con pedido | `#closeWa` + `#barWa` | 21 219 + 12 688 px² |

**Cero intrusos.** Todo lo verde abre WhatsApp, y en todos los estados hay al
menos un llamado a la vista. El único caso con dos verdes simultáneos es el
cierre, donde ambos van al mismo sitio: se refuerzan, no compiten.

El flotante desaparece cuando el hero está a la vista y cuando hay pedido
(`body[data-cart="on"]`), así que nunca hay dos CTA verdes distintos peleándose.
Esa lógica **estuvo rota** hasta el nivel 4: el `ReferenceError` de
`animarEntrada` impedía que corriera el observador, y el flotante se quedaba
encima del botón del hero de forma permanente.

Contraste del texto sobre el botón: **8.38:1**, muy por encima del 4.5 de AA.

Ahora hay una prueba que recorre esos estados y falla si aparece verde en algo
que no lleve a `wa.me`. Se comprobó pintando el botón "Agregar" de verde: la
prueba lo detectó en los tres botones visibles.

**Sobre el barrido de brillo del punto 18:** es blanco, dura 0,6 s, solo existe
con puntero fino y solo en la tarjeta apuntada. No compite con el verde, que es
un color saturado, permanente y siempre presente en pantalla. Un destello
momentáneo en un elemento no altera la jerarquía de color del punto 3.
