#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
Normaliza las fotos de producto para el catálogo.

Las fotos llegan de proveedores distintos y traen fondo blanco, texto legal
impreso en un costado y la botella a cualquier escala. En una grilla de dos a
cuatro columnas eso se nota: unas botellas se ven grandes, otras diminutas, y
ninguna apoya a la misma altura.

Este script deja todas iguales:

  1. Quita el fondo blanco por inundación desde los bordes. No borra "todo lo
     blanco": una etiqueta blanca dentro de la botella se conserva porque no
     toca el borde.
  2. Descarta la franja de texto legal. Se detecta por proyección de columnas:
     el texto queda como un bloque aparte, separado por espacio vacío y con
     mucha menos masa que el producto. Un pack de dos botellas tiene dos
     bloques de masa parecida y se conserva entero.
  3. Recorta al contenido y escala para que el producto ocupe siempre la misma
     fracción del alto.
  4. Centra en horizontal y apoya todas las bases en la misma línea.
  5. Guarda en WebP probando sin pérdida y con pérdida, y se queda con el
     archivo más chico.

El texto legal no se pierde: vive en el pie de la web, donde se lee. Impreso en
la foto queda ilegible a 150 px de alto y le come una cuarta parte del ancho al
producto.

Uso:
    python tools/preparar_fotos.py entrada/ -s src/Boleto.Web/wwwroot/assets/productos
    python tools/preparar_fotos.py entrada/ --revisar        # no escribe nada
