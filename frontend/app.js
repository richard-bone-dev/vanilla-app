const state = {
  customers: [],
  entries: [],
  summary: emptySummary(),
  loading: true,
  mutating: false,
  error: "",
};

let mode = "sale";
let activeFilter = "all";
let selectedCustomerId = "";
let selectedCustomerName = "";
let selectedCustomerSnapshot = null;
let customerSuggestions = [];
let customerSearchLoading = false;
let customerSearchError = "";
let customerSearchRequestId = 0;

const els = {
  addCustomer: document.querySelector("#addCustomer"),
  apiBaseUrl: document.querySelector("#apiBaseUrl"),
  apiStatus: document.querySelector("#apiStatus"),
  cancelCustomer: document.querySelector("#cancelCustomer"),
  checkApi: document.querySelector("#checkApi"),
  clearData: document.querySelector("#clearData"),
  closeFullApp: document.querySelector("#closeFullApp"),
  customerDialog: document.querySelector("#customerDialog"),
  customerForm: document.querySelector("#customerForm"),
  customerList: document.querySelector("#customerList"),
  customerName: document.querySelector("#customerName"),
  customerSuggestions: document.querySelector("#customerSuggestions"),
  dismissCustomer: document.querySelector("#dismissCustomer"),
  entryList: document.querySelector("#entryList"),
  exportBox: document.querySelector("#exportBox"),
  exportData: document.querySelector("#exportData"),
  fullApp: document.querySelector("#fullApp"),
  heroCustomers: document.querySelector("#heroCustomers"),
  heroOutstanding: document.querySelector("#heroOutstanding"),
  heroToday: document.querySelector("#heroToday"),
  metricOutstanding: document.querySelector("#metricOutstanding"),
  metricPayments: document.querySelector("#metricPayments"),
  metricSales: document.querySelector("#metricSales"),
  newCustomerQuick: document.querySelector("#newCustomerQuick"),
  openFullApp: document.querySelector("#openFullApp"),
  openingBalance: document.querySelector("#openingBalance"),
  paidNowField: document.querySelector("#paidNowField"),
  quickAmount: document.querySelector("#quickAmount"),
  quickCustomer: document.querySelector("#quickCustomer"),
  quickForm: document.querySelector("#quickForm"),
  quickPaidNow: document.querySelector("#quickPaidNow"),
  quickSubmit: document.querySelector("#quickForm button[type='submit']"),
  quickTitle: document.querySelector("#quickTitle"),
  selectedCustomerBalance: document.querySelector("#selectedCustomerBalance"),
  selectedCustomerName: document.querySelector("#selectedCustomerName"),
  selectedCustomerStatus: document.querySelector("#selectedCustomerStatus"),
  submitLabel: document.querySelector("#submitLabel"),
};

bindEvents();
render();
loadData();

function bindEvents() {
  document.querySelectorAll(".mode-option").forEach((button) => {
    button.addEventListener("click", () => setMode(button.dataset.mode));
  });

  document.querySelectorAll(".filter-chip").forEach((button) => {
    button.addEventListener("click", () => {
      activeFilter = button.dataset.filter;
      renderEntries();
      renderFilters();
    });
  });

  els.quickForm.addEventListener("submit", recordQuickEntry);
  els.quickCustomer.addEventListener("input", handleCustomerSearchInput);
  els.quickCustomer.addEventListener("focus", renderCustomerSuggestions);
  els.quickCustomer.addEventListener("keydown", handleCustomerSearchKeys);
  els.openFullApp.addEventListener("click", openFullApp);
  els.closeFullApp.addEventListener("click", closeFullApp);
  els.addCustomer.addEventListener("click", openCustomerDialog);
  els.checkApi.addEventListener("click", checkApiHealth);
  els.newCustomerQuick.addEventListener("click", openCustomerDialog);
  els.cancelCustomer.addEventListener("click", closeCustomerDialog);
  els.dismissCustomer.addEventListener("click", closeCustomerDialog);
  els.customerForm.addEventListener("submit", saveCustomer);
  els.exportData.addEventListener("click", exportData);
  els.clearData.addEventListener("click", clearData);

  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape" && els.fullApp.classList.contains("is-open")) {
      closeFullApp();
    }

    if (event.key === "Escape" && document.activeElement === els.quickCustomer) {
      hideCustomerSuggestions();
    }
  });

  document.addEventListener("click", (event) => {
    if (!event.target.closest(".customer-search")) {
      hideCustomerSuggestions();
    }
  });
}

