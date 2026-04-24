# MTCA — Mathematics Test Creation Application

Nền tảng khảo thí toán cho Đại học FPT (MAE101 / MAD101 / MAS291). Kiến trúc đa thành phần, mỗi thành phần deploy độc lập.

## Cấu trúc repo

```
MTCA/
├── api/     # Backend ASP.NET Core 8 (Clean Architecture — xem api/README.md)
├── cas/     # Python FastAPI + SymPy side-car (chưa triển khai)
├── web/     # React 18 + Vite SPA (chưa triển khai)
├── .gitignore
└── README.md
```

## Stack

| Layer | Công nghệ |
|---|---|
| Backend | ASP.NET Core 8, EF Core, MediatR, FluentValidation |
| Database | SQL Server |
| Cache / Pub-Sub | Redis |
| CAS Engine | Python + FastAPI + SymPy (side-car) |
| Auth | ASP.NET Identity + JWT (HttpOnly cookie) |
| Frontend | React 18.3 + Vite 5 + TypeScript |
| Realtime | Server-Sent Events (SSE) |
| Math | KaTeX (render) + MathLive (input) |

Chi tiết kiến trúc: [dev/tech_stack.md](../antigravity/SEP490/dev/tech_stack.md).

## Quick start (api only)

```bash
cd api
dotnet restore
cd MTCA.Api
dotnet user-secrets set "Jwt:Key" "<random 64 chars>"
cd ..
dotnet run --project MTCA.Api
# http://localhost:5080/health → Healthy
# http://localhost:5080/swagger
```

Mọi thành phần có Dockerfile + docker-compose + `.env.example` riêng. Copy `.env.example` → `.env` và điền secrets trước khi `docker compose up`.
