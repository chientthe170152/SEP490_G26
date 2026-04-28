# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

MTCA is a math-test-creation platform for FPT University. Monorepo with two independently-deployable components:

- `api/` — ASP.NET Core 8 backend (Clean Architecture, 4 projects)
- `web/` — React 18 + Vite SPA (TypeScript)
- `cas/` — Python FastAPI + SymPy side-car (planned, not yet in repo)

## Common commands

### Backend (`api/`)

```bash
cd api
dotnet restore
dotnet user-secrets set "Jwt:Key" "<random 64+ chars>" --project MTCA.Api
dotnet run --project MTCA.Api          # http://localhost:5080 — /health, /swagger
dotnet build MTCA.sln
```

EF migrations (run from `api/`):
```bash
dotnet ef migrations add <Name>  -p MTCA.Infrastructure -s MTCA.Api
dotnet ef database update        -p MTCA.Infrastructure -s MTCA.Api
```
`DbInitializer` auto-applies pending migrations + seeds roles + admin on every startup, so manual `database update` is rarely needed in dev. Dev DB connection + admin seed creds live in `MTCA.Api/appsettings.Development.json` (uses Windows Integrated Security against localhost SQL Server).

No test project exists yet.

### Frontend (`web/`)

```bash
cd web
npm install
npm run dev       # http://localhost:5173 — talks to API at VITE_API_BASE_URL (cross-origin, NOT proxied)
npm run build     # tsc -b && vite build
npm run lint      # eslint
npm run preview
```

### Docker

`api/docker-compose.yml` defines the `api` service only (sqlserver/redis/cas come later). Env via `api/.env` (copy from `.env.example`). Nested config keys use `__` instead of `:` (Microsoft convention) — e.g. `Jwt__Key`.

## Backend architecture

### Project layering (enforced by csproj references)

| Project | Depends on | Holds |
|---|---|---|
| `MTCA.Domain` | nothing (BCL only — no NuGet) | Entities, value objects, enums, `IAuditable`, `IConcurrencyAware`, domain exceptions |
| `MTCA.Application` | Domain | MediatR handlers (CQRS), FluentValidation validators, `Result<T>`, `Error`, interfaces (`IAppDbContext`, `IJwtTokenService`, `IRefreshTokenStore`, `ICurrentUserService`) |
| `MTCA.Infrastructure` | Application | EF Core (`AppDbContext : IdentityDbContext<…>`), Identity, Redis, JWT/refresh-token services, seeders, save-changes interceptors |
| `MTCA.Api` | Application + Infrastructure | Controllers, middleware, options binding, auth scheme, rate limiting, `Program.cs` |

`Program.cs` composes via `AddApplication()` + `AddInfrastructure(config)` + `AddMtcaAuth(config)` + `AddMtcaRateLimiting()`. Middleware order: ForwardedHeaders → Serilog → ExceptionHandler → Routing → CORS → RateLimiter → Auth → `FirstLoginPasswordMiddleware` → Authorization.

Features under `MTCA.Application/Features/{Admin,Auth,HeadOfDepartment,Realtime,Student,Teacher}/…` are organized **by role audience**, not by aggregate. Mirror the same role tree under `MTCA.Api/Controllers/`.

### Domain conventions — read before touching `MTCA.Domain` or EF Configurations

`api/MTCA.Domain/CONVENTIONS.md` is binding. The non-obvious rules:

- **Anemic POCO + state-transition methods.** Public setters everywhere. No factory methods, no private setters, no private ctors. DTO + FluentValidation at Application layer is the input gate; Domain only enforces state-machine invariants (throw `InvariantViolationException`).
- **No defaults on business fields** — neither Domain field initializers nor EF `HasDefaultValue`. Handlers must set every business field explicitly when constructing entities. The only allowed initializers are technical (`= default!` for non-null nav, `= new List<T>()` for collections).
- **Audit fields are infrastructure**: `CreatedAt`/`Timestamp` use `HasDefaultValueSql("SYSUTCDATETIME()")`; `CreatedById`/`UpdatedAt`/`UpdatedById` filled by `AuditSaveChangesInterceptor` for any `IAuditable` entity. The interceptor **throws `InvariantViolationException` if there's no authenticated user on insert** — handlers must run under an authenticated `HttpContext` or supply one.
- **UTC only.** Inject `TimeProvider` and call `time.GetUtcNow().UtcDateTime` — never `DateTime.UtcNow`/`Now`. `AppDbContext.OnModelCreating` applies a global `UtcDateTimeConverter` / `UtcNullableDateTimeConverter` to every `DateTime`/`DateTime?` property: writes coerce non-UTC to UTC, reads stamp `Kind=Utc` on values from SQL Server.
- **Concurrency:** entities implementing `IConcurrencyAware` have a `RowVersion`; conflicts surface as `DbUpdateConcurrencyException` → handlers convert to `Result.Failure(409 Conflict)`.

### Result + Error → HTTP mapping

Handlers return `Result` or `Result<T>`. `MTCA.Api/Extensions/ResultExtensions.ToActionResult(this)` is the only thing controllers should call to translate. Mapping (in `ResultExtensions.MapFailure`):

