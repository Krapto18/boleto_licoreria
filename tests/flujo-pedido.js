/* ══════════════════════════════════════════════════════════════
   Flujo crítico del negocio, en un navegador de verdad.

   Cubre lo único que la web tiene que lograr: que el cliente arme su
   pedido, elija su distrito y el mensaje de WhatsApp salga con el
   total correcto.

   Existe porque dos defectos reales pasaron una verificación hecha
   solo con peticiones al servidor: el selector de distrito cerraba la
   vista previa al tocarlo, y un ReferenceError en el arranque impedía
   que se registrara el service worker. Ninguno de los dos se ve desde
   fuera del navegador.

   Uso:
     npm install
     npm test                      (la app debe estar corriendo)
     BOLETO_URL=http://localhost:5000 npm test
   ══════════════════════════════════════════════════════════════ */
const { chromium } = require('playwright');

const URL = process.env.BOLETO_URL || 'http://localhost:5199/';

let fallos = 0;
const ok = (cond, msg) => {
  console.log((cond ? '  PASA  ' : '  FALLA ') + msg);
  if (!cond) fallos++;
};

/* Perfiles: el dueño mira desde escritorio, el cliente casi siempre
   desde el celular. El defecto del selector se comportaba igual en los
   dos, pero el hover y los objetivos táctiles no. */
const PERFILES = [
  { nombre: 'ESCRITORIO', viewport: { width: 1280, height: 900 } },
  { nombre: 'MOVIL', viewport: { width: 390, height: 844 }, hasTouch: true, isMobile: true }
];

async function abrirNavegador() {
  // Chrome del sistema primero: evita bajar 150 MB si ya está instalado.
  try {
    return await chromium.launch({ channel: 'chrome' });
  } catch {
    console.log('  (sin Chrome del sistema, usando el Chromium de Playwright)');
    return await chromium.launch();
  }
}

