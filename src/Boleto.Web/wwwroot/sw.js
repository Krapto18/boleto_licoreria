/* Service worker — catálogo disponible sin señal.
   A las 3 a.m. con mala cobertura, la web igual abre. */
/* La versión SE SUBE cada vez que cambia esta lista o alguno de sus
   archivos: al activarse, el worker borra las cachés que no coinciden.
   Sin subirla, quien ya visitó el sitio seguiría viendo los archivos
   viejos —el logo anterior, por ejemplo— hasta vaciar el navegador. */
const CACHE = 'boleto-v2';
/* Rutas de ASP.NET, no archivos sueltos. El catálogo ya viene dentro
   del HTML de '/', así que cachearlo alcanza para funcionar sin señal.
   Las piezas de marca entran porque salen en la primera pantalla: sin
   ellas, sin señal se abre una web sin logo. */
const ASSETS = [
  '/', '/css/styles.css', '/js/app.js', '/manifest.webmanifest',
  '/assets/favicon.svg',
  '/assets/marca/logo-oscuro.svg', '/assets/marca/isotipo-claro.svg',
  '/assets/iconos/carrito.svg', '/assets/iconos/hielo.svg',
  '/assets/iconos/acompanante.svg'
];

self.addEventListener('install', (e) => {
  /* addAll aborta todo si un solo recurso falla. Se cachea uno por uno
     para que un 404 aislado no deje la PWA sin instalar. */
  e.waitUntil(
    caches.open(CACHE)
      .then((c) => Promise.allSettled(ASSETS.map((a) => c.add(a))))
      .then(() => self.skipWaiting())
  );
});

self.addEventListener('activate', (e) => {
  e.waitUntil(caches.keys()
    .then((ks) => Promise.all(ks.filter((k) => k !== CACHE).map((k) => caches.delete(k))))
    .then(() => self.clients.claim()));
});

/* El panel nunca se cachea: los precios deben venir frescos */
self.addEventListener('fetch', (e) => {
  if (e.request.method !== 'GET') return;
  const url = new URL(e.request.url);
  if (/^\/(panel|cuenta|api)/.test(url.pathname)) return;

  e.respondWith(
    fetch(e.request)
      .then((r) => {
        const copia = r.clone();
        caches.open(CACHE).then((c) => c.put(e.request, copia)).catch(() => {});
        return r;
      })
      .catch(() => caches.match(e.request).then((r) => r || caches.match('/')))
  );
});
