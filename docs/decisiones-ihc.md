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
