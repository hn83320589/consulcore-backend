# 諮心管理 ConsulCore — Backend

> .NET 10 Web API | Phase 1

---

## 專案概述

諮心管理（ConsulCore）是一套針對心理諮商所與個人執業心理師的全流程執業管理工具。本倉庫為後端 API 專案。

前端倉庫：`consulcore-frontend`（Vue 3 SPA，獨立部署）

### 技術棧

| 項目 | 技術 | 版本 |
|------|------|------|
| 框架 | C# .NET Core Web API | 10.0 |
| ORM | Entity Framework Core | 10.0 |
| 資料庫 | SQLite（開發）；正式環境預留 MySQL / MariaDB | — |
| 認證 | JWT Bearer Token (HS256) | — |
| 密碼 | BCrypt.Net-Next | — |
| API 文件 | Swagger / Swashbuckle | — |

### 專案結構

```
consulcore-backend/
├── CLAUDE.md
├── Controllers/           # API 控制器
├── Services/              # 業務邏輯
├── Models/                # EF Core Entity
├── DTOs/                  # 每 Entity 三層 DTO
│   └── EntityName/
│       ├── CreateEntityNameDto.cs
│       ├── UpdateEntityNameDto.cs
│       └── EntityNameResponseDto.cs
├── Data/
│   ├── AppDbContext.cs    # DB Context + Global Query Filter
│   ├── DatabaseSeeder.cs  # 初始資料
│   └── Encryption/        # 加密服務 + ValueConverter
├── Authorization/         # Policy-based 權限
├── Middleware/
│   └── ErrorHandlingMiddleware.cs
├── tests/
│   ├── ConsulCore.Tests/   # xUnit：Unit/ 純邏輯、Integration/ 以暫存 SQLite 起完整 API
│   └── integration-test.sh # 對執行中 API 的端對端情境（CI 也會跑）
├── Program.cs
└── appsettings.json
```

---

## 核心架構決策

### 1. 多租戶隔離

所有業務資料表都有 `OrganizationId`。使用 EF Core Global Query Filter：

```csharp
// AppDbContext.cs — OnModelCreating
builder.Entity<ClientProfile>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
```

`_currentOrgId` 從 JWT Claims 的 `organizationId` 取得，透過 `IHttpContextAccessor` 注入 AppDbContext。

### 2. 欄位層級加密

```csharp
// 自訂屬性標記
[Encrypted]
public string Name { get; set; }

// ValueConverter 自動加解密
public class EncryptedStringConverter : ValueConverter<string, string>
{
    public EncryptedStringConverter(IEncryptionService enc)
        : base(v => enc.Encrypt(v), v => enc.Decrypt(v)) { }
}

// OnModelCreating 中自動套用所有標記 [Encrypted] 的 Property
```

演算法：AES-256-GCM。密鑰從設定鍵 `Encryption:Key` 讀取（環境變數 `Encryption__Key`），不寫在 appsettings.json。

### 3. 權限檢查

Policy-based Authorization，對應 Permission 表：

```csharp
// 註冊
options.AddPolicy("CanManageClients", policy =>
    policy.Requirements.Add(new PermissionRequirement("client.manage")));

// 使用
[Authorize(Policy = "CanManageClients")]
public async Task<IActionResult> CreateClient(...) { }
```

`PermissionHandler` 從 `X-Active-Role` header 取 activeRole + DB 中的 RolePermission 表檢查權限。

> ⚠️ **安全（勿回歸）**：`X-Active-Role` 是 client 可控 header。`PermissionHandler`
> **必須**先驗證 activeRole 存在於 JWT 的 `roles` claim 才受理，否則任何登入者送
> `X-Active-Role: owner` 即可提權。Controller 內依 `X-Active-Role` 做 scope 判斷處
> （如 `ClientProfilesController.HasFullClientScope`）同樣要對照 JWT roles 驗證。

### 4. 多角色 JWT

JWT Payload 包含所有角色，前端選擇 activeRole 後透過 Header 告知：

```
JWT Claims: { userId, organizationId, roles: ["therapist","client"], username, mustChangePassword }
Request Headers: X-Active-Role: therapist   （organizationId 只從 JWT 取，不收 header）
```

`mustChangePassword=true`（個案以櫃檯交付的隨機初始密碼首次登入）時，`MustChangePasswordMiddleware`
攔截除 `/api/auth/change-password`、`/api/auth/me`、`/api/auth/logout` 以外的所有請求回 403，
強制先改密碼；改密碼後需重新登入才會拿到 `mustChangePassword=false` 的新 token。

### 5. 稽核

所有需稽核的操作在 Service 層呼叫 `IAuditService.LogAsync()`：

```csharp
await _audit.LogAsync("update", "session_note", noteId, "解鎖晤談紀錄",
    new { before = snapshot, after = updated });
```

必記觸發點含各資料的**檢視**（ClientProfile GetById/Search、SessionNote by-client/by-appointment）。

### 6. 背景排程