async function loadData({ preserveCustomerId = selectedCustomerId, quiet = false } = {}) {
  state.loading = true;
  state.error = "";
  render(preserveCustomerId);

  if (!quiet) {
    setApiStatus("Loading API data...");
  }

  try {
    const [customers, entries, summary] = await Promise.all([
      window.vanillaApi.getCustomers(),
      window.vanillaApi.getLedgerEntries(),
      window.vanillaApi.getDashboardSummary(),
    ]);

    state.customers = normalizeCustomers(customers);
    state.entries = normalizeEntries(entries);
    state.summary = normalizeSummary(summary);
    state.loading = false;
    state.error = "";
    render(preserveCustomerId);
    setApiStatus("Connected to API", "healthy");
  } catch (error) {
    state.customers = [];
    state.entries = [];
    state.summary = emptySummary();
    state.loading = false;
    state.error = errorMessage(error);
    render(preserveCustomerId);
    setApiStatus(`Unable to load API data: ${state.error}`, "unhealthy");
  }
}

function setMode(nextMode) {
  mode = nextMode;
  els.quickTitle.textContent = mode === "sale" ? "Sale" : "Payment";
  els.submitLabel.textContent = mode === "sale" ? "Record sale" : "Record payment";
  els.paidNowField.hidden = mode !== "sale";
  els.quickForm.classList.toggle("is-payment", mode !== "sale");
  if (mode !== "sale") {
    els.quickPaidNow.value = "";
  }
  document.querySelectorAll(".mode-option").forEach((button) => {
    const active = button.dataset.mode === mode;
    button.classList.toggle("is-active", active);
    button.setAttribute("aria-selected", String(active));
  });
  renderControls();
}

async function recordQuickEntry(event) {
  event.preventDefault();
  const customerId = selectedCustomerId;
  const amount = toMoney(els.quickAmount.value);
  const paidNow = mode === "sale" ? toMoney(els.quickPaidNow.value) : 0;

  if (!customerId) {
    els.quickCustomer.reportValidity();
    setApiStatus("Select a valid customer from the search results.", "unhealthy");
    return;
  }

  if (amount <= 0) return;

  state.mutating = true;
  renderControls();
  setApiStatus(mode === "sale" ? "Recording sale..." : "Recording payment...");

  try {
    let deletedCustomer = false;

    if (mode === "sale") {
      await window.vanillaApi.recordSale({ customerId, amount, paidNow });
      if (paidNow > 0) {
        deletedCustomer = await promptDeleteIfSettled(customerId);
      }
    } else {
      await window.vanillaApi.recordPayment({ customerId, amount });
      deletedCustomer = await promptDeleteIfSettled(customerId);
    }

    els.quickForm.reset();
    if (!deletedCustomer) {
      await loadData({ preserveCustomerId: customerId, quiet: true });
      selectCustomerById(customerId);
      renderSelectedCustomer();
    } else {
      clearSelectedCustomer({ clearInput: true });
    }

    setApiStatus(deletedCustomer ? "Settled customer deleted from API" : "Entry saved to API", "healthy");
    els.quickAmount.focus();
  } catch (error) {
    setApiStatus(errorMessage(error), "unhealthy");
  } finally {
    state.mutating = false;
    renderControls();
  }
}

