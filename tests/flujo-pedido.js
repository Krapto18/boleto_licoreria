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

(async () => {
  const browser = await abrirNavegador();
  try {
    for (const perfil of PERFILES) await probar(browser, perfil);
    await accesibilidad(browser);
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
