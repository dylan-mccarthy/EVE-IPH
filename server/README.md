# Server (ASP.NET Core) Scaffold

Planned stack:
- Target: `net8.0-windows` → `net9.0-windows`
- Templates: ASP.NET Core Web API (minimal APIs), `UseWindowsService` optional.
- Auth: cookie/session for local, token-based for SPA calls; CORS enabled for `frontend` origin.
- Data: SQLite via `Microsoft.Data.Sqlite` (or System.Data.SQLite), domain services shared with legacy code as we extract.

Initial steps to flesh out:
1) Create SDK-style solution + `server` project (dotnet new webapi --no-https initially; add HTTPS/dev cert later).
2) Add reference to shared domain library when extracted; for now use placeholder services.
3) Add baseline endpoints: `/health`, `/version`, `/blueprints`, `/manufacturing/calc`, `/prices`, `/settings`.
4) Wire DI, logging, configuration (`appsettings.json` + user secrets if needed).
5) Add CORS policy for `http://localhost:5173` (Vite dev server) and production host.
6) Add minimal tests project (`dotnet new xunit`) for API contracts.