```
Validation     → 422 Unprocessable Entity (always emits code "VALIDATION")
Unauthorized   → 401
Forbidden      → 403
NotFound       → 404
Conflict       → 409
Locked         → 423
TooManyRequests→ 429
Unexpected     → 500
```

Failure responses are **`ProblemDetails` carrying only `{ type, title, status }`**, where `type = ProblemTypes.Prefix + code` and `title = code` (a stable identifier like `INVALID_CREDENTIALS`). **Do not add human-readable detail/message fields** — clients localize off the code. Add new codes in `MTCA.Application/Common/Constants/ErrorCodes.cs` and corresponding factory in the `*Errors` static class.

### MediatR ValidationBehavior

`ValidationBehavior<TRequest,TResponse>` runs all `IValidator<TRequest>` in parallel. If `TResponse` implements `IResultResponse` (i.e. `Result` / `Result<T>`), it short-circuits to `Result.Failure(Error.Validation("VALIDATION"))` — it does **not** throw `ValidationException`. Therefore: handler signatures returning `Result`/`Result<T>` swallow validation failures into a 422; signatures not returning `Result` would throw. Prefer `Result<T>` for any feature that has a validator.

### Auth

- **JWT bearer** is the scheme; tokens are accepted from the `Authorization: Bearer …` header **or** the `mtca.auth` HttpOnly cookie (`OnMessageReceived` falls back to the cookie when the header is empty). See `MTCA.Api/Extensions/AuthSchemeExtensions.cs`.
- `MapInboundClaims = false` — claims are not auto-renamed, so use `JwtRegisteredClaimNames` / custom keys directly.
- **First-login flow.** The `mcp` claim (`AuthClaims.MustChangePassword`) is `"true"` until the user changes their initial password. The `passwordFresh` policy requires `mcp=false` and is set as **both** `DefaultPolicy` and `FallbackPolicy` — meaning every endpoint, including those with bare `[Authorize]` and even unattributed endpoints, requires a fresh password by default. `FirstLoginPasswordMiddleware` enforces this earlier in the pipeline so the response is the `PASSWORD_CHANGE_REQUIRED` ProblemDetails (not a generic 403). To opt an endpoint out (e.g. the change-password endpoint itself), use `[Authorize(Policy = AuthPolicies.AllowPasswordChange)]`.
- **Refresh tokens** are stored in Redis via `IRefreshTokenStore` (`MTCA.Infrastructure.Services.Tokens.RefreshTokenStore`), keyed by user + JTI. Refresh rotation issues a new access + refresh pair; on failure the controller clears both cookies via `CookieHelper.ClearAuthCookies`.
- **Rate limits** (per-IP fixed window, partition key from `HttpClientContext.ResolveIp`): `RateLimitPolicies.Login` = 10/min, `RateLimitPolicies.Refresh` = 30/min. Apply with `[EnableRateLimiting(RateLimitPolicies.X)]` on the action.
- **Password policy** is a single regex `PasswordRules.Pattern`: ≥8 chars, must contain letter and digit, allowed specials are `!@#$%^&*()_=+-`. Used by both FluentValidation and `MtcaPasswordValidator` (Identity).
- Roles live in `MTCA.Domain.Common.Roles` — `TEACHER`, `STUDENT`, `HOD`, `ADMIN`. `RoleSeeder` ensures they exist on startup.

### Logging

Serilog is configured in `Program.cs` (Console + rolling file `logs/mtca-.log`). Settings overrides come from `appsettings*.json`. `EventLog` entity exists in the schema for future MSSqlServer sink; `Serilog.Sinks.MSSqlServer` is referenced but not yet wired.

## Frontend architecture (`web/`)

- **All HTTP must go through `src/lib/api-client.ts`** (it sets `credentials: 'include'` for the auth cookie). Never call `fetch` directly.
- **All SSE subscriptions must go through `src/lib/sse-client.ts`** (`withCredentials: true`).
- `VITE_API_BASE_URL` is inlined at build time. Changing it requires a rebuild. Vite does **not** proxy in dev — the SPA hits the API cross-origin (`http://localhost:5173` → `http://localhost:5080`) so CORS issues surface in dev exactly as in prod. Configure `Cors:AllowedOrigins` in `appsettings.Development.json` accordingly.
- `web/CLAUDE.md` exists and contains general behavioral guidelines (think-before-coding, surgical changes, simplicity-first). It is not project-specific architecture.

## Adding a new feature handler — checklist

1. Add request/handler/validator under `MTCA.Application/Features/{role}/{Commands|Queries}/{Name}/`. Use `Result<T>` return type.
2. Add any new error codes to `ErrorCodes.cs` and a factory in the relevant `*Errors` static class.
3. Add controller action under `MTCA.Api/Controllers/{role}/`, call `mediator.Send(...)`, return `result.ToActionResult(this)`.
4. If the action mutates DB while authenticated, no extra wiring needed — the audit interceptor + UTC converter run automatically.
5. If the entity is new: add it to `AppDbContext`, add a Configuration under `MTCA.Infrastructure/Persistence/Configurations/{aggregate}/`, generate a migration. Re-read `MTCA.Domain/CONVENTIONS.md` §3 before touching defaults.