async function probar(browser, perfil) {
  console.log('\n=== ' + perfil.nombre + ' ===');

  const ctx = await browser.newContext(perfil);
  const p = await ctx.newPage();
  const errores = [];
  p.on('pageerror', (e) => errores.push(e.message));

  await p.goto(URL, { waitUntil: 'networkidle' });

  // Verificación de edad: puede estar desactivada desde la tabla Tienda.
  if (await p.locator('#edadSi').isVisible().catch(() => false)) {
    await p.click('#edadSi');
  }

  if (await p.locator('.card:not(.card--out) .add').count() === 0) {
    throw new Error('No hay productos con stock. ¿Corrió el seed?');
  }

  // ── Armar el pedido ─────────────────────────────────────────
  await p.click('.card:not(.card--out) .add');
  await p.waitForTimeout(300);

  const peek = () => p.getAttribute('#bar', 'data-peek');

  ok(await peek() === 'true',
     'la vista previa se abre sola al agregar el primer producto');

  if (await p.locator('#distritoSel').count() === 0) {
    throw new Error('No hay selector de distrito: la tabla Tienda no tiene zonas cargadas.');
  }

  const totalAntes = (await p.textContent('#barTotal')).trim();

  /* ── El defecto que reportó el dueño ──────────────────────────
     El selector vive dentro de .preview, que se cierra con cualquier
     clic. Sin la salida explícita, el distrito no se puede elegir. */
  await p.click('#distritoSel');
  await p.waitForTimeout(250);
  ok(await peek() === 'true',
     'la vista previa SIGUE ABIERTA al tocar el selector de distrito');

  await p.click('.distrito__lbl');
  await p.waitForTimeout(250);
  ok(await peek() === 'true', 'sigue abierta al tocar la etiqueta "Tu distrito"');

  // ── El costo del delivery entra al total ────────────────────
  const opcion = await p.$eval('#distritoSel', (s) => {
    const o = [...s.options].find((x) => /—/.test(x.textContent) && x.value !== 'otro');
    return { value: o.value, texto: o.textContent };
  });
  const costo = parseFloat((opcion.texto.match(/([\d.]+)\s*$/) || [0, '0'])[1]) || 0;

  await p.selectOption('#distritoSel', opcion.value);
  await p.waitForTimeout(250);

  const num = (s) => parseFloat(s.replace(/[^\d.]/g, ''));
  const totalDespues = (await p.textContent('#barTotal')).trim();
  ok(Math.abs(num(totalDespues) - (num(totalAntes) + costo)) < 0.01,
     `el delivery entra al total: ${totalAntes} + ${costo} = ${totalDespues}`);

  // ── El mensaje que se manda por WhatsApp ────────────────────
  const burbuja = await p.textContent('#bubble');
  ok(/Delivery/i.test(burbuja), 'el mensaje desglosa la línea de delivery');
  ok(/Subtotal/i.test(burbuja), 'el mensaje separa el subtotal del total');
  ok(/wa\.me/.test(await p.getAttribute('#barWa', 'href')),
     'el botón enviar apunta a WhatsApp con el pedido armado');

  // Cerrar por fuera del selector debe seguir funcionando
  await p.click('#bubble');
  await p.waitForTimeout(250);
  ok(await peek() === 'false',
     'tocar la burbuja SÍ cierra la vista previa (no se rompió el gesto original)');

  // ── Persistencia entre visitas ──────────────────────────────
  await p.reload({ waitUntil: 'networkidle' });
  await p.waitForTimeout(400);
  ok(await p.inputValue('#distritoSel') !== '',
     'el distrito elegido se recuerda tras recargar');
  ok(num(await p.textContent('#barTotal')) > 0, 'el pedido se recupera tras recargar');

  /* ── El arranque llega hasta el final ────────────────────────
     Un ReferenceError a media función deja lo de abajo sin ejecutar,
     y ahí es donde se registra el service worker. */
  /* La barra es fija y tapa el pie de la página: --bar-h reserva ese
     espacio y posiciona el aviso y el botón flotante. Estuvo clavado en
     82 px mientras la barra medía 136: el final de la página quedaba
     debajo y el aviso se dibujaba encima de la barra. */
  {
    if (await peek() === 'true') { await p.click('#peek'); await p.waitForTimeout(450); }
    const m = await p.evaluate(() => {
      document.documentElement.style.scrollBehavior = 'auto';
      window.scrollTo(0, document.documentElement.scrollHeight);
      const bar = document.querySelector('#bar').getBoundingClientRect();
      const pie = document.querySelector('.legal--c').getBoundingClientRect();
      return { barra: Math.round(bar.height), holgura: Math.round(bar.top - pie.bottom),
               reserva: Math.round(parseFloat(getComputedStyle(document.body).paddingBottom)) };
    });
    ok(m.reserva >= m.barra,
       `--bar-h reserva la altura real de la barra (${m.reserva} >= ${m.barra})`);
    ok(m.holgura >= 0,
       `el pie de página no queda debajo de la barra (holgura ${m.holgura}px)`);
    await p.evaluate(() => window.scrollTo(0, 0));
  }

  ok(await p.evaluate(() => navigator.serviceWorker.getRegistration().then((r) => !!r)),
     'el service worker QUEDA REGISTRADO (la PWA abre sin señal)');
  ok(await p.getAttribute('body', 'data-hero') !== null,
     'corre el observador que oculta el botón flotante sobre el hero');

  ok(errores.length === 0,
     'sin errores de JavaScript' + (errores.length ? ': ' + errores.join('; ') : ''));

  await ctx.close();
}

/* ══════════════════════════════════════════════════════════════
   Accesibilidad. El documento de decisiones afirma tres cosas que
   conviene medir y no suponer: 44 px de objetivo táctil, foco visible
   nunca suprimido, y una verificación de edad de la que no se sale.
   Las tres estaban incumplidas cuando se midieron por primera vez.
   ══════════════════════════════════════════════════════════════ */