`ScheduledMaintenanceService`（`IHostedService`，每 24 小時）跨機構執行系統維護：

- 套餐到期 → `expired`
- 考核可見性排程（`VisibleFrom` 到時開放 / `VisibleUntil` 到期收回，BR-17）
- 常態預約續約提醒（剩餘 ≤ 2 次 → 通知心理師）

> 背景執行無 HttpContext，`_currentOrgId` 為 null 會讓 Global Query Filter 過濾掉所有列。
> 跨機構維護任務**必須**用 `IgnoreQueryFilters()` 才掃得到全機構資料。

---

## 統一回應格式

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string msg = "操作成功");
    public static ApiResponse<T> Fail(string msg, List<string>? errors = null);
}
```

---

## 資料模型（36 張表）

### 帳號與組織（6）
Organization, User, UserRole, Role, Permission, RolePermission

### 個案（2）
ClientProfile（14 欄位全加密, user_id nullable）, TherapistClient（含 status）

### 同意書（2）
ConsentTemplate（版本控制 + 觸發事件）, ConsentRecord（快照 + 簽名, 全加密）

### 診間與排班（4）
Room, TherapistRoom, ScheduleTemplate, ScheduleSlot

### 預約（2）
Appointment（含 session_type, visit_type）, AppointmentSeries

### 動態表單（5）
FormTemplate → FormSection → FormField, FormSubmission → FormFieldValue（值加密）

### 晤談紀錄（3）
SessionNote（SOAP 各欄位加密）, PhraseTemplate, ClientMemo（加密）

### 收費（6）
FeeSchedule, Package, ClientPackage, Discount（therapist_user_id 可空=機構通用）, ClientDiscount, PaymentRecord（original + final amount）

### 考核（2）
PerformanceReview（含 visibility 控制）, ViolationRecord（加密）

### 通知（3）
NotificationTemplate, NotificationLog, NotificationPreference

### 稽核（1）
AuditLog（不可刪除, changes 加密）

---

## 業務規則

| ID | 規則 |
|----|------|
| BR-01 | 個案資料歸屬機構，非個別心理師 |
| BR-02 | 同時段最大諮商數 = 啟用中診間數 |
| BR-03 | 心理師固定診間 |
| BR-04 | 展班不覆蓋已存在時段 |
| BR-05 | 不可調整過去日期班表 |
| BR-06 | 個案可自由選擇心理師，系統溫和提示 |
| BR-07 | 常態性預約心理師手動標記，上限 3 個月 |
| BR-08 | 晤談紀錄鎖定可逆，留完整稽核 |
| BR-09 | 費率查找：心理師專屬 > 機構通用 |
| BR-10 | 套餐剩餘 2 次觸發提醒 |
| BR-11 | 折扣可綁定角色標籤自動套用；折扣分心理師專屬與機構通用，查找時專屬優先（比照 BR-09） |
| BR-12 | 必要通知不可關閉 |
| BR-13 | 稽核紀錄不可刪除 |
| BR-14 | 同意書簽署保存內容快照與版本號 |
| BR-15 | 個案紀錄至少保存 10 年 |
| BR-16 | 個案備忘跟著個案走，接手心理師唯讀 |
| BR-17 | 定期考核預設 hidden，Owner 手動/排程開放 |
| BR-18 | 量化指標心理師可見，質性考核僅 Owner |
| BR-19 | 同一帳號可擁有多角色，身份空間隔離 |
| BR-20 | 代診/交接：therapist 可**唯讀**非自己指派個案的紀錄（個別紀錄，非清單），寫入仍限紀錄建立者；跨心理師檢視**必須留稽核** |

---

## API 路由總覽

| Controller | 路由前綴 | 說明 |
|-----------|---------|------|
| AuthController | /api/auth | login, register, me, change-password |
| UsersController | /api/users | CRUD (Owner) |
| RolesController | /api/roles | 角色 + 權限矩陣 |
| ClientProfilesController | /api/client-profiles | CRUD + assign/transfer + search |
| ConsentTemplatesController | /api/consent-templates | CRUD |
| ConsentRecordsController | /api/consent-records | sign / void / list |
| RoomsController | /api/rooms | CRUD |
| ScheduleTemplatesController | /api/schedule-templates | CRUD |
| ScheduleSlotsController | /api/schedule-slots | expand / block / unblock |
| AppointmentsController | /api/appointments | CRUD + status 流轉 + calendar/list |
| AppointmentSeriesController | /api/appointment-series | create / renew / end |
| FormTemplatesController | /api/form-templates | CRUD + clone |
| FormSubmissionsController | /api/form-submissions | submit / get |
| SessionNotesController | /api/session-notes | CRUD + lock/unlock |
| PhraseTemplatesController | /api/phrase-templates | CRUD |
| ClientMemosController | /api/client-memos | CRUD |
| FeeSchedulesController | /api/fee-schedules | CRUD |
| PackagesController | /api/packages | CRUD |
| BillingController | /api/billing | confirm / refund / waive / purchase |
| DiscountsController | /api/discounts | CRUD + assign/remove |
| ReportsController | /api/reports | revenue / stats |
| NotificationTemplatesController | /api/notification-templates | CRUD |
| NotificationsController | /api/notifications | list / unread / mark-read |
| NotificationPreferencesController | /api/notification-preferences | get / update |
| PerformanceReviewsController | /api/performance-reviews | CRUD + visibility + metrics |
| ViolationRecordsController | /api/violation-records | CRUD + resolve/dismiss |
| AuditLogsController | /api/audit-logs | list + export |

---

## 開發規範

- 每個 Entity 都需要 3 層 DTO：CreateDto / UpdateDto / ResponseDto
- Service 層處理業務邏輯，Controller 只做 DTO 轉換 + 回傳 ApiResponse
- 所有含 OrganizationId 的 Entity 加 Global Query Filter
- 加密欄位標記 `[Encrypted]`，OnModelCreating 自動套用 converter
- Enum 用 string 儲存（`.HasConversion<string>()`）
- Enum 欄位必須有預設值，不允許 null
- API 路由用 kebab-case
- 資料庫表名/欄位用 snake_case：Model 上明確標註 `[Table("snake_name")]` 與 `[Column("snake_name")]`（專案未使用自動命名慣例）
- 稽核觸發點在 Service 層呼叫 `IAuditService.LogAsync()`
- 新功能與 bug 修正先寫測試；API 行為用 `Integration/` 的 `ConsulCoreApiFactory` + `ApiTestClient` 測
- 錯誤以例外表達，由 `ErrorHandlingMiddleware` 對應狀態碼：`BusinessRuleException` → 400、`ForbiddenException` → 403、`KeyNotFoundException` → 404、`DbUpdateConcurrencyException` → 409，訊息會回給前端。**不要**用 `InvalidOperationException`（框架也會丟，訊息不外洩、回 500）或 `UnauthorizedAccessException`（401 = 登入失效，前端會登出；只用於登入失敗）
- 資料存取保持 provider 中立（不寫 raw SQL、不指定資料庫型別名稱、decimal 用 `[Precision]`），規則見 `docs/database.md`

## 設定

```json
{
  "Database": { "Provider": "Sqlite" },
  "ConnectionStrings": { "DefaultConnection": "Data Source=App_Data/consulcore.db" },
  "Jwt": { "SecretKey": "", "Issuer": "ConsulCore", "ExpiryHours": 24 },
  "Encryption": { "Key": "" },
  "AllowedOrigins": ["http://localhost:5173"]
}
```

SQLite 連線字串不是機密，直接寫在 `appsettings.json`；`Jwt:SecretKey`、`Encryption:Key` 透過環境變數或 gitignored 的 `appsettings.Development.json` 注入。

## 常用指令

```bash
dotnet run                       # 啟動 API (http://localhost:5037)
dotnet ef migrations add <name> -o Migrations/Sqlite  # 新增 migration
dotnet ef database update        # 更新資料庫
dotnet build                     # 建置（警告視為錯誤）
dotnet test                      # 單元 + API 整合測試（tests/ConsulCore.Tests）
bash tests/integration-test.sh   # 整合測試（需先 dotnet run；直接查 App_Data/consulcore.db 驗證加密）
```

## 最新異動記錄

| 日期 | 異動 |
|------|------|
| 2026-04-03 | 初始版本，Phase 1 完整架構 |
| 2026-07-30 | 稽核修復（安全/通知/排程/正確性）；修復 X-Active-Role 偽造提權；新增 MustChangePasswordMiddleware 與 ScheduledMaintenanceService；Discount 改 therapist 層級（+TherapistUserId 可空）；通知 Seeder 補至 24 筆（8 event×3 channel）；稽核新增 PDF 匯出；密鑰移出版控；新增前端介接文件 `docs/frontend-integration.md` 與 `SECURITY.md` |
| 2026-08-15 | IDOR 改 fail-closed（新增 `Authorization/ActiveRoleScope`，以 JWT 驗證角色取代可偽造 header）；BR-20 代診/交接唯讀（放寬 ClientProfile 單筆 403、補 ClientMemo/FormSubmission 檢視稽核、FormSubmission 補權限閘）；`.claude/settings.json` 移出版控 |
| 2026-10-11 | 資料庫改為 SQLite（EF Core 10），以 `Database:Provider` 保留正式環境改用 MySQL/MariaDB 的路徑；舊 MySQL migration 重建為 `Migrations/Sqlite/InitialCreate`；整合測試改查 SQLite。詳見 `docs/database.md` |
| 2026-10-11 | 新增 xUnit 測試專案（單元 + WebApplicationFactory 整合測試）、GitHub Actions CI、TreatWarningsAsErrors |
| 2026-10-11 | 重構 Phase 2：表單範本權限、擁有者保護、JWT 金鑰檢查、登入限流與失敗稽核、隨機初始密碼、收費冪等、時段樂觀併發（409）、例外對應整理（BusinessRuleException / ForbiddenException，模型驗證回 ApiResponse） |
