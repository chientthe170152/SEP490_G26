# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Solution layout

Three .NET 8 projects in `MTCA_SEP490_G26.sln`:

- `Backend/` — ASP.NET Core Web API. JWT auth, EF Core (SQL Server), Hangfire, SignalR. Runs on `http://localhost:5217` / `https://localhost:7167` in dev. Swagger at `/swagger`, Hangfire dashboard at `/hangfire` (Development only).
- `Frontend/` — ASP.NET Core MVC (server-rendered Razor + jQuery). Runs on `http://localhost:5291` / `https://localhost:7109`. Controllers only render views; the browser calls the Backend API directly using `window.API_BASE_URL` set from `Backend:BaseUrl` (see `Views/Shared/_ApiConfig.cshtml`).
- `BackEnd_UnitTest/` — xUnit + Moq + EF Core InMemory. References `Backend`. Tests are grouped per service (e.g. `AnalyticsUnitTest/`, `CourseUnitTest/`).

The Frontend does **not** import the Backend assembly — they are independently deployed and only communicate over HTTP. Cross-cutting types (DTOs, error response shape) are duplicated by intent.

## Common commands

Run from the repo root unless noted.

```bash
# Restore + build everything
dotnet build MTCA_SEP490_G26.sln

# Run the API (loads .env from Backend/, requires SQL Server reachable)
dotnet run --project Backend

# Run the MVC frontend (talks to API via Backend:BaseUrl in appsettings)
dotnet run --project Frontend

# Run all backend unit tests
dotnet test BackEnd_UnitTest

# Run a single test by display name or fully-qualified method
dotnet test BackEnd_UnitTest --filter "FullyQualifiedName~GetExamAnalyticsDetailAsync_UTCID01"
dotnet test BackEnd_UnitTest --filter "DisplayName~UTCID01"

# EF Core migrations (project is Backend, startup is Backend)
dotnet ef migrations add <Name> --project Backend --startup-project Backend
dotnet ef database update --project Backend --startup-project Backend
```

Docker: `Backend/docker-compose.yml` builds an image that listens on `:8080` (mapped to host `7167`) and reads every secret from environment variables — the `.env` mechanism below is for local `dotnet run`.

## Configuration / secrets

`Backend/Program.cs` calls `DotNetEnv.Env.Load()` at startup, then `MapRequiredEnv` resolves each setting from environment variables **first**, falling back to `appsettings.json`. If both are empty for any required key the app throws and refuses to start. Required env vars (also accepted as the matching `appsettings.json` keys):

```
DB_CONNECTION_STRING       → ConnectionStrings:MyCnn
JWT_KEY / JWT_ISSUER / JWT_AUDIENCE
EMAIL_SMTP_SERVER / EMAIL_PORT / EMAIL_SENDER / EMAIL_PASSWORD
GOOGLE_CLIENT_ID           → Google:ClientId
FRONTEND_BASE_URL          → FrontendSettings:BaseUrl
```

Place a `.env` next to `Backend/Backend.csproj` for local development (the file is gitignored). When SMTP is unset the OTP code is logged to console instead of emailed (see `docs/EMAIL_SETUP.md`).

The Frontend project reads `Backend:BaseUrl` and `Google:ClientId` from its own `appsettings*.json` only.

## Architecture notes

**Layering (Backend).** `Controllers → Services (Interfaces/Implements) → Repositories (Interfaces/Implements) → MtcaSep490G26Context (EF Core)`. Every service and repo is registered in `Program.cs`; constructor injection is the only wiring style. `Models/` was scaffolded from the database — `MtcaSep490G26Context.Partial.cs` is the safe place to override mappings without losing changes when re-scaffolding. The schema source of truth is `database/schema-v6.dbml`; ad-hoc migration SQL lives in `Backend/DatabaseScripts/`.

**Controller conventions.** All API controllers inherit `BaseController` (`Backend/Controllers/BaseController.cs`) which extracts `userId`, role, and email from JWT claims and provides `SuccessResponse<T>`, `ErrorResponse`, `Forbidden`. Don't read claims directly — use the helpers so the response shape (`{ success, message, data|details }`) stays consistent.

**Errors.** `Backend.Helper.ExceptionMiddleware` is wired first in the pipeline. Throw subclasses of `Backend.Exceptions.BaseException` (`NotFoundException`, `ForbiddenException`, `DetailedValidationException`, etc.) — the middleware reads `StatusCode` and `Details` off the exception and serialises `{ success: false, message, details }`. `KeyNotFoundException → 404` and `UnauthorizedAccessException → 403` are also mapped. 500s currently leak `InnerException.Message` and stack trace by design (marked `// TEMPORARY` in the middleware) for diagnosis.

**ID obfuscation.** `Backend/Helper/SecureIdHelper.cs` encrypts integer IDs using Skip32 with a hard-coded key — public-facing IDs (URLs, DTOs) should be passed through `EncryptId` / `DecryptId` rather than exposing raw `int`.

**Auth.** JWT bearer with claims-based roles (`Backend/Constants/UserRoles.cs`). Google sign-in uses `Google.Apis.Auth` to validate ID tokens — `Google:ClientId` must match the token's audience. OTP for register/reset is currently in `IMemoryCache` keyed `OTP_{email}` / `RESET_OTP_{email}` with 10-minute TTL; an in-progress migration to Redis is documented in `docs/redis_migration_plan.md`.

**Background jobs.** Hangfire uses the same SQL Server as EF (`MyCnn`). `Backend/Jobs/ExamStatusJob.cs` schedules per-exam transitions at the exact `OpenAt` / `CloseAt` UTC instants instead of polling. Jobs are idempotent — they re-check the current `Status` before mutating, so cancelling an exam is implemented as "let the job run and no-op" rather than removing the scheduled job (Hangfire's `BackgroundJob.Schedule` doesn't support custom IDs). Call `ExamStatusJob.ScheduleExamJobs(examId, openAt, closeAt)` from services after creating/updating an exam.

**Realtime.** `Backend/Hubs/ExamHub.cs` (mounted at `/examHub`) tracks live student-online state via SignalR groups (`Exam_{examId}`). `docs/signalr_removal_and_redis_migration_report.md` plans to replace this with Redis Pub/Sub, so prefer minimal additions to `ExamHub`.

**Frontend wiring.** Razor controllers (`Frontend/Controllers/*Controller.cs`) only return views; the actual data flow is `Views/<area>/*.cshtml` + `wwwroot/js/<area>/*.js` → `fetch(window.API_BASE_URL + '/api/...')` with the JWT in cookies/localStorage. When adding a feature, the typical surface is: Backend controller + service + repo, then a Razor view + a JS file under `wwwroot/js/<feature>/`, then a route in the matching `Frontend` MVC controller. The Frontend MVC controller does not proxy API calls.

**Vietnamese in user-facing strings.** Error/validation/success messages live in `Backend/Constants/Messages.cs` and are written in Vietnamese. Match the existing language when adding new messages there.

## Testing notes

- xUnit + `Moq` (`MockBehavior.Strict` is common in the existing tests — every mocked method needs an explicit `Setup`).
- Use `Microsoft.EntityFrameworkCore.InMemory` for repository tests that need a real `DbContext`.
- Use `Hangfire.MemoryStorage` for tests that touch code paths calling `BackgroundJob.Schedule`.
- Test classes follow the `<Method>_UTCID<NN>_<scenario>` naming convention and set `[Fact(DisplayName = "...")]` so they're filterable by display name.
