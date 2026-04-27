# API Solution

This folder contains the API-only Visual Studio solution for the monorepo.

## Restore

From `api/`:

```powershell
dotnet restore Vanilla.Api.sln
```

## Build

From `api/`:

```powershell
dotnet build Vanilla.Api.sln
```

## Test

From `api/`:

```powershell
dotnet test Vanilla.Api.sln
```

## Run the API

From `api/`:

```powershell
dotnet run --project src/Vanilla.Api/Vanilla.Api.csproj
```

Set `FIELD_ENCRYPTION_KEY` and a valid connection string before running the API directly. In Docker, those values come from `.env`.

## Docker networking

`docker compose up --build` publishes only the API port to `127.0.0.1:${API_PORT}`. SQL Server stays on the internal `backend` Docker network with no host `1433` port mapping, so the frontend and public network traffic cannot reach SQL Server directly. The frontend should call only the API, and the API reaches SQL Server through the Docker service name `sqlserver`.