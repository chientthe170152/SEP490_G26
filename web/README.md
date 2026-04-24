# MTCA — Web (React SPA)

Frontend cho MTCA (Mathematics Test Creation Application). Stack và quyết định kiến trúc: xem [dev/tech_stack.md §5 và §10.4](../../../antigravity/SEP490/dev/tech_stack.md) trong workspace planning.

## Dev

```bash
npm install
npm run dev    # http://localhost:5173
```

Backend API chạy ở `http://localhost:5080` (xem `../api/README.md`). Vite **không** dùng `server.proxy` — dev cross-origin thật để phát hiện lỗi CORS sớm như prod.

## Build

```bash
npm run build   # sinh dist/
npm run preview # serve thử dist/ local
```

## Env

| Key | Dev | Prod |
|---|---|---|
| `VITE_API_BASE_URL` | `http://localhost:5080` | `https://api.<domain>` |
| `VITE_APP_NAME` | `MTCA (dev)` | `MTCA` |

`VITE_*` được **inline vào bundle lúc build**. Đổi giá trị = build lại.

| File | Commit | Mục đích |
|---|---|---|
| `.env.example` | ✅ | Template |
| `.env.development` | ✅ | Dev local (dùng khi `npm run dev`) |
| `.env.production.example` | ✅ | Template prod |
| `.env.production` | ❌ | Ops tạo trực tiếp trên VPS Web |

## Quy ước bắt buộc

- Mọi HTTP request đi qua `src/lib/api-client.ts` (đã `credentials: 'include'`) — KHÔNG gọi `fetch` thẳng.
- Mọi SSE subscription đi qua `src/lib/sse-client.ts` (đã `withCredentials: true`).
- Không hard-code domain; đọc qua `import.meta.env.VITE_API_BASE_URL`.

## Cấu trúc `src/`

```
app/         # entry, router, providers, pages top-level
features/    # chia theo domain: exam, question-bank, class, dashboard, practice
components/  # UI tái dùng (KaTeXView, MathLiveInput, Button...)
hooks/       # useSSE, useAutoSave, useHeartbeat
lib/         # api-client, sse-client, latex utils
services/    # wrap REST endpoint calls
types/       # shared types (có thể sinh từ OpenAPI)
```

## Deploy

Build trực tiếp trên VPS Web (không Docker). Nginx native serve `dist/`. Cloudflare Tunnel forward `web.<domain>` → `127.0.0.1:8080`. Tham khảo `nginx.conf`.