async function promptDeleteIfSettled(customerId) {
  const customer = await window.vanillaApi.getCustomer(customerId);
  const balance = toNumber(customer?.currentBalance ?? customer?.balance);

  if (balance > 0) return false;

  const confirmed = confirm(
    "This customer is now settled. Delete the customer and all their sales/payments?",
  );

  if (!confirmed) return false;

  await window.vanillaApi.deleteCustomer(customerId);
  await loadData({ preserveCustomerId: "", quiet: true });
  return true;
}

async function saveCustomer(event) {
  event.preventDefault();
  const name = els.customerName.value.trim();
  if (!name) return;

  state.mutating = true;
  renderControls();
  setApiStatus("Saving customer...");

  try {
    const customer = await window.vanillaApi.createCustomer({
      name,
      openingBalance: toMoney(els.openingBalance.value),
    });

    closeCustomerDialog();
    els.customerForm.reset();
    await loadData({ preserveCustomerId: customer.id, quiet: true });
    setApiStatus("Customer saved to API", "healthy");
  } catch (error) {
    setApiStatus(errorMessage(error), "unhealthy");
  } finally {
    state.mutating = false;
    renderControls();
  }
}

function openCustomerDialog() {
  els.customerForm.reset();
  els.customerDialog.showModal();
  requestAnimationFrame(() => els.customerName.focus());
}

function closeCustomerDialog() {
  els.customerDialog.close();
}

function openFullApp() {
  els.fullApp.classList.add("is-open");
  els.fullApp.setAttribute("aria-hidden", "false");
}

function closeFullApp() {
  els.fullApp.classList.remove("is-open");
  els.fullApp.setAttribute("aria-hidden", "true");
}

function exportData() {
  els.exportBox.value = JSON.stringify(
    {
      source: window.vanillaApi.apiBaseUrl(),
      exportedAt: new Date().toISOString(),
      summary: state.summary,
      customers: state.customers,
      entries: state.entries,
    },
    null,
    2,
  );
  els.exportBox.focus();
  els.exportBox.select();
}

async function checkApiHealth() {
  setApiStatus("Checking...");

  try {
    const health = await window.vanillaApi.getHealth();
    setApiStatus(typeof health === "string" ? health : "Healthy", "healthy");
  } catch (error) {
    setApiStatus(errorMessage(error), "unhealthy");
  }
}

async function clearData() {
  const confirmed = confirm("Clear all customers and ledger entries from the API?");
  if (!confirmed) return;

  state.mutating = true;
  renderControls();
  setApiStatus("Clearing API data...");

  try {
    const result = await window.vanillaApi.clearLedgerData();
    els.exportBox.value = "";
    await loadData({ preserveCustomerId: "", quiet: true });
    setApiStatus(`Cleared ${result.totalRowsSoftDeleted ?? 0} rows from API`, "healthy");
  } catch (error) {
    setApiStatus(errorMessage(error), "unhealthy");
  } finally {
    state.mutating = false;
    renderControls();
  }
}

function render(preferredCustomerId = selectedCustomerId) {
  renderApiConfig();
  renderCustomerSearch(preferredCustomerId);
  renderSelectedCustomer();
  renderMetrics();
  renderCustomers();
  renderEntries();
  renderFilters();
  setMode(mode);
  renderControls();
  refreshIcons();
}

function renderApiConfig() {
  if (window.vanillaApi) {
    els.apiBaseUrl.textContent = window.vanillaApi.apiBaseUrl();
  }
}

function renderCustomerSearch(preferredCustomerId = selectedCustomerId) {
  if (state.loading) {
    els.quickCustomer.placeholder = "Loading customers...";
    hideCustomerSuggestions();
    return;
  }

  if (state.customers.length === 0) {
    els.quickCustomer.placeholder = "Add a customer first";
    clearSelectedCustomer({ clearInput: true });
    return;
  }

  els.quickCustomer.placeholder = "Type 3+ characters to search";
  if (preferredCustomerId) {
    selectCustomerById(preferredCustomerId, { keepSuggestions: true });
  }
  updateCustomerValidity();
}

