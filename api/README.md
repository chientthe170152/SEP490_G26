# MTCA API

Backend container của MTCA (ASP.NET Core 8, Clean Architecture 4-project). Đây là basic scaffold — chỉ boot được `/health` + `/swagger`, chưa wire Identity/EF/Redis/CAS.

## Quick start (dev local)

```bash
cd api
dotnet restore
dotnet run --project MTCA.Api
# → listening on http://localhost:5080
```

Kiểm tra:
- `http://localhost:5080/health` → `Healthy`
- `http://localhost:5080/swagger` → OpenAPI UI (Development only)

## Docker

```bash
cp .env.example .env    # fill real values (Jwt__Key tối thiểu)
docker compose up -d api
curl http://127.0.0.1:5080/health
```

## Cấu trúc

| Project | Vai trò |
|---|---|
| `MTCA.Domain` | Entities, VO, Enums. BCL-only, không package NuGet. |
| `MTCA.Application` | Use-case (MediatR CQRS), Interfaces, Validators. |
| `MTCA.Infrastructure` | EF Core, Redis, CAS client, SMTP, SSE broker. |
| `MTCA.Api` | Controllers, Middleware, Program.cs. |

Chi tiết kiến trúc và ánh xạ feature ↔ project: [../../antigravity/SEP490/detail-design/api_project_structure.md](../../antigravity/SEP490/detail-design/api_project_structure.md).
Quyết định công nghệ: [../../antigravity/SEP490/dev/tech_stack.md](../../antigravity/SEP490/dev/tech_stack.md).

## Next steps (sau basic setup)

1. Wire Identity (`ApplicationUser`, `ApplicationRole`, `AppDbContext : IdentityDbContext`).
2. Thêm aggregate đầu tiên (đề xuất: `MasterData` — Subject/Chapter/Semester) + migration.
3. Bật `UseAuthentication` / `UseAuthorization` trong `Program.cs`.
4. Thêm services `sqlserver`/`redis`/`cas` vào `docker-compose.yml`.
5. Serilog `.WriteTo.MSSqlServer()` vào bảng `EventLog` (sau khi migration sinh bảng).
