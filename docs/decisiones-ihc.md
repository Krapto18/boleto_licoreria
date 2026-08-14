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

---
---

# Nivel 6 — pedidos del cliente

Este bloque es distinto a los anteriores. No sale de una auditoría: sale de tres
cambios que **pidió el dueño del negocio** y que en parte van en contra de lo que
recomienda la heurística. Están hechos igual —es su web y es su decisión— y lo
que se documenta acá es qué cuesta cada uno y qué se hizo para que costara lo
menos posible.

## 32. Dos carruseles de cinco, uno debajo del otro

Lo pedido: dos carruseles de cinco banners en lugar de uno, **el segundo debajo
del primero**.

Lo que cuesta, medido en un móvil de 390 px: cada banner de 1200×500 ocupa 146 px
de alto más 44 de puntos. Los dos carruseles llenos meten **380 px entre el hero
y el catálogo**, y el catálogo pasa a estar a dos pantallas y media de scroll
desde arriba. El cliente entra a comprar y lo primero que encuentra son avisos.

Se propuso separarlos —uno antes del catálogo y otro después— y el cliente los
quiere juntos. Van juntos.

Lo que se hace para amortiguarlo:

| | |
|---|---|
| **Carga diferida** | Solo el primer banner del primer carrusel se pide con prioridad. Los otros nueve son `loading="lazy"`: no compiten con lo que hay que ver primero |
| **El menú salta los dos** | El enlace "Catálogo" lleva directo a la grilla por encima de los banners. Es la salida rápida del que vino a comprar, y se comprueba en la prueba |
| **Ninguno aparece vacío** | Si el dueño solo llena el de arriba, el de abajo no existe. Sin banners no hay hueco ni puntos |

**Detalle que ya estaba mal y salió acá:** los puntos del carrusel se marcaban
con un `querySelectorAll('.punto')` global. Con una sola pista funcionaba; con
dos, desplazar la de abajo habría marcado los puntos de la de arriba. Ahora cada
carrusel busca los suyos.

**Detalle que ya estaba mal y salió acá:** los puntos del carrusel se marcaban
con un `querySelectorAll('.punto')` global. Con una sola pista funcionaba; con
dos, desplazar la de abajo habría marcado los puntos de la de arriba. Ahora cada
carrusel busca los suyos.

**Los puntos ahora se tocan.** Medían 9×9 px — la mitad de la mitad de lo que
pide la WCAG 2.5.5. Se separó lo que se dibuja de lo que se toca: el botón mide
44×44 y el círculo de 9 px es un pseudoelemento adentro. Agrandar el círculo
habría convertido los puntos en botones y le habrían competido la atención al
banner, que es lo que hay que mirar. Nunca falló en la prueba de objetivos
táctiles porque sin banners cargados el carrusel no existe: habría aparecido
recién el día que el dueño subiera el primero.

## 33. El buscador en lugar de "Abierto ahora"

El sello del nav era el punto 11: el estado del sistema como propuesta de valor,
Nielsen #1. Se va.

