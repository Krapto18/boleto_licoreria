# Fotos de producto

## Lo que hay que pedirle al proveedor

| | |
|---|---|
| **Formato** | PNG o WebP **con fondo transparente** |
| **Tamaño** | 1000×1000 px o más, cuadrado |
| **Fondo** | Transparente. Si no se puede, blanco puro y parejo |
| **Encuadre** | Solo el producto, centrado, sin sombra proyectada |
| **Texto legal** | **Que no venga impreso en la foto** |

Del original conviene que sobre resolución: el script recorta y reescala, y
siempre es mejor achicar que agrandar.

## Lo que sale

`500×500 WebP` con transparencia, producto al 86 % del alto y todas las bases
apoyadas en la misma línea.

**Por qué 500 y no 1000.** Medido en el sitio: la foto se dibuja como máximo a
448×258 px reales en escritorio y 417×309 en un móvil de 390 px con densidad 3.
Un producto al 86 % de un lienzo de 500 son 430 px de alto — cubre el peor caso
con margen. Más allá de eso son bytes que el cliente descarga y nunca ve.

**Por qué WebP.** Sobre estas 64 fotos pesa un 63 % menos que PNG con la misma
calidad. El script prueba tres codificaciones por imagen y se queda con el
archivo más chico; cuál gana depende de la foto y no se puede decidir de
antemano.

**Por qué transparente.** La tarjeta tiene un degradado radial de blanco a
crema. Una foto con fondo blanco recorta un cuadrado visible encima de ese
degradado.

**Por qué sin texto legal.** Ya está en el pie de la web, que es donde se lee y
donde cumple. Impreso en la foto queda ilegible —la imagen se muestra en una
caja de 150 px de alto— y le come hasta una cuarta parte del ancho al producto,
que es lo único que el cliente mira para decidir.

## Uso

```bash
pip install pillow numpy

# Ver qué haría, sin escribir nada
python tools/preparar_fotos.py fotos-nuevas/ --revisar

# Convertir
python tools/preparar_fotos.py fotos-nuevas/ -s salida/
```

Nunca escribe sobre la carpeta de entrada. Revisa el resultado antes de
reemplazar nada.

### Ajustes

| Opción | Por defecto | Para qué |
|---|---|---|
| `--lienzo` | 500 | Lado del cuadrado de salida |
| `--ocupacion` | 0.86 | Cuánto del alto ocupa el producto |
| `--pie` | 0.05 | Margen bajo la base |
| `--tolerancia` | 18 | Cuánto puede alejarse del blanco y seguir siendo fondo |
| `--umbral-legal` | 0.15 | Masa mínima de un tramo para no descartarlo |
| `--hueco` | 12 | Píxeles vacíos que separan dos tramos |
| `--max-upscale` | 1.15 | Cuánto se permite agrandar una foto chica |

## Cómo decide qué es texto legal

Proyecta la opacidad sobre los dos ejes y busca tramos separados por espacio
vacío. El texto legal queda como un tramo aparte, fino y de poca masa; el
producto concentra casi toda. Se descarta lo que no llegue al 15 % del tramo
mayor.

Se miran **los dos ejes** porque unos proveedores lo ponen vertical al costado y
otros horizontal bajo el producto. Mirando solo las columnas se escapaba la
mitad de los casos.

Un pack de dos botellas también son dos tramos, pero de masa parecida: se
conserva entero. Por eso el criterio es la masa y no la posición.

## Detalles que no son obvios

**El fondo se quita por inundación desde los bordes,** no borrando "todo lo
blanco". Una etiqueta blanca dentro de la botella se conserva porque no toca el
borde. Si la foto ya trae transparencia, no se toca.

**No se agranda más de 1.15×.** Agrandar inventa detalle que no existe: se ve
blando y además pesa más, porque la interpolación reemplaza zonas planas por
tonos intermedios. Una lata fotografiada chica queda un poco más chica — y la
presentación va escrita en la tarjeta, así que la información no se pierde.

**Un reescalado de menos del 8 % no se aplica.** No se nota en pantalla pero sí
en el archivo: en las pruebas, ajustes de 6 % duplicaban el peso porque los
halos de la interpolación arruinan la compresión. Con esta salvedad, las mismas
64 fotos pasaron de pesar más que el original a pesar un 66 % menos.

## Lo que el script no puede arreglar

Si el texto legal está **superpuesto sobre el producto** —impreso encima de las
latas, no en una franja aparte— no hay forma de quitarlo sin borrar parte del
producto. Esa foto hay que pedirla limpia.
