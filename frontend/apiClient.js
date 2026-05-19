(function () {
  function apiBaseUrl() {
    const configuredUrl = window.VANILLA_API_BASE_URL || "http://localhost:12345";
    return configuredUrl.replace(/\/$/, "");
  }

  async function request(path, options = {}) {
    const response = await fetch(`${apiBaseUrl()}${path}`, {
      ...options,
      headers: {
        Accept: "application/json, text/plain, */*",
        ...(options.body ? { "Content-Type": "application/json" } : {}),
        ...options.headers,
      },
    });

    const body = await response.text();
    if (!response.ok) {
      throw new Error(readErrorMessage(body, response.status));
    }

    if (!body) return null;

    const contentType = response.headers.get("content-type") || "";
    return contentType.includes("application/json") ? JSON.parse(body) : body;
  }

  function readErrorMessage(body, status) {
    if (!body) return `HTTP ${status}`;

    try {
      const problem = JSON.parse(body);
      if (problem.errors) {
        return Object.values(problem.errors).flat().join(" ");
      }

      return problem.detail || problem.title || problem.message || `HTTP ${status}`;
    } catch {
      return body;
    }
  }

  function postJson(path, body) {
    return request(path, {
      method: "POST",
      body: JSON.stringify(body),
    });
  }

  function getHealth() {
    return request("/health");
  }

  function getCustomers() {
    return request("/api/customers");
  }

  async function getCustomer(customerId) {
    const ledger = await getCustomerLedger(customerId);
    return ledger.customer;
  }

  function createCustomer({ name, openingBalance = 0 }) {
    return postJson("/api/customers", {
      name,
      email: null,
      phone: null,
      notes: null,
      openingBalance,
    });
  }

  function getLedgerEntries() {
    return request("/api/ledger/entries");
  }

  function getCustomerLedger(customerId) {
    return request(`/api/customers/${encodeURIComponent(customerId)}/ledger`);
  }

  function recordSale({ customerId, amount, paidNow = 0 }) {
    return postJson("/api/orders", {
      customerId,
      amount,
      notes: null,
    }).then(async (sale) => {
      if (paidNow > 0) {
        await recordPayment({
          customerId,
          amount: paidNow,
          note: "Paid at sale",
        });
      }

      return sale;
    });
  }

  function recordPayment({ customerId, amount, note = null }) {
    return postJson("/api/payments", {
      customerId,
      amount,
      notes: note,
    });
  }

  function getDashboardSummary() {
    return request("/api/dashboard/summary");
  }

  function clearLedgerData() {
    return request("/api/ledger", { method: "DELETE" });
  }

  function deleteCustomer(customerId) {
    return request(`/api/customers/${encodeURIComponent(customerId)}`, { method: "DELETE" });
  }

  window.vanillaApi = {
    apiBaseUrl,
    clearLedgerData,
    createCustomer,
    deleteCustomer,
    getCustomer,
    getCustomerLedger,
    getCustomers,
    getDashboardSummary,
    getHealth,
    getLedgerEntries,
    recordPayment,
    recordSale,
  };
})();
