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
        } catch (_) { }
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
        } catch (_) { }
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

        etiquetasMasivo();
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

    /* El ajuste opera sobre lo que está filtrado, no sobre el catálogo
       entero, y el botón tiene que decirlo. Con un filtro activo, un
       botón que dice "todos" hace creer que se ajustaron los 55 cuando
       en realidad se tocaron 11. */
    function etiquetasMasivo() {
        const n = visibles().length;
        const alcance = n === draft.length ? 'todos' : `los ${n} de la lista`;
        $('#subir').textContent = `Subir ${alcance} +1%`;
        $('#bajar').textContent = `Bajar ${alcance} −1%`;
    }

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
        const cs = cambios();
        if (!cs.length) return;

        /* Se valida lo que se va a publicar, no el catálogo entero: un
           combo mal guardado hace tiempo en otro producto no tiene por qué
           bloquear un cambio de stock de hoy. */
        const malos = cs.filter((p) => p.combo != null && p.combo <= p.p);
        if (malos.length) {
            toast(`Revisa ${malos.length} combo${malos.length === 1 ? '' : 's'}: cuesta menos que la botella`, false);
            return;
        }

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
            try { localStorage.removeItem(LS); } catch (_) { }
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
        try { localStorage.removeItem(LS); } catch (_) { }
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
    /* Cada pestaña dice qué es y —lo que más confunde— cuándo sale a la web.
       Solo Precios usa borrador; el resto publica al guardar. Sin decirlo,
       el dueño busca un botón "Publicar" que en esas pantallas no existe. */
    const AYUDA = {
        precios: ['Precios y stock',
            'Cambia lo que necesites. Se guardan como borrador en este dispositivo y ' +
            '<b>recién aparecen en la web cuando le das a Publicar</b>.'],
        productos: ['Productos y fotos',
            'Alta, edición y baja de productos. <b>Lo que guardes acá sale a la web al momento</b>, ' +
            'no pasa por el borrador de precios.'],
        delivery: ['Delivery por distrito',
            'El costo de cada distrito se suma al total del pedido del cliente. ' +
            '<b>Sale a la web apenas guardas.</b>'],
        banners: ['Banners',
            'Hasta 5 imágenes para el carrusel de la página. <b>Salen a la web apenas guardas.</b>']
    };

    $('.tabs')?.addEventListener('click', (e) => {
        const b = e.target.closest('.tab'); if (!b) return;
        document.querySelectorAll('.tab').forEach((x) =>
            x.setAttribute('aria-selected', x === b));
        document.querySelectorAll('.panel').forEach((p) =>
            p.hidden = p.id !== 'panel-' + b.dataset.tab);

        const t = AYUDA[b.dataset.tab];
        if (t) { $('#ayudaT').textContent = t[0]; $('#ayudaP').innerHTML = t[1]; }

        if (b.dataset.tab === 'productos') pintarFotos();
        if (b.dataset.tab === 'delivery') pintarZonas();
        if (b.dataset.tab === 'banners') pintarBanners();
    });

    /* ══════════════════════════════════════════════════════════
       Delivery · costo por distrito

       El costo entra en el total que ve el cliente antes de enviar el
       pedido, así que un número mal puesto acá es un precio equivocado
       en WhatsApp. Se valida en el campo y otra vez en el servidor.
       ══════════════════════════════════════════════════════════ */
    let zonas = (CONFIG.zonas || []).map((z) => ({ ...z }));
    let buscaZona = '';

    /* Un distrito desmarcado no se borra de la lista en memoria: se marca.
       Así se puede volver a activar sin perder el costo que tenía. */
    zonas.forEach((z) => { z.on = true; });

    function zonasVisibles() {
        return zonas.filter((z) => !buscaZona || z.n.toLowerCase().includes(buscaZona));
    }

    function pintarZonas() {
        if (!$('#zonasAdm')) return;

        const lista = zonasVisibles();
        const activos = zonas.filter((z) => z.on).length;
        $('#zCount').textContent =
            `${activos} distrito${activos === 1 ? '' : 's'} con reparto`
            + (lista.length !== zonas.length ? ` · ${lista.length} en la búsqueda` : '');

        $('#zonasAdm').innerHTML = lista.map((z) => {
            const i = zonas.indexOf(z);
            return `
      <div class="zona-adm" data-i="${i}" data-off="${!z.on}">
        <label class="zona-adm__on">
          <input type="checkbox" data-zon="${i}" ${z.on ? 'checked' : ''}
                 aria-label="Repartir a ${esc(z.n)}">
          <b>${esc(z.n)}</b>
        </label>
        <div class="campo">
          <label for="zc-${i}">Costo</label>
          <input type="number" id="zc-${i}" data-zcosto="${i}" value="${(z.c ?? 0).toFixed(2)}"
                 step="0.50" min="0" inputmode="decimal" ${z.on ? '' : 'disabled'}>
        </div>
        <div class="campo">
          <label for="zt-${i}">Tiempo</label>
          <input id="zt-${i}" data-ztiempo="${i}" value="${esc(z.t || '')}"
                 placeholder="30-45 min" maxlength="30" ${z.on ? '' : 'disabled'}>
        </div>
      </div>`;
        }).join('');
    }

    $('#qZonas')?.addEventListener('input', (e) => {
        buscaZona = e.target.value.trim().toLowerCase();
        pintarZonas();
    });

    $('#zonasAdm')?.addEventListener('input', (e) => {
        const el = e.target;

        const ic = el.dataset.zcosto;
        if (ic !== undefined) {
            const v = parseFloat(el.value);
            /* Nielsen #9 · el error se marca en el campo exacto */
            if (el.value === '' || isNaN(v) || v < 0) { el.classList.add('mal'); return; }
            el.classList.remove('mal');
            zonas[+ic].c = v;
            return;
        }

        const it = el.dataset.ztiempo;
        if (it !== undefined) zonas[+it].t = el.value;
    });

    $('#zonasAdm')?.addEventListener('change', (e) => {
        const i = e.target.dataset.zon;
        if (i === undefined) return;
        zonas[+i].on = e.target.checked;
        pintarZonas();
    });

    /* Cuando sube la gasolina, el costo sube parejo en los 43. Sin esto
       son 43 campos a mano y el dueño termina no actualizando ninguno. */
    $('#zAplicar')?.addEventListener('click', () => {
        const v = parseFloat($('#zCostoTodos').value);
        if (isNaN(v) || v < 0) {
            $('#zCostoTodos').classList.add('mal');
            toast('Escribe un costo válido para aplicar', false);
            return;
        }
        $('#zCostoTodos').classList.remove('mal');

        /* Respeta la búsqueda, igual que el ajuste masivo de precios. */
        const lista = zonasVisibles().filter((z) => z.on);
        lista.forEach((z) => { z.c = v; });
        pintarZonas();
        toast(`S/ ${v.toFixed(2)} aplicado a ${lista.length} distrito${lista.length === 1 ? '' : 's'}`, false);
    });

    $('#zReponer')?.addEventListener('click', () => {
        let nuevos = 0;
        DISTRITOS_LIMA.forEach((n) => {
            const y = zonas.find((z) => z.n.toLowerCase() === n.toLowerCase());
            if (y) { y.on = true; return; }
            zonas.push({ n, c: 10, t: '', on: true });
            nuevos++;
        });
        pintarZonas();
        toast(nuevos ? `${nuevos} distrito${nuevos === 1 ? '' : 's'} agregado${nuevos === 1 ? '' : 's'}`
                     : 'Ya estaban los 43, se reactivaron los desmarcados', false);
    });

    $('#guardarZonas')?.addEventListener('click', async () => {
        if ($('#zonasAdm').querySelector('.mal')) {
            toast('Revisa los costos marcados en rojo', false);
            return;
        }

        const btn = $('#guardarZonas');
        const txt = btn.textContent;
        btn.disabled = true; btn.textContent = 'Guardando…';

        try {
            const r = await fetch('/panel?handler=Zonas', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
                body: JSON.stringify(zonas.filter((z) => z.on).map((z) => ({ n: z.n, c: z.c, t: z.t })))
            });
            const d = await r.json().catch(() => ({}));
            toast(r.ok ? 'Zonas publicadas: ya están en la web' : (d.error || 'No se pudo guardar.'), false);
        } catch (_) {
            toast('Sin conexión. Intenta de nuevo.', false);
        } finally {
            btn.disabled = false; btn.textContent = txt;
        }
    });

    /* ══════════════════════════════════════════════════════════
       Fotos y edición de productos
       ══════════════════════════════════════════════════════════ */
    let buscaFoto = '';
    let editando = null;   // id en edición, o null si se está creando

    function pintarFotos() {
        if (!$('#fotos')) return;
        const lista = TODOS.filter((p) => !buscaFoto ||
            `${p.n} ${p.c} ${p.v} ${p.id}`.toLowerCase().includes(buscaFoto));

        $('#fotos').innerHTML = lista.map((p) => `
      <div class="foto" data-id="${p.id}" data-baja="${!p.activo}">
        <label class="foto__caja">
          ${p.img ? `<img src="${esc(p.img)}" alt="">`
                : '<span class="foto__vacia">Sin foto<br>Toca para subir</span>'}
          <input type="file" accept="image/jpeg,image/png,image/webp" data-foto="${p.id}">
          <span class="foto__prog"></span>
        </label>
        <p class="foto__n">${esc(p.n)}</p>
        <p class="foto__v">${esc(p.v)}</p>
        ${p.activo ? '' : '<span class="foto__baja">De baja</span>'}
        <button class="foto__acc" data-editar="${p.id}">Editar</button>
      </div>`).join('');
    }

    $('#qFotos')?.addEventListener('input', (e) => {
        buscaFoto = e.target.value.trim().toLowerCase();
        pintarFotos();
    });

    /* ── Subida de foto ─────────────────────────────────────── */
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
                method: 'POST', headers: { 'RequestVerificationToken': token() }, body: fd
            });
            const d = await r.json().catch(() => ({}));
            if (!r.ok) { toast(d.error || 'No se pudo subir la imagen.', false); return; }

            /* Se actualizan las tres copias para que el borrador no marque un
               cambio falso al comparar contra el catálogo publicado. */
            [TODOS, draft, BASE].forEach((col) => {
                const x = col.find((y) => y.id === id);
                if (x) x.img = d.url;
            });

            pintarFotos();
            toast('Foto actualizada y publicada', false);
        } catch (_) {
            toast('Sin conexión. Intenta de nuevo.', false);
        } finally {
            caja.dataset.sub = 'false';
            caja.querySelector('.foto__prog').style.width = '0';
        }
    });

    /* ── Editor ─────────────────────────────────────────────── */
    const campos = {
        id: '#nId', n: '#nNombre', v: '#nPres', c: '#nCat', g: '#nGrupo',
        orden: '#nOrden', p: '#nPrecio', combo: '#nCombo',
        aco: '#nAco', hie: '#nHielo'
    };

    if ($('#nGrupo')) {
        $('#nGrupo').innerHTML = GRUPOS.filter((g) => g !== 'Todo')
            .map((g) => `<option>${esc(g)}</option>`).join('');
    }

    function abrirEditor(p) {
        editando = p ? p.id : null;

        $('#editorT').textContent = p ? 'Editar producto' : 'Agregar producto';
        Object.values(campos).forEach((s) => { const e = $(s); if (e) e.classList.remove('mal'); });

        $('#nId').value = p?.id ?? '';
        $('#nNombre').value = p?.n ?? '';
        $('#nPres').value = p?.v ?? '';
        $('#nCat').value = p?.c ?? '';
        $('#nGrupo').value = p?.g ?? GRUPOS[1];
        $('#nOrden').value = p?.orden ?? '';
        $('#nPrecio').value = p?.p ?? '';
        $('#nCombo').value = p?.combo ?? '';
        $('#nAco').value = p?.aco ?? '';
        $('#nHielo').value = p?.hie ?? '';
        $('#nPromo').checked = !!p?.promo;

        /* El identificador está atado a la imagen y a la auditoría:
           cambiarlo dejaría huérfanas a las dos. */
        $('#nId').disabled = !!p;
        $('#nIdNota').textContent = p
            ? 'No se puede cambiar: está ligado a la foto y al historial'
            : 'Solo letras, números y guiones';

        $('#darBaja').hidden = !p;
        $('#bajaNota').hidden = !p;
        if (p) $('#darBaja').textContent = p.activo ? 'Dar de baja' : 'Reactivar';

        $('#editor').dataset.on = 'true';
        setTimeout(() => (p ? $('#nNombre') : $('#nId')).focus(), 80);
    }

    function cerrarEditor() {
        $('#editor').dataset.on = 'false';
        editando = null;
    }

    $('#abrirNuevo')?.addEventListener('click', () => abrirEditor(null));
    $('#cerrarEditor')?.addEventListener('click', cerrarEditor);
    $('#cancelarEditor')?.addEventListener('click', cerrarEditor);
    $('#editor')?.addEventListener('click', (e) => { if (e.target.id === 'editor') cerrarEditor(); });
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && $('#editor')?.dataset.on === 'true') cerrarEditor();
    });

    $('#fotos')?.addEventListener('click', (e) => {
        const b = e.target.closest('[data-editar]'); if (!b) return;
        abrirEditor(TODOS.find((p) => p.id === b.dataset.editar));
    });

    /* ── Guardar (crear o editar) ───────────────────────────── */
    $('#guardarProducto')?.addEventListener('click', async () => {
        const v = (s) => $(s).value.trim();
        const num = (s) => { const n = parseFloat($(s).value); return isNaN(n) ? null : n; };

        Object.values(campos).forEach((s) => { const e = $(s); if (e) e.classList.remove('mal'); });

        const faltan = [];
        if (!editando && !v('#nId')) faltan.push('#nId');
        if (!v('#nNombre')) faltan.push('#nNombre');
        if (!num('#nPrecio')) faltan.push('#nPrecio');
        if (faltan.length) {
            faltan.forEach((s) => $(s).classList.add('mal'));
            $(faltan[0]).focus();
            toast('Completa los campos marcados', false);
            return;
        }

        const cuerpo = {
            id: editando ?? v('#nId'),
            nombre: v('#nNombre'), presentacion: v('#nPres'),
            categoria: v('#nCat'), grupo: $('#nGrupo').value,
            precio: num('#nPrecio'), precioCombo: num('#nCombo'),
            comboAcompanante: v('#nAco'), comboHielo: v('#nHielo'),
            promo: $('#nPromo').checked
        };
        if (editando) cuerpo.orden = parseInt($('#nOrden').value, 10) || 0;
        else cuerpo.color = '';

        const btn = $('#guardarProducto');
        const txt = btn.textContent;
        btn.disabled = true; btn.textContent = 'Guardando…';

        try {
            const r = await fetch(`/panel?handler=${editando ? 'Editar' : 'Nuevo'}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
                body: JSON.stringify(cuerpo)
            });
            const d = await r.json().catch(() => ({}));
            if (!r.ok) { toast(d.error || 'No se pudo guardar.', false); return; }

            if (editando) {
                const p = TODOS.find((x) => x.id === editando);
                Object.assign(p, {
                    n: cuerpo.nombre, v: cuerpo.presentacion, c: cuerpo.categoria,
                    g: cuerpo.grupo, p: cuerpo.precio, combo: cuerpo.precioCombo,
                    aco: cuerpo.comboAcompanante, hie: cuerpo.comboHielo,
                    promo: cuerpo.promo, orden: cuerpo.orden
                });
                toast('Producto actualizado y publicado', false);
            } else {
                TODOS.push({
                    ...cuerpo, id: d.id, activo: true, stock: true, img: '',
                    aco: cuerpo.comboAcompanante, hie: cuerpo.comboHielo
                });
                toast('Producto creado. Recarga para editar sus precios.', false);
            }

            pintarFotos();
            cerrarEditor();
        } catch (_) {
            toast('Sin conexión. Intenta de nuevo.', false);
        } finally {
            btn.disabled = false; btn.textContent = txt;
        }
    });

    /* ── Dar de baja o reactivar ────────────────────────────── */
    $('#darBaja')?.addEventListener('click', async () => {
        if (!editando) return;
        const p = TODOS.find((x) => x.id === editando);
        const nuevo = !p.activo;

        /* Confirmación solo al dar de baja: reactivar no rompe nada. */
        if (!nuevo && !confirm(`¿Sacar "${p.n}" del catálogo?\n\nConserva su historial y puedes reactivarlo cuando quieras.`))
            return;

        try {
            const r = await fetch(`/panel?handler=Activo&id=${encodeURIComponent(editando)}&activo=${nuevo}`, {
                method: 'POST', headers: { 'RequestVerificationToken': token() }
            });
            const d = await r.json().catch(() => ({}));
            if (!r.ok) { toast(d.error || 'No se pudo cambiar el estado.', false); return; }

            p.activo = nuevo;
            pintarFotos();
            cerrarEditor();
            toast(nuevo ? 'Producto reactivado' : 'Producto dado de baja', false);
        } catch (_) {
            toast('Sin conexión. Intenta de nuevo.', false);
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