Lo que se pierde es menos de lo que parece, porque el 24/7 se dice en otros tres
sitios que no se tocaron: la franja roja de arriba, el titular del hero y el
párrafo con **la hora del propio cliente** ("Son las 8:52 a. m. y estamos
atendiendo"), que es el que de verdad lo hace verificable. El sello era el
recordatorio, no la prueba.

Lo que se gana es real: 55 productos con un solo filtro por categoría. Un
buscador en la barra fija es el atajo que la Ley de Hick pide cuando la lista es
larga.

**Hay dos campos de búsqueda y es a propósito.** El del nav no reemplaza al del
catálogo porque en móvil el nav se pliega, y nadie debería tener que abrir un
menú para buscar. Son dos vistas del mismo estado: se copian el texto entre sí y
filtran la misma grilla. Si dijeran cosas distintas, el cliente vería resultados
filtrados por algo que no está escrito en el campo que tiene delante — Nielsen #4
roto de la peor manera, en silencio.

**Buscar desde el nav trae el catálogo a la pantalla.** Sin eso, escribir en la
barra fija mientras se mira el hero es teclear a ciegas: el filtro corre y no se
ve nada. Solo se desplaza si el catálogo no está ya a la vista y solo con algo
escrito.

## 34. El menú hamburguesa en móvil

Esto sí es una pérdida y conviene decirlo sin adornos. Esconder la navegación
detrás de un icono contradice Nielsen #6 —reconocer en vez de recordar—: lo que
está a la vista se usa; lo que hay que ir a buscar, no. Es una decisión del
cliente y se hizo.

Lo que se cuidó para que costara lo menos posible:

| | |
|---|---|
| **Se anuncia** | `aria-expanded` en el botón, no una clase suelta. El lector de pantalla dice si está abierto |
| **Se sale** | Escape lo cierra y devuelve el foco al botón. Tocar fuera también (Nielsen #3) |
| **Se cierra al elegir** | Si no, el panel tapa justo la sección a la que acaba de saltar |
| **El foco entra** | Al abrir, el foco va a la primera opción: quien navega con teclado no queda tabulando a ciegas |
| **44 px** | El botón mide 44×44 exactos; las opciones del panel, 52 de alto |
| **El icono dice el estado** | Las tres barras se vuelven una X. El mismo control cierra, sin agregar un segundo botón |
| **Opaco** | Fondo sólido, no traslúcido como el nav: detrás pasa el titular del hero y con transparencia el contraste deja de ser el calculado |

**Lo que NO se escondió:** el buscador del catálogo sigue a la vista en móvil sin
abrir nada, y el botón flotante de WhatsApp tampoco está en el menú. El acceso a
comprar y el acceso a escribir no dependen de que el cliente descubra la
hamburguesa.

**Promoción no está en el menú de móvil.** El pedido fue "solo el logo y
catálogo". La sección sigue existiendo y la franja roja la anuncia en todas las
pantallas; devolverla al menú es quitar una clase.

El panel no es un modal y no atrapa el foco: es un menú desplegable y tabular
fuera de él es una salida legítima, no un escape.

## 35. El logo en lugar del titular

Lo pedido: que el hero muestre el logo del negocio en vez de "A la hora que sea".

Lo que cuesta. Un titular dice a qué vino uno; un logotipo dice quién eres. El
visitante que llega de una búsqueda y ve un logo tiene que deducir el resto. Y
si el `<h1>` pasa a ser una imagen, la página se queda sin encabezado de texto:
Google lee el encabezado para entender de qué trata, y un lector de pantalla lo
usa para orientar a quien no ve la imagen. Es decisión del cliente y está hecho.

Lo que se hace para que no se pierda nada de eso:

| | |
|---|---|
| **Sigue siendo el `<h1>`** | El logo va dentro del encabezado, no lo reemplaza. El documento conserva su estructura y sigue habiendo uno solo |
| **El `alt` carga el mensaje** | "Boleto Licorería · licorería abierta las 24 horas, todos los días". Es lo que leen Google y el lector de pantalla, y es la frase que antes llevaba el titular |
| **El párrafo lo repite en pantalla** | "Tienda y WhatsApp abiertos las 24 horas… Son las 9:29 a. m. y estamos atendiendo". Quien sí ve la imagen tampoco se queda sin saber a qué llegó |
| **Si no carga, vuelve el texto** | Un `onerror` devuelve el titular "A la hora que sea". Un `<h1>` con una imagen rota es un `<h1>` vacío |

**El logo trae su propio fondo.** Es azul marino `#000828` con letras crema — el
mismo azul del sitio, con dos puntos de diferencia. Sobre la chapa crema del hero
queda como una placa, y así es como se lee: recortarle el fondo dejaría letras
crema sobre crema, invisibles. La placa lleva las mismas esquinas redondeadas que
el resto para que se vea decidida y no pegada.

**Pesaba 194 KB.** El `logo.svg` era un PNG en base64 dentro de un SVG, y el
base64 infla un tercio. Convertido a WebP sin pérdida: **62 KB**, un 68 % menos,
sin tocar un solo píxel. Importa porque es el elemento más grande del hero — es
el que mide el LCP, y esta web se abre desde datos móviles a las tres de la
mañana. Lleva `width`, `height` y `fetchpriority="high"`: se pide temprano y
reserva su espacio antes de cargar, así el resto del hero no salta.

## 36. Qué se verifica solo de todo esto

28 comprobaciones nuevas en `tests/flujo-pedido.js`, sobre las 40 que ya había:

- El sello ya no está y el buscador ocupa su lugar
- Escribir en el nav filtra la grilla y el otro campo repite el texto; borrar en
  uno limpia el otro
- En escritorio no hay hamburguesa y el menú sigue desplegado
- En móvil: el botón mide 44, el menú arranca plegado y lo dice, abre, el foco
  entra, Escape cierra y devuelve el foco, elegir cierra
- El buscador del catálogo se ve sin abrir el menú
- El logo del hero es el `<h1>`, es el único, su `alt` menciona las 24 horas y la
  imagen carga de verdad — si fallara, el encabezado quedaría vacío y nadie se
  enteraría
- Cada banner cae en su carrusel, el segundo va debajo del primero, cada uno
  tiene sus puntos, mover uno no marca los del otro, los puntos se tocan a 44 px
  y el enlace "Catálogo" salta por encima de los dos
- Ninguna pieza de marca queda rota, el isotipo está en el nav y en el pie, los
  tres iconos están y cada uno recorta su silueta con la máscara del SVG

Los carruseles se prueban **inyectando banners en la respuesta**, porque la base
todavía no tiene ninguno cargado. Sin eso, la función quedaría sin probar hasta
que el dueño subiera el primero — que es tarde para enterarse de que algo no
funciona.

## 37. El kit de marca

Llegó el kit oficial: logos en cinco variantes de color, isotipo, tres iconos y
el manual. Hasta entonces la web funcionaba con placeholders — un sello con la
letra "B" y un logo que era un PNG metido en un SVG.

**Qué variante va en cada sitio, y por qué importa.** El kit trae el logo en azul
y en marfil porque están pensados para fondos opuestos. La chapa del hero es
crema: ahí va el azul. Si se pusiera el marfil se leerían letras crema sobre
crema. Es la misma razón por la que el modal de verificación de edad cambió: traía
el archivo con fondo propio y dibujaba un rectángulo oscuro dentro de una tarjeta
clara.

**Los iconos se pintan como máscara, no como imagen.** Un `<img>` no puede
heredar el color del texto que lo acompaña; una máscara sí. Con un solo archivo,
el carrito sale crema en la barra de pedido y el acompañante sale azul sobre la
chapa de la promoción. La regla va dentro de un `@supports`: si el navegador no
soporta máscaras el icono no se dibuja, en vez de dejar un cuadrado de color
donde debería haber una silueta.

**Van donde significan algo.** El carrito en el resumen del pedido, el hielo en
la sección que pregunta si se acabó el hielo a las 3 a.m., el acompañante en la
promoción de la gaseosa gratis. Tres iconos, una instancia cada uno. Repartirlos
por decoración los habría convertido en ruido y habría competido con lo único
que tiene que destacar, que es el verde de WhatsApp (punto 3).

**Al logo se le ciñó el `viewBox`.** El arte mide 909×624 dentro de un lienzo de
1046×1030 — el 40 % del alto es vacío que el exportador dejó. En el hero eso
salía como un bache entre el logo y la filigrana. Ceñirlo al arte con un 1 % de
margen lo arregla sin tocar el dibujo.

**La paleta pasó a la del manual**, leída de los propios SVG: azul `#000625` y
rojo `#CF2026`, contra los `#000725` y `#C8102E` que se habían elegido a ojo
antes de que hubiera manual. Medido antes de aplicarlo: el rojo de marca sobre
crema da **4.80:1**, por encima del 4.5 de AA — más justo que el 5.22 anterior,
pero cumple, y es el color de la marca.

**El crema se mantiene.** El marfil del manual es `#FEFCEC`, casi blanco.
Aplicarlo aclararía toda la chapa del hero y las tarjetas de promoción, que es un
cambio de aspecto que nadie pidió. Queda anotado por si el cliente lo prefiere:
es una variable.

**El service worker subió a `boleto-v2`.** Un service worker sirve de su caché
antes de mirar la red: sin subir la versión, quien ya había entrado seguiría
viendo el logo viejo hasta vaciar el navegador. Es el tipo de detalle que no
falla en desarrollo —donde nadie tiene caché— y falla para todos los demás.
