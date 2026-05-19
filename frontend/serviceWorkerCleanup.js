(function () {
  if (!("serviceWorker" in navigator)) return;

  window.addEventListener("load", async () => {
    try {
      const registrations = await navigator.serviceWorker.getRegistrations();

      await Promise.all(
        registrations.map((registration) => registration.unregister()),
      );
    } catch {
      // Service worker cleanup is best-effort for local demo use.
    }
  });
})();