function renderSelectedCustomer() {
  const customer = getSelectedCustomer();
  if (!customer) {
    els.selectedCustomerName.textContent = state.loading ? "Loading customers" : "Select customer";
    els.selectedCustomerBalance.textContent = currency(0);
    els.selectedCustomerStatus.textContent = "settled";
    els.selectedCustomerStatus.classList.remove("owed");
    return;
  }

  const balance = getBalance(customer.id);
  els.selectedCustomerName.textContent = customer.name;
  els.selectedCustomerBalance.textContent = currency(balance);
  els.selectedCustomerStatus.textContent = balance > 0 ? "owed" : "settled";
  els.selectedCustomerStatus.classList.toggle("owed", balance > 0);
}

function renderMetrics() {
  const todayKey = new Date().toDateString();
  const todaySales = state.entries
    .filter((entry) => entry.type === "sale" && new Date(entry.createdAt).toDateString() === todayKey)
    .reduce((sum, entry) => sum + entry.amount, 0);

  els.heroOutstanding.textContent = currency(state.summary.outstandingBalance);
  els.heroCustomers.textContent = String(state.summary.activeCustomerCount);
  els.heroToday.textContent = currency(todaySales);
  els.metricOutstanding.textContent = currency(state.summary.outstandingBalance);
  els.metricSales.textContent = currency(state.summary.activeOrderTotal);
  els.metricPayments.textContent = currency(state.summary.activePaymentTotal);
}

function renderCustomers() {
  if (state.loading) {
    els.customerList.innerHTML = '<p class="empty-state">Loading customers from API...</p>';
    return;
  }

  if (state.error) {
    els.customerList.innerHTML = `<p class="empty-state">Could not load customers. ${escapeHtml(state.error)}</p>`;
    return;
  }

  if (state.customers.length === 0) {
    els.customerList.innerHTML = '<p class="empty-state">Add a customer to start recording money.</p>';
    return;
  }

  els.customerList.replaceChildren(
    ...state.customers.map((customer) => {
      const balance = getBalance(customer.id);
      const row = document.createElement("article");
      row.className = "customer-row";
      row.innerHTML = `
        <div>
          <strong>${escapeHtml(customer.name)}</strong>
          <p class="subtext">Current balance from API</p>
        </div>
        <strong class="${balance > 0 ? "amount-positive" : "amount-negative"}">${currency(balance)}</strong>
      `;
      return row;
    }),
  );
}

function renderEntries() {
  if (state.loading) {
    els.entryList.innerHTML = '<p class="empty-state">Loading ledger entries from API...</p>';
    return;
  }

  if (state.error) {
    els.entryList.innerHTML = `<p class="empty-state">Could not load ledger entries. ${escapeHtml(state.error)}</p>`;
    return;
  }

  const entries = activeFilter === "all" ? state.entries : state.entries.filter((entry) => entry.type === activeFilter);
  if (entries.length === 0) {
    els.entryList.innerHTML = '<p class="empty-state">No ledger entries yet.</p>';
    return;
  }

  els.entryList.replaceChildren(
    ...entries.map((entry) => {
      const customer = getCustomer(entry.customerId);
      const row = document.createElement("article");
      row.className = "entry-row";
      row.innerHTML = `
        <span class="entry-icon"><i data-lucide="${entry.type === "sale" ? "receipt-text" : "banknote"}"></i></span>
        <div>
          <strong>${escapeHtml(customer?.name ?? entry.customerName ?? "Unknown customer")}</strong>
          <p class="subtext">${labelForEntry(entry)} &middot; ${formatDate(entry.createdAt)}</p>
        </div>
        <strong class="${entry.type === "sale" ? "amount-positive" : "amount-negative"}">
          ${entry.type === "sale" ? "+" : "-"}${currency(entry.amount)}
        </strong>
      `;
      return row;
    }),
  );
  refreshIcons();
}

function renderFilters() {
  document.querySelectorAll(".filter-chip").forEach((button) => {
    button.classList.toggle("is-active", button.dataset.filter === activeFilter);
  });
}

