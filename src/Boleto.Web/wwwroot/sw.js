/* Service worker — catálogo disponible sin señal.
   A las 3 a.m. con mala cobertura, la web igual abre. */
const CACHE = 'boleto-v1';
/* Rutas de ASP.NET, no archivos sueltos. El catálogo ya viene dentro
   del HTML de '/', así que cachearlo alcanza para funcionar sin señal. */
const ASSETS = [
  '/', '/css/styles.css', '/js/app.js',
  '/assets/favicon.svg', '/assets/logo.svg', '/manifest.webmanifest'
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
