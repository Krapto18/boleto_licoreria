/* ══════════════════════════════════════════════════════════════
   BOLETO LICORERÍA — lógica del sitio público
   Depende de js/data.js (CONFIG, GRUPOS, PRODUCTOS)
   ══════════════════════════════════════════════════════════════ */
(function () {
  'use strict';

  const $ = (s) => document.querySelector(s);
  const money  = (n) => `${CONFIG.moneda} ${n.toFixed(2)}`;
  const waLink = (t) => `https://wa.me/${CONFIG.telefono}?text=${encodeURIComponent(t)}`;
  const esc = (s) => String(s).replace(/[&<>"']/g, (c) =>
    ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

  const LS_PEDIDO = 'boleto:pedido';

  const carrito = new Map();
  const modos   = new Map();
  let grupo = 'Todo', busca = '', ultima = null, toastT = null, yaAbrio = false;
  let confirmado = false;   // declaración de mayoría de edad
  let envio = null;         // zona elegida: { n, c, t }

  /* Entero grande, decimales en superíndice. Recurso de cartel de
     licorería: el número que decide la compra manda, y de paso los
     precios de tres dígitos dejan de desbordar la chapa. */
  const precioHtml = (n) => {
    const [ent, dec] = n.toFixed(2).split('.');
    return `${ent}<i>${dec}</i>`;
  };

  const keyOf    = (id, m) => `${id}|${m}`;
  const modoDe   = (p) => modos.get(p.id) || 'botella';
  const precioDe = (p, m) => (m === 'combo' && p.combo ? p.combo : p.p);

  /* ══════════════════════════════════════════════════════════
     Reloj — hace verificable el 24/7. El usuario ve su propia
     hora y la web le confirma que hay alguien atendiendo.
     ══════════════════════════════════════════════════════════ */
  function reloj() {
    const t = new Date().toLocaleTimeString('es-PE',
      { hour: 'numeric', minute: '2-digit', hour12: true });
    $('#reloj').textContent = t;
    $('#relojHero').textContent = t;
  }

  /* ── Medición ────────────────────────────────────────────── */
  function medicion() {
    if (CONFIG.ga4) {
      const s = document.createElement('script');
      s.async = true;
      s.src = `https://www.googletagmanager.com/gtag/js?id=${CONFIG.ga4}`;
      document.head.appendChild(s);
      window.dataLayer = window.dataLayer || [];
      window.gtag = function () { window.dataLayer.push(arguments); };
      gtag('js', new Date());
      gtag('config', CONFIG.ga4);
    }
    if (CONFIG.metaPixel) {
      /* eslint-disable */
      !function(f,b,e,v,n,t,s){if(f.fbq)return;n=f.fbq=function(){n.callMethod?
      n.callMethod.apply(n,arguments):n.queue.push(arguments)};if(!f._fbq)f._fbq=n;
      n.push=n;n.loaded=!0;n.version='2.0';n.queue=[];t=b.createElement(e);t.async=!0;
      t.src=v;s=b.getElementsByTagName(e)[0];s.parentNode.insertBefore(t,s)}
      (window,document,'script','https://connect.facebook.net/en_US/fbevents.js');
      /* eslint-enable */
      fbq('init', CONFIG.metaPixel);
      fbq('track', 'PageView');
    }
  }

  /* Única conversión que importa: el clic en WhatsApp */
  function evento(origen) {
    const { n, total } = totales();
    if (window.gtag) gtag('event', 'contacto_whatsapp', { origen, valor: total, productos: n });
    if (window.fbq)  fbq('track', 'Lead', { value: total, currency: 'PEN', content_name: origen });
  }

  /* ── Franja y filtros ────────────────────────────────────── */
  function promobar() {
    const l = CONFIG.promos.map((t) => `${esc(t)} <b>◆</b> `).join('');
    $('#promobar').innerHTML = l + l;
  }
  function chips() {
    $('#chips').innerHTML = GRUPOS.map((g) =>
      `<button class="chip" data-g="${esc(g)}" aria-pressed="${g === grupo}">${esc(g)}</button>`).join('');
  }
  $('#chips').addEventListener('click', (e) => {
    const b = e.target.closest('.chip'); if (!b) return;
    grupo = b.dataset.g;
    document.querySelectorAll('.chip').forEach((c) => c.setAttribute('aria-pressed', c.dataset.g === grupo));
    pintar();
  });
  $('#q').addEventListener('input', (e) => { busca = e.target.value.trim().toLowerCase(); pintar(); });
  $('#promoBtn').addEventListener('click', () => {
    grupo = 'Todo'; busca = ''; $('#q').value = ''; chips();
    pintar(orden(PRODUCTOS.filter((p) => p.promo)));
    document.getElementById('catalogo').scrollIntoView({ behavior: 'smooth' });
  });

  /* Los agotados no se ocultan: se mandan al final.
     Se conserva la información y se prioriza lo accionable. */
  const orden = (l) => [...l].sort((a, b) => (b.stock !== false) - (a.stock !== false));

  function visibles() {
    return orden(PRODUCTOS.filter((p) => {
      const okG = grupo === 'Todo' || p.g === grupo;
      const okQ = !busca || `${p.n} ${p.c} ${p.v}`.toLowerCase().includes(busca);
      return okG && okQ;
    }));
  }

  /* ── Grilla ──────────────────────────────────────────────── */
  function pintar(forzada) {
    const lista = forzada || visibles();
    $('#empty').hidden = lista.length > 0;
    const agot = lista.filter((p) => p.stock === false).length;
    $('#count').textContent = `${lista.length} producto${lista.length === 1 ? '' : 's'}`
      + (agot ? ` · ${agot} agotado${agot === 1 ? '' : 's'}` : '');

    $('#grid').innerHTML = lista.map((p) => {
      const modo = modoDe(p), precio = precioDe(p, modo);
      const hay = p.stock !== false;
      const enCarrito = carrito.has(keyOf(p.id, modo));
      const foto = p.img
        ? `<img class="card__foto" src="${esc(p.img)}" alt="${esc(p.n)}" loading="lazy">`
        : `<span class="bottle" style="background:${esc(p.col)}"></span>`;

      return `
      <article class="card${hay ? '' : ' card--out'}" data-mode="${modo}" data-in="${enCarrito}">
        <div class="card__img">
          ${hay && p.combo ? '<span class="flag">Combo</span>' : ''}
          ${hay && p.promo ? '<span class="flag flag--promo">Coca gratis</span>' : ''}
          ${foto}
          <span class="badge${precio >= 100 ? ' badge--3d' : ''}" aria-label="${CONFIG.moneda} ${precio.toFixed(2)}">
            <b aria-hidden="true">${precioHtml(precio)}</b>
            <span aria-hidden="true">${CONFIG.moneda}</span>
          </span>
          ${hay ? '' : '<span class="out-tag">Agotado</span>'}
        </div>
        <div class="card__body">
          <span class="card__cat">${esc(p.c)}</span>
          <h3 class="card__name">${esc(p.n)}</h3>
          <span class="card__vol">${esc(p.v)}</span>
          ${hay && p.combo ? `
          <div class="seg" role="group" aria-label="Presentación de ${esc(p.n)}">
            <button data-set="botella" data-id="${p.id}" aria-pressed="${modo === 'botella'}">Botella</button>
            <button data-set="combo"   data-id="${p.id}" aria-pressed="${modo === 'combo'}">Combo</button>
          </div>
          <p class="seg__note">${modo === 'combo'
            ? '+ ' + [p.aco, p.hie].filter(Boolean).map(esc).join(' · ')
            : '&nbsp;'}</p>` : ''}
          <div class="card__f">${hay ? control(p, modo)
            : `<button class="avisar" data-avisar="${p.id}">Avísame cuando llegue</button>`}</div>
        </div>
      </article>`;
    }).join('');
  }

  function control(p, modo) {
    const q = carrito.get(keyOf(p.id, modo))?.q || 0;
    if (q === 0) return `<button class="add" data-add="${p.id}">Agregar</button>`;
    return `<div class="qty">
      <button data-sub="${p.id}" aria-label="Quitar uno de ${esc(p.n)}">−</button>
      <span>${q}</span>
      <button data-add="${p.id}" aria-label="Agregar uno de ${esc(p.n)}">+</button>
    </div>`;
  }

  $('#grid').addEventListener('click', (e) => {
    const set = e.target.closest('[data-set]');
    if (set) { modos.set(set.dataset.id, set.dataset.set); return pintar(); }

    /* Un agotado no es un callejón sin salida: es un contacto */
    const av = e.target.closest('[data-avisar]');
    if (av) {
      const p = PRODUCTOS.find((x) => x.id === av.dataset.avisar);
      evento('aviso_stock');
      window.open(waLink(`Hola *${CONFIG.tienda}* 👋 ¿Me avisan cuando llegue ${p.n} ${p.v}?`),
        '_blank', 'noopener');
      return;
    }
    const add = e.target.closest('[data-add]'); if (add) return mover(add.dataset.add, 1);
    const sub = e.target.closest('[data-sub]'); if (sub) return mover(sub.dataset.sub, -1);
  });

  function mover(id, d, silencio) {
    const p = PRODUCTOS.find((x) => x.id === id);
    if (!p || p.stock === false) return;
    const modo = modoDe(p), k = keyOf(id, modo);
    const q = (carrito.get(k)?.q || 0) + d;
    if (q <= 0) carrito.delete(k);
    else carrito.set(k, { ...p, modo, precio: precioDe(p, modo), q });

    pintar(); actualizar(); guardar();

    if (!silencio) {
      ultima = { id, d };
      const corto = p.n.length > 26 ? p.n.slice(0, 24) + '…' : p.n;
      toast(d > 0 ? `${corto} agregado` : `${corto} quitado`);
      if (d > 0 && !yaAbrio) { yaAbrio = true; abrir(true); }
    }
  }

  /* ── Persistencia del pedido ─────────────────────────────── */
  function guardar() {
    try {
      localStorage.setItem(LS_PEDIDO, JSON.stringify(
        [...carrito.values()].map((i) => ({ id: i.id, modo: i.modo, q: i.q }))));
    } catch (_) { /* modo privado */ }
  }
  function recuperar() {
    try {
      const raw = localStorage.getItem(LS_PEDIDO);
      if (!raw) return;
      JSON.parse(raw).forEach(({ id, modo, q }) => {
        const p = PRODUCTOS.find((x) => x.id === id);
        if (!p || p.stock === false) return;   // se cayó del catálogo o se agotó
        modos.set(id, modo);
        carrito.set(keyOf(id, modo), { ...p, modo, precio: precioDe(p, modo), q });
      });
      if (carrito.size) toast('Recuperamos tu pedido anterior');
    } catch (_) { /* ignorar */ }
  }

  /* ── Deshacer ────────────────────────────────────────────── */
  function toast(txt) {
    /* Si la vista previa está abierta, ya muestra el cambio en vivo.
       Un aviso encima sería duplicado y taparía lo que informa. */
    if ($('#bar').dataset.peek === 'true') return;
    $('#toastTxt').textContent = txt;
    $('#toast').dataset.on = 'true';
    clearTimeout(toastT);
    toastT = setTimeout(() => { $('#toast').dataset.on = 'false'; }, 4000);
  }
  $('#undo').addEventListener('click', () => {
    if (!ultima) return;
    mover(ultima.id, -ultima.d, true);
    ultima = null;
    $('#toast').dataset.on = 'false';
  });

  /* ── Mensaje ─────────────────────────────────────────────── */
  function totales() {
    const items = [...carrito.values()];
    return { items,
      n: items.reduce((s, i) => s + i.q, 0),
      total: items.reduce((s, i) => s + i.precio * i.q, 0) };
  }
  function mensaje() {
    const { items, total } = totales();
    if (!items.length) return '';
    /* El combo se desglosa: el cliente y la tienda ven exactamente
       qué acompañante y qué hielo entran en el pedido. */
    const lineas = items.map((i) => {
      let l = `• ${i.q}× ${i.n} ${i.v} — ${money(i.precio * i.q)}`;
      if (i.modo === 'combo') {
        if (i.aco) l += `\n    ↳ ${i.aco}`;
        if (i.hie) l += `\n    ↳ ${i.hie}`;
      }
      return l;
    }).join('\n');
    const regalo = items.some((i) => i.promo)
      ? '\n🎁 Con la Coca Cola 1.5 L gratis por llevar whisky/ron.\n' : '';
    /* La declaración va en el mensaje: la tienda queda con el registro
       fechado en su propio WhatsApp, que vale más que una marca en el
       navegador del cliente. */
    const edad18 = CONFIG.verificar18 && confirmado
      ? '\n✅ Confirmo que soy mayor de 18 años.'
      : '';

    /* El envío entra en el total antes de enviar: el cliente no se
       entera del cargo recién cuando la tienda le responde. */
    let bloque = `\n*Total: ${money(total)}*`;
    if (envio) {
      const t2 = total + envio.c;
      bloque = `\nSubtotal: ${money(total)}`
        + `\nDelivery ${envio.n}: ${envio.c > 0 ? money(envio.c) : 'gratis'}`
        + (envio.t ? ` (${envio.t})` : '')
        + `\n*Total: ${money(t2)}*`;
    }

    return `Hola *${CONFIG.tienda}* 👋\n\nQuiero hacer este pedido:\n\n${lineas}\n${regalo}`
      + bloque + edad18 + `\n\nMi dirección: `;
  }

  /* ── Barra ───────────────────────────────────────────────── */
  function actualizar() {
    const { n, total } = totales();
    $('#bar').dataset.on = n > 0;
    document.body.dataset.cart = n > 0 ? 'on' : 'off';
    document.body.style.setProperty('--bar-h', n > 0 ? '82px' : '0px');
    if (n === 0) abrir(false);
    $('#barTotal').textContent = money(total + (envio ? envio.c : 0));
    $('#barCount').textContent = n === 1 ? '1 producto' : `${n} productos`;
    $('#bubble').textContent = mensaje().replace(/\*/g, '');
    $('#barWa').href = waLink(mensaje());
  }
  function abrir(on) {
    $('#bar').dataset.peek = on;
    $('#peek').setAttribute('aria-expanded', on);
  }
  $('#peek').addEventListener('click', () => abrir($('#bar').dataset.peek !== 'true'));

  /* En móvil el chevron es un objetivo chico. Tocar la vista previa
     completa también la cierra: objetivo grande, misma acción.

     Menos el selector de distrito, que vive acá dentro: sin esta salida,
     tocarlo cierra la vista previa antes de poder elegir y el distrito se
     vuelve inseleccionable. */
  document.querySelector('.preview').addEventListener('click', (e) => {
    if (e.target.closest('.distrito')) return;
    abrir(false);
  });

  /* ── Instalar (PWA) ──────────────────────────────────────── */
  let prompt_ = null;
  window.addEventListener('beforeinstallprompt', (e) => {
    e.preventDefault(); prompt_ = e;
    if (localStorage.getItem('boleto:noInstalar')) return;
    setTimeout(() => { $('#install').dataset.on = 'true'; }, 8000);
  });
  $('#installBtn').addEventListener('click', async () => {
    $('#install').dataset.on = 'false';
    if (!prompt_) return;
    prompt_.prompt();
    const r = await prompt_.userChoice;
    if (window.gtag) gtag('event', 'pwa_install', { resultado: r.outcome });
    prompt_ = null;
  });
  $('#installNo').addEventListener('click', () => {
    $('#install').dataset.on = 'false';
    try { localStorage.setItem('boleto:noInstalar', '1'); } catch (_) {}
  });

  /* ══════════════════════════════════════════════════════════
     Zonas de reparto y métodos de pago
     Se rinden solo si hay datos confirmados.
     ══════════════════════════════════════════════════════════ */
  function zonasYPagos() {
    const z = $('#zonas');
    if (z && CONFIG.zonas?.length) {
      /* Con un costo uniforme, listar los 43 distritos es escribir 43 veces
         lo mismo: puro ruido entre el usuario y el pedido (Hick). Se colapsa
         en una línea. El selector de la barra sigue teniendo la lista
         completa, que es donde el usuario de verdad la necesita.
         Si el dueño diferencia precios por zona, vuelve la lista sola. */
      const uniforme = new Set(CONFIG.zonas.map((x) => x.c)).size === 1;
      const n = CONFIG.zonas.length;

      z.innerHTML = (uniforme && n > 3)
        ? `<li class="zona">
             <span class="zona__n">Reparto a ${n} distritos de Lima Metropolitana</span>
             ${CONFIG.zonas[0].c > 0
               ? `<span class="zona__c">${money(CONFIG.zonas[0].c)}</span>`
               : '<span class="zona__c--free">Gratis</span>'}
           </li>`
        : CONFIG.zonas.map((x) => `
        <li class="zona">
          <span class="zona__n">${esc(x.n)}${x.t ? `<span class="zona__t">${esc(x.t)}</span>` : ''}</span>
          ${x.c > 0
            ? `<span class="zona__c">${money(x.c)}</span>`
            : '<span class="zona__c--free">Gratis</span>'}
        </li>`).join('');
    }

    const p = $('#pagos');
    if (p && CONFIG.pagos?.length) {
      /* SLOT: cuando lleguen los logos oficiales, reemplazar el
         texto por <img src="/assets/pagos/{slug}.svg" class="pago__logo"> */
      p.innerHTML = CONFIG.pagos
        .map((m) => `<li class="pago">${esc(m)}</li>`).join('');
    }
  }

  /* ══════════════════════════════════════════════════════════
     Verificación de edad · Ley N° 28681
     Requisito regulatorio del cliente. La declaración viaja además
     dentro del mensaje: la tienda queda con el registro en su chat.
     ══════════════════════════════════════════════════════════ */
  function edad() {
    const caja = $('#edad');
    if (!caja) { confirmado = true; return; }

    try {
      if (localStorage.getItem('boleto:mayor18') === 'si') {
        confirmado = true;
        return;
      }
    } catch (_) { /* modo privado: se pregunta igual */ }

    caja.dataset.on = 'true';
    document.body.dataset.edad = 'pendiente';
    setTimeout(() => $('#edadSi')?.focus(), 120);

    $('#edadSi').addEventListener('click', () => {
      confirmado = true;
      try { localStorage.setItem('boleto:mayor18', 'si'); } catch (_) {}
      caja.dataset.on = 'false';
      document.body.dataset.edad = '';
      actualizar();
      if (window.gtag) gtag('event', 'verificacion_edad', { resultado: 'mayor' });
    });

    $('#edadNo').addEventListener('click', () => {
      $('#edadAsk').hidden = true;
      $('#edadStop').hidden = false;
      if (window.gtag) gtag('event', 'verificacion_edad', { resultado: 'menor' });
    });

    /* No se cierra con Escape: la verificación es obligatoria. */
    caja.addEventListener('keydown', (e) => { if (e.key === 'Escape') e.preventDefault(); });
  }

  /* ══════════════════════════════════════════════════════════
     Selector de distrito
     ══════════════════════════════════════════════════════════ */
  function distrito() {
    const sel = $('#distritoSel');
    if (!sel || !CONFIG.zonas?.length) return;

    sel.innerHTML = '<option value="">Elige tu distrito…</option>'
      + CONFIG.zonas.map((z, i) =>
          `<option value="${i}">${esc(z.n)} — ${z.c > 0 ? money(z.c) : 'gratis'}</option>`
        ).join('')
      + '<option value="otro">Otro distrito</option>';

    try {
      const g = localStorage.getItem('boleto:distrito');
      if (g !== null && CONFIG.zonas[g]) { sel.value = g; envio = CONFIG.zonas[g]; }
    } catch (_) {}

    sel.addEventListener('change', (e) => {
      const v = e.target.value;
      envio = (v !== '' && v !== 'otro') ? CONFIG.zonas[v] : null;
      try {
        if (envio) localStorage.setItem('boleto:distrito', v);
        else localStorage.removeItem('boleto:distrito');
      } catch (_) {}
      if (v === 'otro') toast('Coordinamos tu zona por WhatsApp');
      actualizar();
    });
  }

  /* ══════════════════════════════════════════════════════════
     Carrusel de banners
     Desplazamiento nativo con anclaje: sin librerías, funciona con
     el dedo y con teclado, y no bloquea el scroll de la página.
     ══════════════════════════════════════════════════════════ */
  function carrusel() {
    const banners = CONFIG.banners || [];
    if (!banners.length) return;

    $('#bannersSec').hidden = false;
    $('#pista').innerHTML = banners.map((b, i) => b.url
      ? `<a class="banner" href="${esc(b.url)}"><img src="${esc(b.img)}" alt="${esc(b.alt || '')}" loading="${i ? 'lazy' : 'eager'}"></a>`
      : `<div class="banner"><img src="${esc(b.img)}" alt="${esc(b.alt || '')}" loading="${i ? 'lazy' : 'eager'}"></div>`
    ).join('');

    if (banners.length < 2) return;

    const pista = $('#carrusel');
    $('#puntos').innerHTML = banners.map((_, i) =>
      `<button class="punto" role="tab" data-i="${i}" aria-selected="${i === 0}" aria-label="Promoción ${i + 1}"></button>`
    ).join('');

    $('#puntos').addEventListener('click', (e) => {
      const b = e.target.closest('.punto'); if (!b) return;
      pista.scrollTo({ left: pista.clientWidth * +b.dataset.i, behavior: 'smooth' });
    });

    pista.addEventListener('scroll', () => {
      const i = Math.round(pista.scrollLeft / pista.clientWidth);
      document.querySelectorAll('.punto').forEach((p, n) =>
        p.setAttribute('aria-selected', n === i));
    }, { passive: true });
  }

  /* ── CTAs ────────────────────────────────────────────────── */
  function ctas() {
    const saludo = `Hola *${CONFIG.tienda}* 👋 Quiero hacer un pedido.`;
    [['#heroWa', 'hero'], ['#closeWa', 'cierre'], ['#float', 'flotante'], ['#footWa', 'footer']]
      .forEach(([sel, origen]) => {
        const el = $(sel); if (!el) return;
        el.href = waLink(saludo);
        el.addEventListener('click', () => evento(origen));
      });
    $('#barWa').addEventListener('click', () => evento('pedido_armado'));
    $('#year').textContent = new Date().getFullYear();
  }

  /* ── Arranque ────────────────────────────────────────────── */
  medicion();
  reloj(); setInterval(reloj, 30000);
  promobar();
  carrusel();
  zonasYPagos();
  distrito();
  chips();
  edad();
  recuperar();
  pintar();
  actualizar();
  ctas();

  /* Acá se llamaba animarEntrada(), que no existe en ninguna parte. El
     ReferenceError abortaba todo lo que viene debajo: el observador que
     oculta el botón flotante y —peor— el registro del service worker.
     La PWA nunca llegaba a instalarse y el sitio no abría sin señal, que
     es justo lo que promete. No hay ningún elemento .reveal en el HTML,
     así que la función no tenía nada que animar: se quita la llamada. */

  /* Mientras el botón del hero se vea, el flotante sobra: dos CTA
     verdes juntos se estorban en vez de reforzarse. */
  if ('IntersectionObserver' in window) {
    new IntersectionObserver(
      ([e]) => { document.body.dataset.hero = e.isIntersecting; },
      { threshold: 0 }
    ).observe($('#heroWa'));
  }

  if ('serviceWorker' in navigator && location.protocol !== 'file:') {
    navigator.serviceWorker.register('/sw.js').catch(() => {});
  }
})();
