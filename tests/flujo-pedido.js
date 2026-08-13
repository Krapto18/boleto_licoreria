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

(async () => {
  const browser = await abrirNavegador();
  try {
    for (const perfil of PERFILES) await probar(browser, perfil);
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