async function accesibilidad(browser) {
  console.log('\n=== ACCESIBILIDAD ===');

  // ── Objetivos táctiles reales, en móvil ──
  {
    const ctx = await browser.newContext({ viewport: { width: 390, height: 844 },
                                           hasTouch: true, isMobile: true });
    const p = await ctx.newPage();
    await p.goto(URL, { waitUntil: 'networkidle' });
    if (await p.locator('#edadSi').isVisible().catch(() => false)) await p.click('#edadSi');
    await p.click('.card:not(.card--out) .add');
    await p.waitForTimeout(300);

    const chicos = await p.evaluate(() => {
      const out = [];
      document.querySelectorAll('a,button,select,[role="tab"]').forEach((el) => {
        const cs = getComputedStyle(el);
        // Lo invisible o inerte no es un objetivo táctil
        if (cs.visibility === 'hidden' || cs.display === 'none') return;
        if (parseFloat(cs.opacity) === 0 || cs.pointerEvents === 'none') return;
        let r = el.getBoundingClientRect();
        if (!r.width || !r.height) return;
        const lbl = el.closest('label');
        if (lbl) { const l = lbl.getBoundingClientRect();
                   r = { width: Math.max(r.width, l.width), height: Math.max(r.height, l.height) }; }
        if (r.height < 44 || r.width < 44) {
          out.push(`${el.tagName.toLowerCase()}${el.id ? '#' + el.id : ''} ` +
                   `${Math.round(r.width)}x${Math.round(r.height)} "${(el.textContent || '').trim().slice(0, 18)}"`);
        }
      });
      return [...new Set(out)];
    });
    ok(chicos.length === 0,
       'todos los objetivos táctiles llegan a 44px' +
       (chicos.length ? ` — faltan ${chicos.length}: ${chicos.slice(0, 5).join(' | ')}` : ''));
    await ctx.close();
  }

  // ── Foco visible en todo control del flujo ──
  {
    const ctx = await browser.newContext({ viewport: { width: 1280, height: 900 } });
    const p = await ctx.newPage();
    await p.goto(URL, { waitUntil: 'networkidle' });
    if (await p.locator('#edadSi').isVisible().catch(() => false)) await p.click('#edadSi');
    await p.click('.card:not(.card--out) .add');
    await p.waitForTimeout(300);

    const sinFoco = [];
    for (const sel of ['#q', '#distritoSel', '#peek', '#barWa', '.chip']) {
      if (!(await p.locator(sel).first().count())) continue;
      await p.locator(sel).first().focus();
      await p.keyboard.press('Shift+Tab');
      await p.keyboard.press('Tab');
      const s = await p.evaluate(() => {
        const cs = getComputedStyle(document.activeElement);
        return { ow: parseFloat(cs.outlineWidth), os: cs.outlineStyle, bs: cs.boxShadow };
      });
      if (!((s.os !== 'none' && s.ow > 0) || s.bs !== 'none')) sinFoco.push(sel);
    }
    ok(sinFoco.length === 0,
       'foco visible en todos los controles del pedido' +
       (sinFoco.length ? ` — sin indicador: ${sinFoco.join(', ')}` : ''));
    await ctx.close();
  }

  // ── La verificación de edad contiene el foco (Ley N° 28681) ──
  {
    const ctx = await browser.newContext({ viewport: { width: 1280, height: 900 } });
    const p = await ctx.newPage();
    await p.goto(URL, { waitUntil: 'networkidle' });
    await p.waitForTimeout(400);

    if (await p.locator('#edad[data-on="true"]').count()) {
      let escapo = 0;
      for (let i = 0; i < 10; i++) {
        await p.keyboard.press('Tab');
        if (await p.evaluate(() => !document.activeElement?.closest('#edad'))) { escapo = i + 1; break; }
      }
      ok(escapo === 0,
         'el foco no se escapa de la verificación de edad en 10 tabulaciones' +
         (escapo ? ` — salió en el Tab #${escapo}` : ''));
    }
    await ctx.close();
  }

  /* ── La regla de color del proyecto ──────────────────────────
     El verde --wa se reserva para lo que abre WhatsApp. Es la única
     conversión que importa, y el efecto Von Restorff solo funciona si
     ese color no aparece en ningún otro lado: si estuviera en cinco
     sitios distintos dejaría de significar "escríbenos".

     Se mira el fondo pintado, que es lo que domina la vista, en los
     estados por los que pasa el cliente. */
  {
    const ctx = await browser.newContext({ viewport: { width: 390, height: 844 },
                                           hasTouch: true, isMobile: true });
    const p = await ctx.newPage();
    await p.goto(URL, { waitUntil: 'networkidle' });
    if (await p.locator('#edadSi').isVisible().catch(() => false)) await p.click('#edadSi');
    await p.waitForTimeout(300);

    const inventario = () => {
      const hsl = (c) => {
        const m = (c || '').match(/[\d.]+/g);
        if (!m || m.length < 3) return null;
        if (m.length >= 4 && Number(m[3]) === 0) return null;
        const [r, g, b] = m.slice(0, 3).map(Number).map((v) => v / 255);
        const mx = Math.max(r, g, b), mn = Math.min(r, g, b), d = mx - mn;
        let h = 0;
        if (d) { h = mx === r ? ((g - b) / d) % 6 : mx === g ? (b - r) / d + 2 : (r - g) / d + 4;
                 h *= 60; if (h < 0) h += 360; }
        return { h, s: mx ? d / mx : 0, l: (mx + mn) / 2 };
      };
      const esVerde = (c) => {
        const v = hsl(c);                       // --wa: h=142 s=.82 l=.49
        return !!v && v.h > 110 && v.h < 175 && v.s > 0.45 && v.l > 0.30;
      };
      const out = { verdes: [], intrusos: [] };
      document.querySelectorAll('body *').forEach((el) => {
        const cs = getComputedStyle(el);
        if (cs.visibility === 'hidden' || cs.display === 'none') return;
        if (parseFloat(cs.opacity) === 0 || cs.pointerEvents === 'none') return;
        const r = el.getBoundingClientRect();
        if (!r.width || !r.height || r.bottom < 0 || r.top > window.innerHeight) return;
        if (!esVerde(cs.backgroundColor)) return;

        const a = el.closest('a');
        const wa = !!(a && /wa\.me|whatsapp/i.test(a.getAttribute('href') || ''));
        const id = el.tagName.toLowerCase() + (el.id ? '#' + el.id : '');
        (wa ? out.verdes : out.intrusos).push(`${id} ${Math.round(r.width * r.height)}px²`);
      });
      return out;
    };

    const estados = [
      ['hero', async () => {}],
      ['catálogo', async () => { await p.evaluate(() => {
          document.documentElement.style.scrollBehavior = 'auto';
          document.querySelector('#catalogo').scrollIntoView(); }); await p.waitForTimeout(500); }],
      ['con pedido', async () => { await p.click('.card:not(.card--out) .add');
          await p.waitForTimeout(700); }],
      ['pie de página', async () => { await p.evaluate(() =>
          window.scrollTo(0, document.documentElement.scrollHeight)); await p.waitForTimeout(500); }]
    ];

    const intrusos = [];
    let sinVerde = [];
    for (const [nombre, preparar] of estados) {
      await preparar();
      const r = await p.evaluate(inventario);
      r.intrusos.forEach((x) => intrusos.push(`${nombre}: ${x}`));
      if (!r.verdes.length) sinVerde.push(nombre);
    }

    ok(intrusos.length === 0,
       'el verde de marca solo aparece en lo que abre WhatsApp' +
       (intrusos.length ? ` — intrusos: ${intrusos.join(' | ')}` : ''));
    ok(sinVerde.length === 0,
       'siempre hay un llamado a WhatsApp a la vista' +
       (sinVerde.length ? ` — sin verde en: ${sinVerde.join(', ')}` : ''));

    // El CTA principal tiene que ser legible además de visible
    const contraste = await p.evaluate(() => {
      const lum = (c) => { const [r, g, b] = c.match(/[\d.]+/g).slice(0, 3).map(Number)
        .map((v) => { v /= 255; return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4); });
        return 0.2126 * r + 0.7152 * g + 0.0722 * b; };
      const el = document.querySelector('#barWa') || document.querySelector('.btn--wa');
      const cs = getComputedStyle(el);
      const l1 = lum(cs.color), l2 = lum(cs.backgroundColor);
      return (Math.max(l1, l2) + 0.05) / (Math.min(l1, l2) + 0.05);
    });
    ok(contraste >= 4.5,
       `el texto del botón de WhatsApp contrasta ${contraste.toFixed(2)}:1 (AA pide 4.5)`);

    await ctx.close();
  }

  // ── El pedido sin distrito lo dice en el mensaje ──
  {
    const ctx = await browser.newContext({ viewport: { width: 390, height: 844 },
                                           hasTouch: true, isMobile: true });
    const p = await ctx.newPage();
    await p.goto(URL, { waitUntil: 'networkidle' });
    if (await p.locator('#edadSi').isVisible().catch(() => false)) await p.click('#edadSi');
    await p.click('.card:not(.card--out) .add');
    await p.waitForTimeout(300);

    if (await p.locator('#distritoCaja').count()) {
      ok(await p.getAttribute('#distritoCaja', 'data-pendiente') === 'true',
         'sin distrito elegido, el selector se muestra pendiente');
      ok(await p.isVisible('#distritoSel'),
         'el selector se ve SIN tener que desplegar la vista previa');

      await p.click('#bubble');           // cerrar la vista previa
      await p.waitForTimeout(200);
      ok(await p.isVisible('#distritoSel'),
         'el selector sigue visible con la vista previa cerrada');

      const href = decodeURIComponent(await p.getAttribute('#barWa', 'href'));
      ok(/falta sumar el delivery/i.test(href),
         'el mensaje avisa que al total le falta el delivery');
    }
    await ctx.close();
  }
}

