# Corner Ledger

A dark, local-first prototype for recording simple customer sales and payments.

Run the local static server when testing API calls:

```powershell
powershell -ExecutionPolicy Bypass -File .\serve.ps1
```

Then open `http://localhost:5173`.

Open `index.html` directly only for UI-only checks. The app stores data in the browser's local storage.

The API base URL is configured in `config.js`:

```javascript
window.VANILLA_API_BASE_URL = "http://localhost:12345";
```

The app can check `http://localhost:12345/health` from the full settings view.

## What it does

- Records sales against a customer.
- Records payments that do not need to be linked to a sale.
- Allows a sale to include a paid-now amount for immediate or partial payment.
- Shows customer balances as sales minus payments.
- Keeps the quick-entry app small and fixed in the corner.
- Opens the fuller customer, ledger, export, and reset tools from the settings button.

## Current limits

- No stock or product catalogue yet.
- No cloud sync or multi-device data sharing yet.
- No user accounts or permissions yet.
