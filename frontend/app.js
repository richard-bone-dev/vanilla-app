const STORAGE_KEY = "corner-ledger:v1";

const starterData = {
  customers: [
    { id: makeId(), name: "Cash customer", openingBalance: 0 },
    { id: makeId(), name: "A. Carter", openingBalance: 24.5 },
  ],
  entries: [],
};

const state = loadState();
let mode = "sale";
let activeFilter = "all";

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
  quickTitle: document.querySelector("#quickTitle"),
  selectedCustomerBalance: document.querySelector("#selectedCustomerBalance"),
  selectedCustomerName: document.querySelector("#selectedCustomerName"),
  selectedCustomerStatus: document.querySelector("#selectedCustomerStatus"),
  submitLabel: document.querySelector("#submitLabel"),
};

seedIfEmpty();
bindEvents();
render();

function loadState() {
  try {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (!saved) return structuredClone(starterData);
    const parsed = JSON.parse(saved);
    return {
      customers: Array.isArray(parsed.customers) ? parsed.customers : [],
      entries: Array.isArray(parsed.entries) ? parsed.entries : [],
    };
  } catch {
    return structuredClone(starterData);
  }
}

function seedIfEmpty() {
  if (state.customers.length > 0) return;
  state.customers = structuredClone(starterData.customers);
  saveState();
}

function saveState() {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
}

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
  els.quickCustomer.addEventListener("change", renderSelectedCustomer);
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
  });
}

function setMode(nextMode) {
  mode = nextMode;
  els.quickTitle.textContent = mode === "sale" ? "Sale" : "Payment";
  els.submitLabel.textContent = mode === "sale" ? "Record sale" : "Record payment";
  els.paidNowField.hidden = mode !== "sale";
  document.querySelectorAll(".mode-option").forEach((button) => {
    const active = button.dataset.mode === mode;
    button.classList.toggle("is-active", active);
    button.setAttribute("aria-selected", String(active));
  });
}

function recordQuickEntry(event) {
  event.preventDefault();
  const customerId = els.quickCustomer.value;
  const amount = toMoney(els.quickAmount.value);
  const paidNow = toMoney(els.quickPaidNow.value);

  if (!customerId || amount <= 0) return;

  const now = new Date().toISOString();
  const batchId = makeId();

  if (mode === "sale") {
    state.entries.unshift({
      id: makeId(),
      batchId,
      customerId,
      type: "sale",
      amount,
      createdAt: now,
    });

    if (paidNow > 0) {
      state.entries.unshift({
        id: makeId(),
        batchId,
        customerId,
        type: "payment",
        amount: paidNow,
        createdAt: now,
        note: "Paid at sale",
      });
    }
  } else {
    state.entries.unshift({
      id: makeId(),
      customerId,
      type: "payment",
      amount,
      createdAt: now,
    });
  }

  saveState();
  els.quickForm.reset();
  els.quickCustomer.value = customerId;
  render();
  els.quickAmount.focus();
}

function saveCustomer(event) {
  event.preventDefault();
  const name = els.customerName.value.trim();
  if (!name) return;

  const customer = {
    id: makeId(),
    name,
    openingBalance: toMoney(els.openingBalance.value),
  };

  state.customers.push(customer);
  state.customers.sort((a, b) => a.name.localeCompare(b.name));
  saveState();
  closeCustomerDialog();
  render();
  els.quickCustomer.value = customer.id;
  renderSelectedCustomer();
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
  els.exportBox.value = JSON.stringify(state, null, 2);
  els.exportBox.focus();
  els.exportBox.select();
}

async function checkApiHealth() {
  els.apiStatus.textContent = "Checking...";
  els.apiStatus.className = "api-status";

  try {
    const health = await window.vanillaApi.getHealth();
    els.apiStatus.textContent = health;
    els.apiStatus.classList.add("is-healthy");
  } catch (error) {
    els.apiStatus.textContent = error.message || "API unavailable";
    els.apiStatus.classList.add("is-unhealthy");
  }
}

function clearData() {
  const confirmed = confirm("Clear all customers and ledger entries?");
  if (!confirmed) return;

  state.customers = structuredClone(starterData.customers);
  state.entries = [];
  saveState();
  render();
  els.exportBox.value = "";
}

function render() {
  renderApiConfig();
  renderCustomerOptions();
  renderSelectedCustomer();
  renderMetrics();
  renderCustomers();
  renderEntries();
  renderFilters();
  setMode(mode);
  refreshIcons();
}

function renderApiConfig() {
  if (window.vanillaApi) {
    els.apiBaseUrl.textContent = window.vanillaApi.apiBaseUrl();
  }
}

function renderCustomerOptions() {
  const current = els.quickCustomer.value;
  els.quickCustomer.replaceChildren(
    ...state.customers.map((customer) => {
      const option = document.createElement("option");
      option.value = customer.id;
      option.textContent = customer.name;
      return option;
    }),
  );

  if (state.customers.some((customer) => customer.id === current)) {
    els.quickCustomer.value = current;
  }
}

function renderSelectedCustomer() {
  const customer = getCustomer(els.quickCustomer.value) ?? state.customers[0];
  if (!customer) {
    els.selectedCustomerName.textContent = "No customer";
    els.selectedCustomerBalance.textContent = currency(0);
    return;
  }

  els.quickCustomer.value = customer.id;
  const balance = getBalance(customer.id);
  els.selectedCustomerName.textContent = customer.name;
  els.selectedCustomerBalance.textContent = currency(balance);
  els.selectedCustomerStatus.textContent = balance > 0 ? "owed" : "settled";
  els.selectedCustomerStatus.classList.toggle("owed", balance > 0);
}

function renderMetrics() {
  const totals = state.entries.reduce(
    (acc, entry) => {
      acc[entry.type] += entry.amount;
      return acc;
    },
    { sale: 0, payment: 0 },
  );
  const outstanding = state.customers.reduce((sum, customer) => sum + getBalance(customer.id), 0);
  const todayKey = new Date().toDateString();
  const todaySales = state.entries
    .filter((entry) => entry.type === "sale" && new Date(entry.createdAt).toDateString() === todayKey)
    .reduce((sum, entry) => sum + entry.amount, 0);

  els.heroOutstanding.textContent = currency(outstanding);
  els.heroCustomers.textContent = String(state.customers.length);
  els.heroToday.textContent = currency(todaySales);
  els.metricOutstanding.textContent = currency(outstanding);
  els.metricSales.textContent = currency(totals.sale);
  els.metricPayments.textContent = currency(totals.payment);
}

function renderCustomers() {
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
          <p class="subtext">Opening ${currency(customer.openingBalance)}</p>
        </div>
        <strong class="${balance > 0 ? "amount-positive" : "amount-negative"}">${currency(balance)}</strong>
      `;
      return row;
    }),
  );
}

function renderEntries() {
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
          <strong>${escapeHtml(customer?.name ?? "Unknown customer")}</strong>
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

function getCustomer(customerId) {
  return state.customers.find((customer) => customer.id === customerId);
}

function getBalance(customerId) {
  const customer = getCustomer(customerId);
  const openingBalance = customer?.openingBalance ?? 0;
  return state.entries.reduce((balance, entry) => {
    if (entry.customerId !== customerId) return balance;
    return entry.type === "sale" ? balance + entry.amount : balance - entry.amount;
  }, openingBalance);
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
  return value.replace(/[&<>"']/g, (char) => {
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

function makeId() {
  if (window.crypto?.randomUUID) return window.crypto.randomUUID();
  return `id-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

function refreshIcons() {
  if (window.lucide) {
    window.lucide.createIcons();
  }
}
