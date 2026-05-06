# vanilla-app

`vanilla-app` is the ASP.NET Core Minimal API backend. The demo frontend lives in `frontend/`.

## Standard local URLs

- API base URL: `http://localhost:12345`
- Health: `http://localhost:12345/health`
- OpenAPI JSON: `http://localhost:12345/openapi/v1.json`
- Scalar UI: `http://localhost:12345/scalar/v1`
- UI dev URL: `http://localhost:5173`
- SQL Server: `localhost:1433`

## Non-Docker local run

1. Start SQL Server locally on `localhost:1433` or run the Compose SQL container.
2. Set a non-empty `FIELD_ENCRYPTION_KEY`.
3. Run the API:

```powershell
$env:FIELD_ENCRYPTION_KEY="replace-with-a-long-random-secret"
$env:ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=VanillaApp;User Id=sa;Password=ChangeMe_Example-Strong-Pass123!;Encrypt=False;TrustServerCertificate=True"
dotnet run --project Vanilla.Api/Vanilla.Api.csproj --launch-profile http
```

The API launch profile is pinned to `http://localhost:12345`.

## Docker run

1. Copy `.env.example` to `.env`.
2. Update `SQLSERVER_SA_PASSWORD` and `FIELD_ENCRYPTION_KEY`.
3. Start the stack:

```powershell
docker compose up --build
```

Docker networking is standardized as:

- The API listens on port `12345` inside the container.
- Docker publishes `127.0.0.1:12345` on the host to container port `12345`.
- SQL Server listens on `1433` inside the container and is published to `127.0.0.1:1433`.

## Frontend dev integration

The frontend Vite dev server runs on `http://localhost:5173`.

Create `frontend/.env.local` from `frontend/.env.example` if you want to override the API base URL explicitly:

```powershell
Copy-Item frontend/.env.example frontend/.env.local
```

By default, the frontend dev client targets `http://localhost:12345`, and the API CORS policy allows `http://localhost:5173`.

## Visual Studio and frontend workflow

Use Visual Studio Folder View for the full repository so the `frontend/` Vite/React app remains visible as a normal repo folder. The frontend is not a .NET project, does not need a `.csproj`, and should stay inside this repository at `frontend/`.

Run the API from Visual Studio using the existing API project. The API launch profile is pinned to:

```text
http://localhost:12345
```

Run the frontend from a terminal:

```powershell
cd frontend
npm run dev
```

The frontend dev server runs at:

```text
http://localhost:5173
```

## Docs and health

OpenAPI JSON is exposed at:

```text
http://localhost:12345/openapi/v1.json
```

Scalar UI is exposed at:

```text
http://localhost:12345/scalar/v1
```

API docs are enabled automatically in `Development`. In other environments, set `ApiDocs:Enabled=true` to enable them.

Health is exposed at:

```text
http://localhost:12345/health
```

## Verification

Run:

```powershell
Invoke-WebRequest http://localhost:12345/health
Invoke-WebRequest http://localhost:12345/openapi/v1.json
```

Then manually test:

```text
http://localhost:12345/scalar/v1
```

## Troubleshooting

- `404` at `/scalar/v1`:
  Make sure the API is running in `Development`, or set `ApiDocs:Enabled=true`.
- Port mismatch:
  If the API is not reachable on `12345`, check `Vanilla.Api/Properties/launchSettings.json`, `Vanilla.Api/Dockerfile`, and `docker-compose.yml` for overridden local settings, then restart the process or containers.
- CORS failures from the frontend:
  Confirm the UI is running on `http://localhost:5173` and the API `Cors:AllowedOrigins` configuration still includes that origin.
- SQL connection issues:
  For Docker, use the `sqlserver` service name in `ConnectionStrings__DefaultConnection`. For host-based API runs, use `localhost,1433`.
- Encryption key startup error:
  `FIELD_ENCRYPTION_KEY` can be any non-empty secret string. The app UTF-8 encodes it, hashes it with SHA-256, and uses that 32-byte hash for AES-GCM.

## Tailscale Serve

Expose only the API through Tailscale Serve:

```bash
tailscale serve 12345
```

That keeps the API reachable through Tailscale while SQL Server remains bound to local Docker networking and `localhost`.
