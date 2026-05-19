// Local demo cleanup worker.
//
// A previous frontend build registered a service worker at this scope. This
// static demo does not need offline/PWA caching, so this replacement worker
// immediately takes control and unregisters itself. It intentionally does not
// install any fetch handler, which means navigation and API requests fall back
// to the browser's normal network behavior.

self.addEventListener("install", () => {
  self.skipWaiting();
});

self.addEventListener("activate", (event) => {
  event.waitUntil(
    (async () => {
      await self.registration.unregister();

      const clients = await self.clients.matchAll({
        includeUncontrolled: true,
        type: "window",
      });

      await Promise.allSettled(clients.map((client) => client.navigate(client.url)));
    })(),
  );
});