async function handleCustomerSearchInput() {
  const query = els.quickCustomer.value.trim();

  if (selectedCustomerId && query !== selectedCustomerName) {
    clearSelectedCustomer();
  }

  customerSearchError = "";
  if (query.length < 3) {
    customerSuggestions = [];
    customerSearchLoading = false;
    renderCustomerSuggestions();
    renderSelectedCustomer();
    renderControls();
    return;
  }

  const requestId = ++customerSearchRequestId;
  customerSearchLoading = true;
  renderCustomerSuggestions();

  try {
    const results = await window.vanillaApi.searchCustomers(query);
    if (requestId !== customerSearchRequestId) return;

    customerSuggestions = normalizeCustomers(results);
    customerSearchError = "";
  } catch (error) {
    if (requestId !== customerSearchRequestId) return;

    customerSuggestions = [];
    customerSearchError = errorMessage(error);
  } finally {
    if (requestId === customerSearchRequestId) {
      customerSearchLoading = false;
      renderCustomerSuggestions();
      renderControls();
    }
  }
}

function handleCustomerSearchKeys(event) {
  if (event.key === "Escape") {
    hideCustomerSuggestions();
  }
}

function renderCustomerSuggestions() {
  const query = els.quickCustomer.value.trim();
  updateCustomerValidity();

  if (state.loading || state.mutating || query.length < 3 || selectedCustomerId) {
    hideCustomerSuggestions();
    return;
  }

  els.customerSuggestions.hidden = false;
  els.quickCustomer.setAttribute("aria-expanded", "true");

  if (customerSearchLoading) {
    els.customerSuggestions.innerHTML = '<p class="suggestion-empty">Searching...</p>';
    return;
  }

  if (customerSearchError) {
    els.customerSuggestions.innerHTML = `<p class="suggestion-empty">Search unavailable. ${escapeHtml(customerSearchError)}</p>`;
    return;
  }

  if (customerSuggestions.length === 0) {
    els.customerSuggestions.innerHTML = '<p class="suggestion-empty">No matching customers.</p>';
    return;
  }

  els.customerSuggestions.replaceChildren(
    ...customerSuggestions.map((customer) => {
      const button = document.createElement("button");
      button.type = "button";
      button.className = "suggestion-option";
      button.setAttribute("role", "option");
      button.innerHTML = `
        <span>${escapeHtml(customer.name)}</span>
        <strong>${currency(customer.currentBalance)}</strong>
      `;
      button.addEventListener("click", () => selectCustomer(customer));
      return button;
    }),
  );
}

function hideCustomerSuggestions() {
  els.customerSuggestions.hidden = true;
  els.quickCustomer.setAttribute("aria-expanded", "false");
}

function renderControls() {
  const busy = state.loading || state.mutating;
  const noCustomers = state.customers.length === 0;
  els.quickCustomer.disabled = busy || noCustomers;
  els.quickAmount.disabled = busy || noCustomers;
  els.quickPaidNow.disabled = busy || noCustomers || mode !== "sale";
  els.quickSubmit.disabled = busy || noCustomers || !selectedCustomerId;
  els.addCustomer.disabled = busy;
  els.newCustomerQuick.disabled = busy;
  els.exportData.disabled = busy;
  els.clearData.disabled = busy || (state.customers.length === 0 && state.entries.length === 0);
  els.checkApi.disabled = state.mutating;
  els.customerName.disabled = state.mutating;
  els.openingBalance.disabled = state.mutating;
  updateCustomerValidity();
}

function normalizeCustomers(customers) {
  if (!Array.isArray(customers)) return [];

  return customers
    .map((customer) => ({
      id: customer.id,
      name: customer.name,
      currentBalance: toNumber(customer.currentBalance),
    }))
    .sort((a, b) => a.name.localeCompare(b.name));
}

function normalizeEntries(entries) {
  if (!Array.isArray(entries)) return [];

  return entries
    .map((entry) => {
      const entryType = String(entry.entryType || "").toLowerCase();
      return {
        id: entry.id,
        customerId: entry.customerId,
        customerName: entry.customerName,
        type: entryType === "payment" ? "payment" : "sale",
        amount: toNumber(entry.amount),
        createdAt: entry.createdUtc,
        note: entry.notes,
      };
    })
    .sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));
}