"""
from __future__ import annotations

import argparse
import io
import os
import sys
from collections import deque
from concurrent.futures import ProcessPoolExecutor
from pathlib import Path

try:
    import numpy as np
    from PIL import Image
except ImportError:
    sys.exit("Faltan dependencias:  pip install pillow numpy")

EXTS = {".png", ".jpg", ".jpeg", ".webp"}

# La consola de Windows suele venir en cp1252 y revienta al imprimir "→" o un
# nombre de archivo con tildes. El fallo sería de impresión, no de conversión:
# sin esto, una foto bien procesada se reporta como error.
try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except (AttributeError, OSError):
    pass


def quitar_fondo(im: Image.Image, tol: int) -> Image.Image:
    """Vuelve transparente el fondo claro conectado a los bordes."""
    im = im.convert("RGBA")
    a = np.array(im)

    if a[..., 3].min() < 250:        # ya trae transparencia: se respeta
        return im

    rgb = a[..., :3].astype(np.int16)
    h, w = rgb.shape[:2]

    # Semilla: el color de las esquinas. Si no son claras, no hay fondo que quitar.
    esquinas = [rgb[0, 0], rgb[0, w - 1], rgb[h - 1, 0], rgb[h - 1, w - 1]]
    base = np.median(np.array(esquinas), axis=0)
    if base.min() < 230:
        return im

    parecido = (np.abs(rgb - base).max(axis=2) <= tol)

    # Inundación desde el marco: solo se borra lo que se alcanza desde afuera.
    fondo = np.zeros((h, w), dtype=bool)
    cola = deque()
    for x in range(w):
        for y in (0, h - 1):
            if parecido[y, x] and not fondo[y, x]:
                fondo[y, x] = True
                cola.append((y, x))
    for y in range(h):
        for x in (0, w - 1):
            if parecido[y, x] and not fondo[y, x]:
                fondo[y, x] = True
                cola.append((y, x))

    while cola:
        y, x = cola.popleft()
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < h and 0 <= nx < w and parecido[ny, nx] and not fondo[ny, nx]:
                fondo[ny, nx] = True
                cola.append((ny, nx))

    a[..., 3] = np.where(fondo, 0, a[..., 3])
    return Image.fromarray(a, "RGBA")


def bloques(masa: np.ndarray, hueco_min: int):
    """Tramos con contenido a lo largo de un eje, separados por huecos vacíos."""
    llenas = masa > 0
    tramos, ini, vacias = [], None, 0

    for i, hay in enumerate(llenas):
        if hay:
            if ini is None:
                ini = i
            vacias = 0
        elif ini is not None:
            vacias += 1
            if vacias >= hueco_min:
                tramos.append((ini, i - vacias + 1))
                ini, vacias = None, 0
    if ini is not None:
        tramos.append((ini, len(llenas)))

    return [(a, b, float(masa[a:b].sum())) for a, b in tramos]


def _util(masa: np.ndarray, umbral: float, hueco_min: int):
    """Rango que ocupa el producto, descartando tramos de poca masa."""
    tr = bloques(masa, hueco_min)
    if len(tr) < 2:
        return None, 0

    mayor = max(t[2] for t in tr)
    # Un pack de dos botellas tiene tramos de masa comparable: se conservan.
    utiles = [t for t in tr if t[2] >= mayor * umbral]
    if len(utiles) == len(tr):
        return None, 0

    return (min(t[0] for t in utiles), max(t[1] for t in utiles)), len(tr) - len(utiles)


def recortar_legal(im: Image.Image, umbral: float, hueco_min: int):
    """Descarta las franjas de texto legal impreso en la foto.

    Se mira en los dos ejes: hay proveedores que lo ponen vertical a un
    costado y otros horizontal bajo el producto. Mirar solo las columnas
    dejaba pasar la mitad de los casos.

    El criterio es la masa: el texto es un trazo fino y suelto, separado del
    producto por espacio vacío. Un pack de dos botellas también son dos
    tramos, pero de masa parecida, así que se conserva entero.
    """
    alpha = np.array(im.getchannel("A"), dtype=np.float64)

    cols, n_col = _util(alpha.sum(axis=0), umbral, hueco_min)
    filas, n_fil = _util(alpha.sum(axis=1), umbral, hueco_min)
    if not cols and not filas:
        return im, None

    x0, x1 = cols if cols else (0, im.width)
    y0, y1 = filas if filas else (0, im.height)

    partes = []
    if n_col:
        partes.append(f"{n_col} lateral(es)")
    if n_fil:
        partes.append(f"{n_fil} horizontal(es)")
    return im.crop((x0, y0, x1, y1)), " + ".join(partes)


def encuadrar(im: Image.Image, lienzo: int, ocupacion: float, pie: float,
              max_upscale: float) -> Image.Image:
    """Escala al alto objetivo, centra y apoya la base a una altura fija."""
    caja = im.getchannel("A").getbbox()
    if not caja:
        raise ValueError("la imagen quedó vacía")
    im = im.crop(caja)

    alto_obj = int(lienzo * ocupacion)
    escala = alto_obj / im.height

    # Agrandar inventa detalle que no existe: se ve blando y además pesa más,
    # porque la interpolación llena de tonos intermedios lo que antes eran
    # zonas planas. Una lata fotografiada chica se queda un poco más chica —
    # y de todos modos la presentación va escrita en la tarjeta.
    if escala > max_upscale:
        escala = max_upscale
        alto_obj = max(1, int(round(im.height * escala)))

    ancho_obj = max(1, int(round(im.width * escala)))

    # Un producto muy ancho (six pack) no puede desbordar a lo ancho.
    ancho_max = int(lienzo * 0.92)
    if ancho_obj > ancho_max:
        escala *= ancho_max / ancho_obj
        ancho_obj, alto_obj = ancho_max, max(1, int(round(im.height * escala)))

    # Zona muerta: un reescalado de menos del 8% no se nota en pantalla, pero
    # sí en el archivo. La interpolación reemplaza píxeles idénticos por tonos
    # ligeramente distintos y deja halos en los bordes; el resultado puede
    # llegar a pesar el doble que el original. Si la corrección es mínima, se
    # dejan los píxeles como estaban.
    if 0.92 <= escala <= 1.08:
        ancho_obj, alto_obj = im.size
    elif (ancho_obj, alto_obj) != im.size:
        im = im.resize((ancho_obj, alto_obj), Image.LANCZOS)

    fondo = Image.new("RGBA", (lienzo, lienzo), (0, 0, 0, 0))
    x = (lienzo - ancho_obj) // 2
    y = lienzo - int(lienzo * pie) - alto_obj      # base alineada, no centrada
    fondo.paste(im, (x, max(0, y)), im)
    return fondo


def guardar_min(im: Image.Image, destino: Path, revisar: bool) -> tuple[int, str]:
    """Prueba varias codificaciones y se queda con el archivo más chico.

    Cuál gana depende de la foto y no se puede decidir de antemano: sin
    reescalar, las zonas planas hacen ganar al modo sin pérdida; después de
    reescalar, la interpolación llena de tonos intermedios y gana el modo con
    pérdida. Se prueban todas y decide la balanza.
    """
    opciones = [
        ("sin pérdida", dict(lossless=True, method=6)),
        ("q90", dict(quality=90, method=6, alpha_quality=100)),
        ("q82", dict(quality=82, method=6, alpha_quality=100)),
    ]
    mejor_buf = mejor_modo = None
    for modo, kw in opciones:
        buf = io.BytesIO()
        im.save(buf, "WEBP", **kw)
        if mejor_buf is None or buf.tell() < mejor_buf.tell():
            mejor_buf, mejor_modo = buf, modo

    if not revisar:
        destino.write_bytes(mejor_buf.getvalue())
    return mejor_buf.tell(), mejor_modo


def procesar(trabajo):
    """Una foto, de principio a fin. Vive fuera de main() para que se pueda
    repartir entre procesos: lo que se manda a un worker tiene que ser
    importable por nombre, no una función anidada."""
    f, cfg = trabajo
    try:
        original = f.stat().st_size
        im = Image.open(f)
        im = quitar_fondo(im, cfg["tolerancia"])
        im, quitado = recortar_legal(im, cfg["umbral_legal"], cfg["hueco"])
        im = encuadrar(im, cfg["lienzo"], cfg["ocupacion"], cfg["pie"], cfg["max_upscale"])

        destino = cfg["salida"] / (f.stem + ".webp")
        peso, modo = guardar_min(im, destino, cfg["revisar"])
        return f.name, original, peso, modo, quitado, None
    except Exception as e:                                      # noqa: BLE001
        return f.name, 0, 0, "", None, str(e)


def main() -> int:
    ap = argparse.ArgumentParser(description="Normaliza fotos de producto para el catálogo.")
    ap.add_argument("entrada", type=Path, help="carpeta con las fotos originales")
    ap.add_argument("-s", "--salida", type=Path, default=Path("salida"))
    ap.add_argument("--lienzo", type=int, default=500, help="lado del cuadrado (500)")
    ap.add_argument("--ocupacion", type=float, default=0.86,
                    help="fracción del alto que ocupa el producto (0.86)")
    ap.add_argument("--pie", type=float, default=0.05,
                    help="margen bajo la base, en fracción del lienzo (0.05)")
    ap.add_argument("--tolerancia", type=int, default=18,
                    help="cuánto puede alejarse del blanco y seguir siendo fondo (18)")
    ap.add_argument("--umbral-legal", type=float, default=0.15,
                    help="masa mínima de un bloque para no ser descartado (0.15)")
    ap.add_argument("--hueco", type=int, default=12,
                    help="columnas vacías que separan dos bloques (12)")
    ap.add_argument("--max-upscale", type=float, default=1.15,
                    help="cuánto se permite agrandar una foto chica (1.15)")
    ap.add_argument("--revisar", action="store_true", help="no escribe: solo informa")
    args = ap.parse_args()

    if not args.entrada.is_dir():
        sys.exit(f"No existe la carpeta {args.entrada}")

    fotos = sorted(p for p in args.entrada.iterdir() if p.suffix.lower() in EXTS)
    if not fotos:
        sys.exit(f"No hay imágenes en {args.entrada}")

    if not args.revisar:
        args.salida.mkdir(parents=True, exist_ok=True)

    print(f"{len(fotos)} imagen(es) · lienzo {args.lienzo}px · "
          f"producto al {args.ocupacion*100:.0f}% del alto"
          + ("  [REVISIÓN, no se escribe]" if args.revisar else ""))
    print("-" * 78)

    antes = despues = 0
    fallos = []

    # Probar tres codificaciones de 60 fotos en serie son varios minutos, y
    # cada foto es independiente de las demás. Un proceso por núcleo lo baja
    # a algo que se puede esperar mirando la pantalla.
    trabajos = [(f, vars(args)) for f in fotos]
    with ProcessPoolExecutor(max_workers=min(os.cpu_count() or 4, 8)) as pool:
        for nombre, original, peso, modo, quitado, error in pool.map(procesar, trabajos):
            if error:
                fallos.append((nombre, error))
                print(f"  {nombre[:40]:<42} ERROR: {error}")
                continue
            antes += original
            despues += peso
            nota = f"  ← quitado: {quitado}" if quitado else ""
            print(f"  {nombre[:40]:<42} {original//1024:>5}K → {peso//1024:>4}K  {modo}{nota}")

    print("-" * 78)
    if antes:
        print(f"  total {antes//1024} KB → {despues//1024} KB "
              f"({100 - despues * 100 // antes}% menos)")
    if fallos:
        print(f"\n  {len(fallos)} sin convertir:")
        for n, e in fallos:
            print(f"    {n}: {e}")
    if not args.revisar:
        print(f"\n  Escrito en {args.salida.resolve()}")
        print("  Revisa el resultado antes de reemplazar los originales.")

    return 1 if fallos else 0


if __name__ == "__main__":
    raise SystemExit(main())
