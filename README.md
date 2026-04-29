# Vanilla Aspire App

This workspace keeps the Aspire development flow for day-to-day work and adds a Docker path for running the backend with SQL Server isolated behind the API container.

## Aspire development

Use the AppHost for normal local development. It starts:

- `sqlserver` as an Aspire-managed SQL Server container with a persistent volume
- `api` as the ASP.NET Core minimal API project

The AppHost injects the SQL connection and the field encryption key into the API so the backend still runs through the same Aspire orchestration flow.

The React frontend folder is still present in the workspace and can continue to run independently alongside the AppHost-backed API flow.

## Docker development

1. Copy `.env.example` to `.env` and set strong values for `SQLSERVER_SA_PASSWORD` and `FIELD_ENCRYPTION_KEY`.
2. Run `docker compose up --build`.
3. Reach the API on `http://127.0.0.1:12345`.

`FIELD_ENCRYPTION_KEY` can be any non-empty secret string. The API does not expect raw AES bytes, hex, or Base64 specifically. It UTF-8 encodes the configured string, hashes it with SHA-256, and uses that 32-byte hash as the AES-GCM key, so a long random secret is the safest choice.

Important network decisions:

- The API is the only published container port and it is bound to `127.0.0.1` only.
- SQL Server is attached only to the private `backend` Docker network.
- SQL Server has no host port mapping, so it is not reachable from the host network or Tailscale directly.
- The API connects to SQL Server by the Docker service name `sqlserver`.

## Tailscale Serve

Expose only the API through Tailscale Serve. Tailscale’s January 2026 Serve documentation says reverse proxies target `http://127.0.0.1`, so keep the API bound to loopback and do not publish SQL Server.

Example:

```bash
tailscale serve 12345
```

That shares the local API privately to your tailnet over HTTPS while still leaving SQL Server private inside Docker. Do not configure Tailscale Serve or Funnel for SQL Server.