/* ══════════════════════════════════════════════════════════════
   Navegación · buscador del nav y menú plegable

   El sello "Abierto ahora" dejó su sitio a un buscador, y en móvil
   el menú se pliega detrás de una hamburguesa. Esconder navegación
   cuesta (Nielsen #6): lo que se comprueba acá es que lo que quedó
   plegado funcione — que diga si está abierto, que se salga con
   Escape, que se cierre al elegir — y que buscar siga siendo
   posible sin abrir nada.
   ══════════════════════════════════════════════════════════════ */
async function navegacion(browser) {
  console.log('\n=== NAVEGACIÓN ===');

  const preparar = async (perfil) => {
    const ctx = await browser.newContext(perfil);
    const p = await ctx.newPage();
    await p.goto(URL, { waitUntil: 'networkidle' });
    if (await p.locator('#edadSi').isVisible().catch(() => false)) await p.click('#edadSi');
    await p.waitForTimeout(200);
    return { ctx, p };
  };

  // ── Escritorio: el menú se queda como estaba, con el buscador ──
  {
    const { ctx, p } = await preparar({ viewport: { width: 1280, height: 900 } });

    ok(await p.locator('.status').count() === 0,
       'el sello "Abierto ahora" ya no está en el nav');

    /* El titular del hero es ahora el logo del negocio. Una imagen no
       dice a qué vino uno; lo que impide que la página se quede muda
       es que siga siendo el <h1> y que el texto alternativo cargue la
       propuesta de valor — es lo que leen Google y un lector. */
    const marca = await p.evaluate(() => {
      const h = [...document.querySelectorAll('h1')];
      const img = document.querySelector('.cartel__marca img');
      return {
        h1s: h.length,
        dentro: !!(img && img.closest('h1')),
        alt: (img?.getAttribute('alt') || '').trim(),
        cargo: !!(img && img.complete && img.naturalWidth > 0)
      };
    });
    ok(marca.h1s === 1 && marca.dentro,
       'el logo del hero sigue siendo el h1 de la página');
    ok(/24 horas/i.test(marca.alt),
       `el texto alternativo carga la propuesta de valor ("${marca.alt}")`);
    ok(marca.cargo, 'el logo del hero carga de verdad (si no, el h1 queda vacío)');

    /* ── El kit de marca ──────────────────────────────────────
       Piezas que no se ven fallar: un SVG que no carga deja un
       hueco silencioso, y una máscara CSS que no se aplica deja un
       cuadrado de color donde debería haber una silueta. */
    const kit = await p.evaluate(() => {
      const rotas = [...document.querySelectorAll('img')]
        .filter((i) => i.complete && i.naturalWidth === 0 && !i.hidden)
        .map((i) => i.getAttribute('src'));
      const iconos = [...document.querySelectorAll('.ico')].map((el) => {
        const cs = getComputedStyle(el);
        const r = el.getBoundingClientRect();
        return {
          clase: el.className,
          mascara: (cs.maskImage || cs.webkitMaskImage || 'none'),
          w: Math.round(r.width), h: Math.round(r.height)
        };
      });
      return { rotas, iconos, sellos: document.querySelectorAll('.logo__img').length };
    });
    ok(kit.rotas.length === 0,
       'ninguna imagen de marca queda rota' +
       (kit.rotas.length ? ` — ${kit.rotas.join(', ')}` : ''));
    ok(kit.sellos >= 2, `el isotipo está en el nav y en el pie (${kit.sellos})`);
    ok(kit.iconos.length === 3, `los tres iconos del kit están en la página (${kit.iconos.length})`);

    const sinMascara = kit.iconos.filter((i) => !/url\(/.test(i.mascara));
    ok(sinMascara.length === 0,
       'cada icono recorta su silueta con la máscara del SVG' +
       (sinMascara.length ? ` — sin máscara: ${sinMascara.map((i) => i.clase).join(', ')}` : ''));
    const sinCaja = kit.iconos.filter((i) => i.w < 16 || i.h < 16);
    ok(sinCaja.length === 0,
       'y ninguno queda sin caja' +
       (sinCaja.length ? ` — ${sinCaja.map((i) => `${i.clase} ${i.w}x${i.h}`).join(', ')}` : ''));
    ok(await p.isVisible('#qNav'), 'el buscador ocupa su lugar en el nav');
    ok(!(await p.isVisible('#burger')), 'en escritorio no aparece la hamburguesa');
    ok(await p.isVisible('.nav__links a[href="#catalogo"]') &&
       await p.isVisible('.nav__link--esc'),
       'en escritorio el menú sigue desplegado con Catálogo y Promoción');

    /* Los dos buscadores son el mismo estado: si dijeran cosas
       distintas, el cliente vería una grilla filtrada por algo que no
       está escrito en el campo que tiene delante. */
    const nombre = (await p.textContent('.card .card__name')).trim().split(' ')[0];
    const antes = await p.locator('.card').count();
    await p.fill('#qNav', nombre);
    await p.waitForTimeout(350);
    const despues = await p.locator('.card').count();

    ok(despues > 0 && despues <= antes,
       `buscar desde el nav filtra el catálogo (${antes} → ${despues} con "${nombre}")`);
    ok((await p.inputValue('#q')) === nombre,
       'el buscador del catálogo repite lo que se escribió en el del nav');

    await p.fill('#q', '');
    await p.waitForTimeout(300);
    ok((await p.inputValue('#qNav')) === '' && await p.locator('.card').count() === antes,
       'borrar en uno limpia el otro y devuelve el catálogo completo');

    await ctx.close();
  }

  // ── Móvil: logo y hamburguesa, nada más ──
  {
    const { ctx, p } = await preparar({ viewport: { width: 390, height: 844 },
                                        hasTouch: true, isMobile: true });

    const abierto = () => p.getAttribute('#burger', 'aria-expanded');

    ok(await p.isVisible('#burger'), 'en móvil aparece la hamburguesa');
    const caja = await p.locator('#burger').boundingBox();
    ok(caja.width >= 44 && caja.height >= 44,
       `la hamburguesa mide ${Math.round(caja.width)}x${Math.round(caja.height)} (Fitts pide 44)`);
    ok(!(await p.isVisible('#navMenu')), 'el menú arranca plegado');
    ok(await abierto() === 'false', 'el botón dice que está cerrado (aria-expanded)');

    /* Que el nav se pliegue no puede dejar sin buscador al cliente:
       el del catálogo sigue a la vista sin abrir nada. */
    await p.evaluate(() => {
      document.documentElement.style.scrollBehavior = 'auto';
      document.querySelector('#catalogo').scrollIntoView();
    });
    await p.waitForTimeout(300);
    ok(await p.isVisible('#q'), 'el buscador del catálogo se ve sin abrir el menú');
    await p.evaluate(() => window.scrollTo(0, 0));
    await p.waitForTimeout(300);

    await p.click('#burger');
    await p.waitForTimeout(300);
    ok(await abierto() === 'true' && await p.isVisible('#navMenu'),
       'la hamburguesa abre el menú y lo anuncia');
    ok(await p.isVisible('.nav__links a[href="#catalogo"]'),
       'dentro del menú está el catálogo');
    ok(!(await p.isVisible('.nav__link--esc')),
       'en móvil el menú queda con lo pedido: solo el catálogo');
    ok(await p.evaluate(() => document.activeElement?.closest('#navMenu') !== null),
       'al abrir, el foco entra al menú');

    // Nielsen #3 · salida de emergencia
    await p.keyboard.press('Escape');
    await p.waitForTimeout(250);
    ok(await abierto() === 'false' && !(await p.isVisible('#navMenu')),
       'Escape cierra el menú');
    ok(await p.evaluate(() => document.activeElement?.id === 'burger'),
       'y el foco vuelve al botón que lo abrió');

    await p.click('#burger');
    await p.waitForTimeout(250);
    await p.click('.nav__links a[href="#catalogo"]');
    await p.waitForTimeout(400);
    ok(await abierto() === 'false',
       'elegir una opción cierra el menú (si no, tapa la sección a la que salta)');

    await ctx.close();
  }
}

/* ══════════════════════════════════════════════════════════════
   Los dos carruseles

   La base todavía no tiene banners cargados, así que se inyectan
   en la respuesta: sin esto la función quedaría sin probar hasta
   que el dueño suba el primero, que es tarde para enterarse.
   ══════════════════════════════════════════════════════════════ */
async function carruseles(browser) {
  console.log('\n=== CARRUSELES ===');

  const falsos = [];
  for (let i = 1; i <= 5; i++) falsos.push({ img: '/assets/logo.svg', alt: `Arriba ${i}`, url: '', g: 1 });
  for (let i = 1; i <= 3; i++) falsos.push({ img: '/assets/logo.svg', alt: `Abajo ${i}`, url: '', g: 2 });

  const ctx = await browser.newContext({ viewport: { width: 390, height: 844 },
                                         hasTouch: true, isMobile: true });
  const p = await ctx.newPage();

  await p.route(URL, async (route) => {
    const r = await route.fetch();
    const html = (await r.text()).replace(/"banners":\[[^\]]*\]/,
      '"banners":' + JSON.stringify(falsos));
    await route.fulfill({ response: r, body: html });
  });

  await p.goto(URL, { waitUntil: 'networkidle' });
  if (await p.locator('#edadSi').isVisible().catch(() => false)) await p.click('#edadSi');
  await p.waitForTimeout(300);

  const n = async (sel) => p.locator(sel).count();

  ok(await p.evaluate(() => (CONFIG.banners || []).length) === 8,
     'la inyección llegó: 8 banners repartidos en dos carruseles');
  ok(await n('#pista1 .banner') === 5 && await n('#pista2 .banner') === 3,
     `cada banner cae en su carrusel (arriba ${await n('#pista1 .banner')}, abajo ${await n('#pista2 .banner')})`);
  ok(await n('#puntos1 .punto') === 5 && await n('#puntos2 .punto') === 3,
     'cada carrusel tiene sus propios puntos');

  const orden = await p.evaluate(() => {
    const y = (s) => document.querySelector(s).getBoundingClientRect().top + window.scrollY;
    return { arriba: y('#bannersSec1'), cat: y('#catalogo'), abajo: y('#bannersSec2') };
  });
  ok(orden.arriba < orden.abajo && orden.abajo < orden.cat,
     'el segundo carrusel va justo debajo del primero, antes del catálogo');

  /* Diez banners empujan el catálogo hacia abajo. El enlace del menú
     tiene que seguir saltándolos: es la salida rápida del que vino a
     comprar y no a mirar promociones. */
  await p.click('#burger');
  await p.waitForTimeout(250);
  await p.click('#navMenu a[href="#catalogo"]');
  await p.waitForTimeout(900);
  ok(await p.evaluate(() => {
    const r = document.querySelector('#catalogo').getBoundingClientRect();
    return r.top < window.innerHeight / 2;
  }), 'el enlace "Catálogo" salta por encima de los dos carruseles');

  /* Antes los puntos se buscaban con un querySelectorAll global.
     Con dos pistas, mover una habría marcado los puntos de la otra. */
  await p.click('#puntos2 .punto[data-i="1"]');
  await p.waitForTimeout(600);
  const sel = await p.evaluate(() => {
    const cual = (id) => [...document.querySelectorAll(id + ' .punto')]
      .findIndex((b) => b.getAttribute('aria-selected') === 'true');
    return { uno: cual('#puntos1'), dos: cual('#puntos2') };
  });
  ok(sel.dos === 1 && sel.uno === 0,
     `mover un carrusel no marca los puntos del otro (arriba ${sel.uno}, abajo ${sel.dos})`);

  const punto = await p.locator('#puntos1 .punto').first().boundingBox();
  ok(punto.width >= 44 && punto.height >= 44,
     `los puntos se tocan a ${Math.round(punto.width)}x${Math.round(punto.height)} aunque se dibujen a 9`);

  await ctx.close();
}

