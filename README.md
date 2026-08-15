# 諮心管理 ConsulCore — Backend

針對心理諮商所與個人執業心理師的全流程執業管理工具，本倉庫為後端 API。

- 框架：C# .NET 10 Web API
- ORM：Entity Framework Core + Pomelo MySQL
- 資料庫：MySQL / MariaDB 8.x+
- 認證：JWT Bearer Token（HS256）+ Policy-based 權限
- 加密：欄位層級 AES-256-GCM

前端倉庫：`consulcore-frontend`（Vue 3 SPA，獨立部署）

## 相關文件

- [`CLAUDE.md`](./CLAUDE.md) — 專案架構、業務規則、開發規範
- [`docs/frontend-integration.md`](./docs/frontend-integration.md) — **前後端介接契約**（API 端點、DTO/TypeScript 型別、enum 值、權限模型）。前端開發以此為唯一事實來源
- [`docs/backend-changes-for-frontend.md`](./docs/backend-changes-for-frontend.md) — **後端 → 前端**異動通知清單（需前端確認/處理的項目）
- [`docs/frontend-requested-endpoints.md`](./docs/frontend-requested-endpoints.md) — **前端 → 後端**待補端點清單（2026-08-15 已結案）
- [`SECURITY.md`](./SECURITY.md) — 機密管理與憑證輪替步驟

---

## 環境需求

- [.NET SDK 10.0+](https://dotnet.microsoft.com/download)
- MySQL / MariaDB 8.x+
- `dotnet-ef` 工具：`dotnet tool install --global dotnet-ef`

---

## 設定

機密設定**不寫入版控**（見 `.gitignore`）。本地開發請擇一：

### 方式 A：appsettings.Development.json（本地，已被 gitignore）

複製範例並填入實際值：

```bash
cp appsettings.Development.json.example appsettings.Development.json
```

### 方式 B：環境變數（建議用於部署）

參考 [`.env.example`](./.env.example)：

| 變數 | 說明 |
|------|------|
| `ConnectionStrings__DefaultConnection` | MySQL 連線字串 |
| `Jwt__SecretKey` | JWT 簽章金鑰（至少 32 字元） |
| `Encryption__Key` | AES-256 金鑰（Base64 編碼的 32 bytes） |
| `AllowedOrigins__0` | 允許的前端來源（如 `http://localhost:5173`） |

產生加密金鑰：

```bash
python3 -c "import os, base64; print(base64.b64encode(os.urandom(32)).decode())"
```

> ⚠️ `Encryption__Key` 一旦用於加密資料就不可變更，否則既有密文將無法解密。

---

## 常用指令

```bash
dotnet restore                   # 還原套件
dotnet build                     # 建置（應為 0 warning）
dotnet run                       # 啟動 API（http://localhost:5037）
dotnet test                      # 執行測試

dotnet ef migrations add <name>  # 新增 migration
dotnet ef database update        # 套用 migration 到資料庫
```

啟動後：

- Swagger UI（僅開發環境）：http://localhost:5037/swagger
- 健康檢查：`GET http://localhost:5037/api/health`

首次啟動且連線字串有效時，會自動套用 migration 並執行 `DatabaseSeeder`（建立預設權限、角色、系統表單與通知範本）。

---

## 背景排程

`ScheduledMaintenanceService` 每 24 小時執行一次跨機構維護：

- 套餐到期轉 `expired`
- 考核可見性排程（到時開放 / 到期收回）
- 常態性預約續約提醒（剩餘 ≤ 2 次）

---

## 整合測試

`tests/integration-test.sh` 針對執行中的 API 跑全流程情境測試（需先啟動 API 與可連線的資料庫）：

```bash
dotnet run &                     # 於另一個終端啟動 API
bash tests/integration-test.sh
```
