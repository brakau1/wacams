// Unregister all service workers and clear caches
self.addEventListener('install', event => {
  self.skipWaiting();
});

self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys().then(cacheNames => {
      return Promise.all(
        cacheNames.map(cacheName => caches.delete(cacheName))
      );
    }).then(() => self.clients.claim())
  );
});

// Unregister this service worker after activation
navigator.serviceWorker.getRegistration().then(reg => {
  if (reg) reg.unregister();
});
