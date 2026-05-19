# Corner Ledger

A dark static frontend for recording simple customer sales and payments through `Vanilla.Api`.

Run the local static server:

```powershell
cd frontend
powershell -ExecutionPolicy Bypass -File .\serve.ps1
```

Then open `http://localhost:5173`.

Do not open `index.html` directly with `file://`; the demo UI is intended to run over HTTP at `localhost:5173`. Customer and ledger data is loaded from the API, not browser storage.

If a browser still has an old service worker registered for `localhost:5173`, first open `http://localhost:5173/sw-reset.html`. If the old worker still blocks navigation, unregister it in DevTools under `Application > Service Workers`, then hard reload `http://localhost:5173/index.html`.

The API base URL is configured in `config.js`:

```javascript
window.VANILLA_API_BASE_URL = "http://localhost:12345";
```

The app uses `http://localhost:12345/health`, `/api/customers`, `/api/customers/{id}/ledger`, `/api/ledger/entries`, `/api/orders`, `/api/payments`, `/api/dashboard/summary`, and the settled-customer `DELETE /api/customers/{customerId}` endpoint from the full settings and quick-entry views.

## What it does

- Records sales against a customer.
- Records payments that do not need to be linked to a sale.
- Allows a sale to include a paid-now amount for immediate or partial payment.
- Shows customer balances as sales minus payments.
- Keeps the quick-entry app small and fixed in the corner.
- Opens the fuller customer, ledger, export, and reset tools from the settings button.

## Current limits

- No stock or product catalogue yet.
- No user accounts or permissions yet.
