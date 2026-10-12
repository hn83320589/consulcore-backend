# 諮心管理 ConsulCore — Backend

針對心理諮商所與個人執業心理師的全流程執業管理工具，本倉庫為後端 API。

- 框架：C# .NET 10 Web API
- ORM：Entity Framework Core 10
- 資料庫：SQLite（開發）；正式環境預留 MySQL / MariaDB，見 [`docs/database.md`](./docs/database.md)
- 認證：JWT Bearer Token（HS256）+ Policy-based 權限
- 加密：欄位層級 AES-256-GCM

前端倉庫：`consulcore-frontend`（Vue 3 SPA，獨立部署；畫面截圖見其 README）

## 功能亮點

- **全流程**：開設機構 → 排班與展班 → 預約（單次 / 常態）→ 同意書簽署 → SOAP 晤談紀錄與鎖定 → 自動收費、套餐扣抵、折扣 → 考核與違規管理 → 通知
- **多租戶與多角色**：每筆業務資料歸屬機構（EF Core Global Query Filter）；同一帳號可有多個角色，以 `X-Active-Role` 切換身份空間，且必須是 JWT 內確實擁有的角色（防提權）
- **個資保護**：個案資料、晤談紀錄、同意書等 28 個欄位以 AES-256-GCM 加密儲存；檢視個案與紀錄都留稽核，稽核紀錄不可刪除並可匯出 CSV / PDF
- **正確性**：收費冪等、時段樂觀併發（409）、診所時區、軟刪除保留歷史資料
- **API 契約**：Swashbuckle 產生 `openapi.json`，測試保證與程式碼一致；前端由此產生 TypeScript 型別
- **測試**：173 個 xUnit 測試（含以 `WebApplicationFactory` 起完整 API 的整合測試）＋ 73 項端對端情境，CI 每次都跑

## 架構

```
Controller（DTO 轉換、回傳 ApiResponse<T>）
   └─ Service（業務規則、稽核、通知）
        └─ AppDbContext（Global Query Filter 多租戶、加密 ValueConverter、軟刪除）
             └─ SQLite（開發）／ MySQL・MariaDB（正式環境預留）

橫切：JWT 驗證 → MustChangePasswordMiddleware → PermissionHandler（角色 × 權限碼）
      ErrorHandlingMiddleware（例外 → 狀態碼）、CorrelationIdMiddleware、ScheduledMaintenanceService
```

## 相關文件

- [`docs/test-report.md`](./docs/test-report.md) — **整合測試報告**（自動化測試、端對端情境、發現的問題）
- [`CLAUDE.md`](./CLAUDE.md) — 專案架構、業務規則、開發規範
- [`docs/frontend-integration.md`](./docs/frontend-integration.md) — **前後端介接契約**（API 端點、DTO/TypeScript 型別、enum 值、權限模型）。前端開發以此為唯一事實來源
- [`docs/backend-changes-for-frontend.md`](./docs/backend-changes-for-frontend.md) — **後端 → 前端**異動通知清單（需前端確認/處理的項目）
- [`docs/frontend-requested-endpoints.md`](./docs/frontend-requested-endpoints.md) — **前端 → 後端**待補端點清單（2026-08-15 已結案）
- [`SECURITY.md`](./SECURITY.md) — 機密管理與憑證輪替步驟
- [`docs/database.md`](./docs/database.md) — 資料庫設定、provider 中立規則、改用 MySQL 的上線步驟

---

## 環境需求

- [.NET SDK 10.0+](https://dotnet.microsoft.com/download)
- `sqlite3` CLI（整合測試用；macOS 內建）
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
| `Database__Provider` | 資料庫 provider（預設 `Sqlite`） |
| `ConnectionStrings__DefaultConnection` | 連線字串（預設 `Data Source=App_Data/consulcore.db`，已寫在 `appsettings.json`） |
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
dotnet test                      # 單元 + API 整合測試（不需啟動 API）
bash tests/integration-test.sh   # 端對端情境測試（需 API 執行中）
python3 scripts/seed-demo.py     # 灌入示範資料（需 API 執行中、空資料庫）

dotnet ef migrations add <name> -o Migrations/Sqlite  # 新增 migration
dotnet ef database update                             # 套用 migration 到資料庫
```

啟動後：

- Swagger UI（僅開發環境）：http://localhost:5037/swagger
- 健康檢查：`GET http://localhost:5037/api/health`（會確認資料庫連線，連不上回 503）
- 範例請求：[`consulcore.http`](./consulcore.http)（開設機構 → 登入 → 帶 token 與 `X-Active-Role` 呼叫 API）

### 示範資料

API 啟動後執行（只用 Python 標準函式庫）：

```bash
python3 scripts/seed-demo.py     # BASE 環境變數可指定 API 位址，預設 http://localhost:5037
```

會建立示範診所「晴光心理諮商所」：擁有者、行政、2 位心理師、6 位個案、兩週排班、預約、晤談紀錄、收費、套餐、同意書與考核。
資料全部透過 API 建立，走與正式操作相同的業務規則與稽核。需要空的資料庫；示範帳號已存在時會直接失敗並顯示原因。

| 身份 | 帳號 | 密碼 |
|------|------|------|
| 擁有者 | `demo_owner` | `Demo1234!` |
| 行政 | `demo_admin` | `Demo1234!` |
| 心理師 | `demo_chen`、`demo_lin` | `Demo1234!` |
| 個案 | 帳號為隨機產生，執行完會列出 | `Demo1234!` |

> 示範帳號只供本機展示，請勿在正式環境執行。

### 資料庫

首次啟動會在 `App_Data/` 建立 SQLite 資料庫、自動套用 migration 並執行 `DatabaseSeeder`（建立權限清單）。
**資料庫是空的，沒有預設帳號**：第一個帳號（機構擁有者）用 `POST /api/auth/register` 開設機構時建立，
預設角色、系統表單與通知範本也在此時產生。可直接執行 `consulcore.http` 的第 1 步，或在前端登入頁之外以 Swagger 呼叫。
重置資料庫：停止 API 後刪除 `App_Data/consulcore.db*`。

---

## 背景排程

`ScheduledMaintenanceService` 每天診所時間 03:00（`Clinic:TimeZone`，預設 Asia/Taipei）執行跨機構維護，結果寫入稽核（操作者為「系統」）：

- 套餐到期轉 `expired`
- 考核可見性排程（到時開放 / 到期收回）
- 常態性預約續約提醒（剩餘 ≤ 2 次；每個系列只提醒一次）

每個請求都有追蹤碼 `X-Correlation-Id`（沿用呼叫端送來的或自動產生），會出現在回應 header、log 與 500 錯誤訊息中。

---

## 整合測試

`tests/integration-test.sh` 針對執行中的 API 跑全流程情境測試，並直接查 SQLite 驗證欄位已加密（`DB_PATH` 預設 `App_Data/consulcore.db`）。建議從空資料庫開始跑：

```bash
dotnet run &                     # 於另一個終端啟動 API
bash tests/integration-test.sh
```

測試結果與發現的問題整理在 [`docs/test-report.md`](./docs/test-report.md)。
