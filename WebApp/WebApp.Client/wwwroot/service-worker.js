// Version-based cache naming for easy updates
const CACHE_VERSION = '2026-08-13-001';
const CACHE_NAME = `hgs-portal-${CACHE_VERSION}`;
const SHELL_CACHE = `hgs-shell-${CACHE_VERSION}`;

// Cache only shell assets (no API data)
const SHELL_URLS = [
    './',
    './favicon.png',
    './icon-192.png',
    './icon-512.png',
    './lib/bootstrap/dist/css/bootstrap.min.css',
    './lib/bootstrap/dist/js/bootstrap.bundle.min.js',
    './lib/bootstrap-icons/font/bootstrap-icons.css',
    './_content/Blazor.Bootstrap/blazor.bootstrap.css',
    './_content/Blazor.Bootstrap/blazor.bootstrap.js',
    './app.css',
    './WebApp.styles.css'
];

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(SHELL_CACHE)
            .then(cache => cache.addAll(SHELL_URLS))
    );
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames.map(cacheName => {
                    // Delete old caches that start with 'hgs-portal-' or 'hgs-shell-' but are not current version
                    if ((cacheName.startsWith('hgs-portal-') || cacheName.startsWith('hgs-shell-')) &&
                        cacheName !== SHELL_CACHE && cacheName !== CACHE_NAME) {
                        console.log('Deleting old cache:', cacheName);
                        return caches.delete(cacheName);
                    }
                })
            );
        })
    );
    self.clients.claim();
});

self.addEventListener('fetch', event => {
    const url = new URL(event.request.url);
    
    // Don't cache API calls - always go to network
    if (url.pathname.startsWith('/api/') || url.pathname.startsWith('/ApiCore/')) {
        event.respondWith(
            fetch(event.request).catch(error => {
                console.log('API fetch failed:', error);
                throw error;
            })
        );
        return;
    }
    
    // Don't cache SignalR/WebSocket
    if (url.pathname.startsWith('/_blazor/') || url.pathname.includes('negotiate')) {
        event.respondWith(
            fetch(event.request).catch(error => {
                console.log('SignalR/WebSocket fetch failed:', error);
                throw error;
            })
        );
        return;
    }
    
    // Network-first for shell assets (HTML, CSS, JS, icons)
    // Always try network first to get latest version, fallback to cache if offline
    if (SHELL_URLS.some(shellUrl => url.pathname.includes(shellUrl.replace('./', '')))) {
        event.respondWith(
            (async () => {
                try {
                    const response = await fetch(event.request);
                    if (response.ok) {
                        const cache = await caches.open(SHELL_CACHE);
                        await cache.put(event.request, response.clone());
                    }
                    return response;
                }
                catch {
                    return caches.match(event.request);
                }
            })()
        );
        return;
    }
    
    // Network-first for everything else
    event.respondWith(
        (async () => {
            try {
                const response = await fetch(event.request);
                if (response.ok) {
                    const cache = await caches.open(CACHE_NAME);
                    await cache.put(event.request, response.clone());
                }
                return response;
            }
            catch {
                return caches.match(event.request);
            }
        })()
    );
});