/* ══════════════════════════════════════════════════════════════
   El combo dice qué incluye

   Un combo es la botella más un aditivo y/o hielo, y qué lleva cada
   uno se decide con una casilla en el panel. Lo que se comprueba acá
   es la punta visible de esa cadena: que lo marcado llegue al mensaje
   de WhatsApp, que es donde el cliente y la tienda se ponen de
   acuerdo. Lo desmarcado no debe aparecer — ni siquiera en el HTML.
   ══════════════════════════════════════════════════════════════ */
async function combos(browser) {
  console.log('\n=== COMBOS ===');

  const ctx = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  const p = await ctx.newPage();
  await p.goto(URL, { waitUntil: 'networkidle' });
  if (await p.locator('#edadSi').isVisible().catch(() => false)) await p.click('#edadSi');
  await p.waitForTimeout(300);

  const conCombo = await p.evaluate(() =>
    PRODUCTOS.filter((x) => x.combo != null && x.stock !== false)
             .map((x) => ({ id: x.id, n: x.n, aco: x.aco, hie: x.hie })));

  if (!conCombo.length) {
    ok(false, 'hay al menos un producto con combo en el catálogo');
    await ctx.close();
    return;
  }
  ok(true, `${conCombo.length} productos con combo en el catálogo`);

  const conPartes = conCombo.find((x) => x.aco || x.hie);
  ok(!!conPartes,
     'algún combo dice de qué está compuesto' +
     (conPartes ? ` (${conPartes.n}: ${[conPartes.aco, conPartes.hie].filter(Boolean).join(' + ')})` : ''));
  if (!conPartes) { await ctx.close(); return; }

  await p.click(`.card [data-set="combo"][data-id="${conPartes.id}"]`);
  await p.waitForTimeout(200);
  await p.click(`.card [data-add="${conPartes.id}"], .card [data-id="${conPartes.id}"] ~ * .add`)
    .catch(async () => {
      // El botón de agregar no siempre lleva el id: se busca por la tarjeta.
      await p.evaluate((id) => {
        const seg = document.querySelector(`[data-set="combo"][data-id="${id}"]`);
        seg.closest('.card').querySelector('.add').click();
      }, conPartes.id);
    });
  await p.waitForTimeout(600);

  const msg = decodeURIComponent(await p.getAttribute('#barWa', 'href'));
  for (const parte of [conPartes.aco, conPartes.hie].filter(Boolean)) {
    ok(msg.includes(parte),
       `el mensaje detalla que el combo lleva "${parte}"`);
  }

  /* Lo que el panel desmarcó no viaja al navegador: no basta con no
     pintarlo, no puede estar en el HTML de una página pública. */
  const apagados = await p.evaluate(() =>
    PRODUCTOS.filter((x) => x.combo != null && !x.aco && !x.hie).length);
  const html = await p.content();
  ok(!/"aco":null|"hie":null/.test(html),
     `los combos sin composición viajan vacíos, no como nulos (${apagados} así)`);

  await ctx.close();
}

(async () => {
  const browser = await abrirNavegador();
  try {
    for (const perfil of PERFILES) await probar(browser, perfil);
    await accesibilidad(browser);
    await navegacion(browser);
    await carruseles(browser);
    await combos(browser);
  } finally {
    await browser.close();
  }
  console.log(fallos === 0 ? '\nTODO PASA' : `\n${fallos} FALLO(S)`);
  process.exit(fallos ? 1 : 0);
})().catch((e) => {
  console.error('\nERROR: ' + e.message);
  console.error('¿Está corriendo la app en ' + URL + '?');
  process.exit(1);
});
