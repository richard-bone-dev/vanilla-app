(function () {
  function apiBaseUrl() {
    const configuredUrl = window.VANILLA_API_BASE_URL;
    if (!configuredUrl) {
      throw new Error("VANILLA_API_BASE_URL is not configured");
    }

    return configuredUrl.replace(/\/$/, "");
  }

  async function getHealth() {
    const response = await fetch(`${apiBaseUrl()}/health`, {
      headers: {
        Accept: "application/json, text/plain, */*",
      },
    });

    const body = await response.text();
    if (!response.ok) {
      throw new Error(body || `HTTP ${response.status}`);
    }

    return body || response.statusText || "OK";
  }

  window.vanillaApi = {
    apiBaseUrl,
    getHealth,
  };
})();