function normalizeSummary(summary) {
  return {
    activeCustomerCount: toNumber(summary?.activeCustomerCount),
    activeOrderTotal: toNumber(summary?.activeOrderTotal),
    activePaymentTotal: toNumber(summary?.activePaymentTotal),
    outstandingBalance: toNumber(summary?.outstandingBalance),
  };
}

function emptySummary() {
  return {
    activeCustomerCount: 0,
    activeOrderTotal: 0,
    activePaymentTotal: 0,
    outstandingBalance: 0,
  };
}

function setApiStatus(message, status) {
  els.apiStatus.textContent = message;
  els.apiStatus.className = "api-status";
  if (status) {
    els.apiStatus.classList.add(`is-${status}`);
  }
}

function getCustomer(customerId) {
  return state.customers.find((customer) => customer.id === customerId);
}

function getSelectedCustomer() {
  if (!selectedCustomerId) return null;
  return getCustomer(selectedCustomerId) ?? selectedCustomerSnapshot;
}

function selectCustomerById(customerId, { keepSuggestions = false } = {}) {
  const customer = getCustomer(customerId) ?? selectedCustomerSnapshot;
  if (!customer || customer.id !== customerId) {
    clearSelectedCustomer({ clearInput: true });
    return;
  }

  selectCustomer(customer, { keepSuggestions });
}

function selectCustomer(customer, { keepSuggestions = false } = {}) {
  selectedCustomerId = customer.id;
  selectedCustomerName = customer.name;
  selectedCustomerSnapshot = customer;
  els.quickCustomer.value = customer.name;
  customerSuggestions = [];

  if (!keepSuggestions) {
    hideCustomerSuggestions();
  }

  renderSelectedCustomer();
  renderControls();
}

function clearSelectedCustomer({ clearInput = false } = {}) {
  selectedCustomerId = "";
  selectedCustomerName = "";
  selectedCustomerSnapshot = null;

  if (clearInput) {
    els.quickCustomer.value = "";
  }

  updateCustomerValidity();
}

function updateCustomerValidity() {
  const needsCustomer = !state.loading && !state.mutating && state.customers.length > 0;
  const valid = !needsCustomer || Boolean(selectedCustomerId);
  els.quickCustomer.setCustomValidity(valid ? "" : "Select a customer from the search results.");
}

function getBalance(customerId) {
  const customer = getCustomer(customerId);
  if (customer) return customer.currentBalance;

  return state.entries.reduce((balance, entry) => {
    if (entry.customerId !== customerId) return balance;
    return entry.type === "sale" ? balance + entry.amount : balance - entry.amount;
  }, 0);
}

function labelForEntry(entry) {
  if (entry.note) return entry.note;
  return entry.type === "sale" ? "Sale" : "Payment";
}

function toMoney(value) {
  const number = Number.parseFloat(value);
  if (!Number.isFinite(number)) return 0;
  return Math.round(number * 100) / 100;
}

function toNumber(value) {
  const number = Number(value);
  return Number.isFinite(number) ? number : 0;
}

function currency(value) {
  return new Intl.NumberFormat("en-GB", {
    style: "currency",
    currency: "GBP",
  }).format(value);
}

function formatDate(value) {
  return new Intl.DateTimeFormat("en-GB", {
    day: "2-digit",
    month: "short",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(value));
}

function escapeHtml(value) {
  return String(value ?? "").replace(/[&<>"']/g, (char) => {
    const entities = {
      "&": "&amp;",
      "<": "&lt;",
      ">": "&gt;",
      '"': "&quot;",
      "'": "&#039;",
    };
    return entities[char];
  });
}

function errorMessage(error) {
  return error?.message || "API unavailable";
}

function refreshIcons() {
  if (window.lucide) {
    window.lucide.createIcons();
  }
}
