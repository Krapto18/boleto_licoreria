/* ══════════════════════════════════════════════════════════════
   PANEL — edición de precios y stock

   El borrador vive en este dispositivo (localStorage) hasta que
   se publica. Al publicar se manda al servidor, que revalida,
   guarda, deja auditoría e invalida la caché del catálogo.
   El estado "sin publicar" está siempre a la vista para que
   nadie crea que ya cambió la web.
   ══════════════════════════════════════════════════════════════ */
(function () {
  'use strict';

  const $ = (s) => document.querySelector(s);
  const money = (n) => `${CONFIG.moneda} ${n.toFixed(2)}`;
  const esc = (s) => String(s).replace(/[&<>"']/g, (c) =>
    ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

  const LS = 'boleto:borrador';
  const BASE = PRODUCTOS.map((p) => ({ ...p }));   // catálogo publicado (mutable al publicar)
  let draft = BASE.map((p) => ({ ...p }));         // catálogo en edición
  let grupo = 'Todo', busca = '', soloCambiados = false;
  let ultimo = null, toastT = null;

  const base = (id) => BASE.find((p) => p.id === id);
  const cambiado = (p) => {
    const b = base(p.id);
    return b.p !== p.p || b.combo !== p.combo || b.stock !== p.stock;
  };
  const cambios = () => draft.filter(cambiado);

  /* ── Borrador ────────────────────────────────────────────── */
  function guardar() {
    try {
      localStorage.setItem(LS, JSON.stringify(
        cambios().map((p) => ({ id: p.id, p: p.p, combo: p.combo, stock: p.stock }))));
    } catch (_) {}
  }
  function recuperar() {
    try {
      const raw = localStorage.getItem(LS);
      if (!raw) return;
      JSON.parse(raw).forEach((c) => {
        const p = draft.find((x) => x.id === c.id);
        if (!p) return;
        p.p = c.p; p.combo = c.combo; p.stock = c.stock;
      });
    } catch (_) {}
  }

  /* ── Filtros ─────────────────────────────────────────────── */
  function chips() {
    $('#chips').innerHTML = GRUPOS.map((g) =>
      `<button class="chip" data-g="${esc(g)}" aria-pressed="${g === grupo}">${esc(g)}</button>`).join('');
  }
  $('#chips').addEventListener('click', (e) => {
    const b = e.target.closest('.chip'); if (!b) return;
    grupo = b.dataset.g; soloCambiados = false;
    document.querySelectorAll('.chip').forEach((c) =>
      c.setAttribute('aria-pressed', c.dataset.g === grupo));
    pintar();
  });
  $('#q').addEventListener('input', (e) => {
    busca = e.target.value.trim().toLowerCase(); pintar();
  });
  $('#verCambios').addEventListener('click', () => { soloCambiados = !soloCambiados; pintar(); });

  function visibles() {
    return draft.filter((p) => {
      if (soloCambiados && !cambiado(p)) return false;
      const okG = grupo === 'Todo' || p.g === grupo;
      const okQ = !busca || `${p.n} ${p.c} ${p.v}`.toLowerCase().includes(busca);
      return okG && okQ;
    });
  }

  /* ── Filas ───────────────────────────────────────────────── */
  function pintar() {
    const lista = visibles();
    $('#empty').hidden = lista.length > 0;
    const agot = lista.filter((p) => !p.stock).length;
    $('#count').textContent = `${lista.length} producto${lista.length === 1 ? '' : 's'}`
      + (agot ? ` · ${agot} marcado${agot === 1 ? '' : 's'} como agotado` : '');

    $('#tabla').innerHTML = lista.map((p) => {
      const b = base(p.id);
      const dif = (act, ori) => (act !== ori && ori != null)
        ? `antes ${ori.toFixed(2)}` : '&nbsp;';

      return `
      <div class="fila" data-tocado="${cambiado(p)}" data-agotado="${!p.stock}">
        <div class="fila__n">
          <b>${esc(p.n)}${p.promo ? '<em>Coca gratis</em>' : ''}</b>
          <span>${esc(p.v)} · ${esc(p.c)}</span>
        </div>

        <div class="campo">
          <label for="p-${p.id}">Botella</label>
          <input type="number" id="p-${p.id}" data-precio="${p.id}"
                 value="${p.p.toFixed(2)}" step="0.10" min="0" inputmode="decimal">
          <small>${dif(p.p, b.p)}</small>
        </div>

        <div class="campo">
          <label for="c-${p.id}">Combo</label>
          <input type="number" id="c-${p.id}" data-combo="${p.id}"
                 value="${p.combo != null ? p.combo.toFixed(2) : ''}"
                 step="0.10" min="0" inputmode="decimal"
                 ${p.combo == null ? 'disabled placeholder="—"' : ''}>
          <small>${p.combo != null ? dif(p.combo, b.combo) : '&nbsp;'}</small>
        </div>

        <div class="sw">
          <label>Hay stock</label>
          <button data-stock="${p.id}" aria-pressed="${p.stock}"
                  aria-label="${p.stock ? 'Hay stock' : 'Agotado'} de ${esc(p.n)}"></button>
        </div>
      </div>`;
    }).join('');
  }

  /* ── Edición ─────────────────────────────────────────────── */
  $('#tabla').addEventListener('input', (e) => {
    const el = e.target;
    const id = el.dataset.precio || el.dataset.combo;
    if (!id) return;
    const p = draft.find((x) => x.id === id);
    const v = parseFloat(el.value);

    /* Nielsen #9 · el error se señala donde ocurre */
    if (el.value === '' || isNaN(v) || v <= 0) { el.classList.add('mal'); return; }
    el.classList.remove('mal');

    if (el.dataset.precio) p.p = v; else p.combo = v;

    /* Prevención de errores: el combo no puede costar menos que la botella */
    if (p.combo != null && p.combo <= p.p) {
      const c = $(`[data-combo="${id}"]`);
      if (c) c.classList.add('mal');
    } else {
      const c = $(`[data-combo="${id}"]`);
      if (c) c.classList.remove('mal');
    }

    marcarFila(id);
    refrescar();
  });

  $('#tabla').addEventListener('click', (e) => {
    const b = e.target.closest('[data-stock]');
    if (!b) return;
    const p = draft.find((x) => x.id === b.dataset.stock);
    p.stock = !p.stock;
    ultimo = { tipo: 'stock', id: p.id, valor: !p.stock };
    toast(p.stock ? `${p.n}: hay stock` : `${p.n}: marcado agotado`, true);
    pintar(); refrescar();
  });

  function marcarFila(id) {
    const p = draft.find((x) => x.id === id);
    const fila = $(`[data-precio="${id}"]`)?.closest('.fila');
    if (fila) fila.dataset.tocado = cambiado(p);
    const b = base(id);
    const sp = $(`[data-precio="${id}"]`)?.parentElement.querySelector('small');
    if (sp) sp.innerHTML = p.p !== b.p ? `antes ${b.p.toFixed(2)}` : '&nbsp;';
    const sc = $(`[data-combo="${id}"]`)?.parentElement.querySelector('small');
    if (sc && b.combo != null) sc.innerHTML = p.combo !== b.combo ? `antes ${b.combo.toFixed(2)}` : '&nbsp;';
  }

  /* ── Ajuste masivo ───────────────────────────────────────── */
  function masivo(factor, txt) {
    const lista = visibles();
    const previo = lista.map((p) => ({ id: p.id, p: p.p, combo: p.combo }));
    lista.forEach((p) => {
      p.p = Math.round(p.p * factor * 100) / 100;
      if (p.combo != null) p.combo = Math.round(p.combo * factor * 100) / 100;
    });
    ultimo = { tipo: 'masivo', previo };
    toast(`${txt} en ${lista.length} producto${lista.length === 1 ? '' : 's'}`, true);
    pintar(); refrescar();
  }
  $('#subir').addEventListener('click', () => masivo(1.01, 'Subida de 1%'));
  $('#bajar').addEventListener('click', () => masivo(0.99, 'Bajada de 1%'));

  /* ── Deshacer ────────────────────────────────────────────── */
  function toast(txt, conUndo) {
    $('#toastTxt').textContent = txt;
    $('#undo').hidden = !conUndo;
    $('#toast').dataset.on = 'true';
    clearTimeout(toastT);
    toastT = setTimeout(() => { $('#toast').dataset.on = 'false'; }, 5000);
  }
  $('#undo').addEventListener('click', () => {
    if (!ultimo) return;
    if (ultimo.tipo === 'stock') {
      draft.find((x) => x.id === ultimo.id).stock = ultimo.valor;
    } else if (ultimo.tipo === 'masivo') {
      ultimo.previo.forEach((c) => {
        const p = draft.find((x) => x.id === c.id);
        p.p = c.p; p.combo = c.combo;
      });
    }
    ultimo = null;
    $('#toast').dataset.on = 'false';
    pintar(); refrescar();
  });

  /* ── Estado global ───────────────────────────────────────── */
  function refrescar() {
    const cs = cambios();
    const n = cs.length;

    $('#estado').textContent = n ? `${n} sin publicar` : 'Sin cambios';
    $('#estado').dataset.cambios = n > 0;
    $('#nCambios').textContent = n;
    $('#actionbar').dataset.on = n > 0;
    document.body.style.setProperty('--bar-h', n > 0 ? '76px' : '0px');
    $('#verCambios').hidden = n === 0;
    $('#verCambios').textContent = soloCambiados ? 'Ver todos' : 'Ver solo los cambiados';

    $('#resumen').hidden = n === 0;
    $('#listaCambios').innerHTML = cs.map((p) => {
      const b = base(p.id);
      const partes = [];
      if (b.p !== p.p) partes.push(`botella <s>${money(b.p)}</s> → <i>${money(p.p)}</i>`);
      if (b.combo !== p.combo && p.combo != null) partes.push(`combo <s>${money(b.combo)}</s> → <i>${money(p.combo)}</i>`);
      if (b.stock !== p.stock) partes.push(p.stock ? '<i>vuelve a haber stock</i>' : '<i>agotado</i>');
      return `<div><b>${esc(p.n)}</b> — ${partes.join(' · ')}</div>`;
    }).join('');

    guardar();
  }

  /* ── Publicar ────────────────────────────────────────────── */
  const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

  $('#publicar').addEventListener('click', async () => {
    const malos = draft.filter((p) => p.combo != null && p.combo <= p.p);
    if (malos.length) {
      toast(`Revisa ${malos.length} combo${malos.length === 1 ? '' : 's'}: cuesta menos que la botella`, false);
      return;
    }

    const cs = cambios();
    if (!cs.length) return;

    /* Nielsen #1 · el sistema dice qué está haciendo.
       Sin esto el dueño toca dos veces creyendo que no pasó nada. */
    const btn = $('#publicar');
    const txt = btn.textContent;
    btn.disabled = true;
    btn.textContent = 'Publicando…';

    try {
      const r = await fetch('/panel?handler=Publicar', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
        body: JSON.stringify(cs.map((p) => ({
          id: p.id, precio: p.p, precioCombo: p.combo, stock: p.stock
        })))
      });

      const data = await r.json().catch(() => ({}));

      if (!r.ok) {
        /* El error viene del servidor con el nombre del producto: se muestra
           tal cual en vez de un "algo salió mal" que no ayuda a nadie. */
        toast(data.error || 'No se pudo publicar. Intenta de nuevo.', false);
        return;
      }

      /* Publicado: el borrador ya es el estado real */
      BASE.length = 0;
      draft.forEach((p) => BASE.push({ ...p }));
      try { localStorage.removeItem(LS); } catch (_) {}
      soloCambiados = false;
      pintar(); refrescar();
      toast(`Listo: ${data.publicados} cambio${data.publicados === 1 ? '' : 's'} ya está${data.publicados === 1 ? '' : 'n'} en la web`, false);
    } catch (_) {
      toast('Sin conexión. Tus cambios siguen guardados acá.', false);
    } finally {
      btn.disabled = false;
      btn.textContent = txt;
    }
  });

  /* ── Descartar ───────────────────────────────────────────── */
  $('#descartar').addEventListener('click', () => { $('#modal').dataset.on = 'true'; });
  $('#modalNo').addEventListener('click', () => { $('#modal').dataset.on = 'false'; });
  $('#modalSi').addEventListener('click', () => {
    draft = BASE.map((p) => ({ ...p }));
    try { localStorage.removeItem(LS); } catch (_) {}
    $('#modal').dataset.on = 'false';
    soloCambiados = false;
    pintar(); refrescar();
    toast('Cambios descartados', false);
  });
  document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') $('#modal').dataset.on = 'false';
  });

  /* Nielsen #5 · avisar antes de perder trabajo */
  window.addEventListener('beforeunload', (e) => {
    if (cambios().length) { e.preventDefault(); e.returnValue = ''; }
  });

  /* ══════════════════════════════════════════════════════════
     Pestañas
     Precios queda de entrada: es la tarea diaria. Fotos y banners
     son ocasionales y no deben estorbar el flujo rápido.
     ══════════════════════════════════════════════════════════ */
  $('.tabs')?.addEventListener('click', (e) => {
    const b = e.target.closest('.tab'); if (!b) return;
    document.querySelectorAll('.tab').forEach((x) =>
      x.setAttribute('aria-selected', x === b));
    document.querySelectorAll('.panel').forEach((p) =>
      p.hidden = p.id !== 'panel-' + b.dataset.tab);
    if (b.dataset.tab === 'productos') pintarFotos();
    if (b.dataset.tab === 'banners') pintarBanners();
  });

  /* ══════════════════════════════════════════════════════════
     Fotos de producto
     ══════════════════════════════════════════════════════════ */
  let buscaFoto = '';

  function pintarFotos() {
    const lista = draft.filter((p) => !buscaFoto ||
      `${p.n} ${p.c} ${p.v}`.toLowerCase().includes(buscaFoto));

    if (!$('#fotos')) return;
    $('#fotos').innerHTML = lista.map((p) => `
      <div class="foto" data-id="${p.id}">
        <label class="foto__caja">
          ${p.img
            ? `<img src="${esc(p.img)}" alt="">`
            : '<span class="foto__vacia">Sin foto<br>Toca para subir</span>'}
          <input type="file" accept="image/jpeg,image/png,image/webp" data-foto="${p.id}">
          <span class="foto__prog"></span>
        </label>
        <p class="foto__n">${esc(p.n)}</p>
        <p class="foto__v">${esc(p.v)}</p>
      </div>`).join('');
  }

  $('#qFotos')?.addEventListener('input', (e) => {
    buscaFoto = e.target.value.trim().toLowerCase();
    pintarFotos();
  });

  $('#fotos')?.addEventListener('change', async (e) => {
    const inp = e.target.closest('[data-foto]'); if (!inp || !inp.files[0]) return;
    const id = inp.dataset.foto;
    const caja = inp.closest('.foto');

    caja.dataset.sub = 'true';
    caja.querySelector('.foto__prog').style.width = '70%';

    const fd = new FormData();
    fd.append('id', id);
    fd.append('archivo', inp.files[0]);

    try {
      const r = await fetch('/panel?handler=Imagen', {
        method: 'POST',
        headers: { 'RequestVerificationToken': token() },
        body: fd
      });
      const d = await r.json().catch(() => ({}));

      if (!r.ok) { toast(d.error || 'No se pudo subir la imagen.', false); return; }

      /* Se actualizan las dos copias para que el borrador no marque
         un cambio falso al comparar contra el catálogo publicado. */
      const p1 = draft.find((x) => x.id === id);
      const p2 = BASE.find((x) => x.id === id);
      if (p1) p1.img = d.url;
      if (p2) p2.img = d.url;

      caja.querySelector('.foto__prog').style.width = '100%';
      pintarFotos();
      toast('Foto actualizada y publicada', false);
    } catch (_) {
      toast('Sin conexión. Intenta de nuevo.', false);
    } finally {
      caja.dataset.sub = 'false';
      caja.querySelector('.foto__prog').style.width = '0';
    }
  });

  /* ══════════════════════════════════════════════════════════
     Alta de producto
     ══════════════════════════════════════════════════════════ */
  if ($('#nGrupo')) {
    $('#nGrupo').innerHTML = GRUPOS.filter((g) => g !== 'Todo')
      .map((g) => `<option>${esc(g)}</option>`).join('');
  }

  $('#crearProducto')?.addEventListener('click', async () => {
    const v = (id) => $(id).value.trim();
    const num = (id) => { const n = parseFloat($(id).value); return isNaN(n) ? null : n; };

    document.querySelectorAll('.nuevo input').forEach((i) => i.classList.remove('mal'));

    /* Se valida acá y también en el servidor: esto es para que el
       dueño lo corrija rápido, no para confiar en el navegador. */
    const faltan = [];
    if (!v('#nId')) faltan.push('#nId');
    if (!v('#nNombre')) faltan.push('#nNombre');
    if (!num('#nPrecio')) faltan.push('#nPrecio');
    if (faltan.length) {
      faltan.forEach((s) => $(s).classList.add('mal'));
      $(faltan[0]).focus();
      toast('Completa los campos marcados', false);
      return;
    }

    const btn = $('#crearProducto');
    const txt = btn.textContent;
    btn.disabled = true; btn.textContent = 'Creando…';

    try {
      const r = await fetch('/panel?handler=Nuevo', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
        body: JSON.stringify({
          id: v('#nId'), nombre: v('#nNombre'), presentacion: v('#nPres'),
          categoria: v('#nCat'), grupo: $('#nGrupo').value,
          precio: num('#nPrecio'), precioCombo: num('#nCombo'),
          comboAcompanante: v('#nAco'), comboHielo: v('#nHielo'),
          promo: $('#nPromo').checked, color: ''
        })
      });
      const d = await r.json().catch(() => ({}));

      if (!r.ok) { toast(d.error || 'No se pudo crear.', false); return; }

      toast('Producto creado. Recarga para verlo en la lista.', false);
      document.querySelectorAll('.nuevo input').forEach((i) => {
        if (i.type === 'checkbox') i.checked = false; else i.value = '';
      });
    } catch (_) {
      toast('Sin conexión. Intenta de nuevo.', false);
    } finally {
      btn.disabled = false; btn.textContent = txt;
    }
  });

  /* ══════════════════════════════════════════════════════════
     Banners · máximo 5
     ══════════════════════════════════════════════════════════ */
  let banners = (CONFIG.banners || []).slice(0, 5).map((b) => ({ ...b }));

  function pintarBanners() {
    /* Siempre se muestran 5 ranuras: se ve de una cuántas quedan
       libres, sin tener que contar ni leer una advertencia. */
    const filas = [];
    for (let i = 0; i < 5; i++) {
      const b = banners[i] || { img: '', alt: '', url: '' };
      filas.push(`
        <div class="banner-adm" data-i="${i}">
          <label class="banner-adm__caja">
            ${b.img ? `<img src="${esc(b.img)}" alt="">` : `<span>Banner ${i + 1}<br>Toca para subir</span>`}
            <input type="file" accept="image/jpeg,image/png,image/webp" data-banner="${i}">
          </label>
          <div class="banner-adm__campos">
            <input data-alt="${i}" value="${esc(b.alt || '')}" placeholder="Descripción para accesibilidad" maxlength="120">
            <input data-url="${i}" value="${esc(b.url || '')}" placeholder="Enlace al tocar (opcional): #catalogo" maxlength="200">
          </div>
          <button class="banner-adm__x" data-quitar="${i}" aria-label="Quitar banner ${i + 1}">✕</button>
        </div>`);
    }
    if ($('#bannersAdm')) $('#bannersAdm').innerHTML = filas.join('');
  }

  $('#bannersAdm')?.addEventListener('change', async (e) => {
    const inp = e.target.closest('[data-banner]');
    if (inp && inp.files[0]) {
      const i = +inp.dataset.banner;
      const fd = new FormData();
      fd.append('indice', i);
      fd.append('archivo', inp.files[0]);
      try {
        const r = await fetch('/panel?handler=Banner', {
          method: 'POST', headers: { 'RequestVerificationToken': token() }, body: fd
        });
        const d = await r.json().catch(() => ({}));
        if (!r.ok) { toast(d.error || 'No se pudo subir el banner.', false); return; }
        banners[i] = { ...(banners[i] || {}), img: d.url };
        pintarBanners();
        toast('Banner subido. Recuerda guardar.', false);
      } catch (_) { toast('Sin conexión. Intenta de nuevo.', false); }
      return;
    }
    const alt = e.target.closest('[data-alt]');
    if (alt) { const i = +alt.dataset.alt; if (banners[i]) banners[i].alt = alt.value; }
    const url = e.target.closest('[data-url]');
    if (url) { const i = +url.dataset.url; if (banners[i]) banners[i].url = url.value; }
  });

  $('#bannersAdm')?.addEventListener('click', (e) => {
    const x = e.target.closest('[data-quitar]'); if (!x) return;
    const i = +x.dataset.quitar;
    if (!banners[i]?.img) return;
    banners.splice(i, 1);   // los siguientes suben una posición
    pintarBanners();
    toast('Banner quitado. Recuerda guardar.', false);
  });

  $('#guardarBanners')?.addEventListener('click', async () => {
    const btn = $('#guardarBanners');
    const txt = btn.textContent;
    btn.disabled = true; btn.textContent = 'Guardando…';
    try {
      const r = await fetch('/panel?handler=Banners', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
        body: JSON.stringify(banners.filter((b) => b.img))
      });
      const d = await r.json().catch(() => ({}));
      toast(r.ok ? 'Banners publicados' : (d.error || 'No se pudo guardar.'), false);
    } catch (_) {
      toast('Sin conexión. Intenta de nuevo.', false);
    } finally {
      btn.disabled = false; btn.textContent = txt;
    }
  });

  /* ── Arranque ────────────────────────────────────────────── */
  recuperar();
  chips();
  pintar();
  refrescar();
})();
