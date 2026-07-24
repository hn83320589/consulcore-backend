# 諮心管理 ConsulCore — Backend

> .NET 10 Web API | Phase 1 | 最後更新 2026-04-03

---

## 專案概述

諮心管理（ConsulCore）是一套針對心理諮商所與個人執業心理師的全流程執業管理工具。本倉庫為後端 API 專案。

前端倉庫：`consulcore-frontend`（Vue 3 SPA，獨立部署）

### 技術棧

| 項目 | 技術 | 版本 |
|------|------|------|
| 框架 | C# .NET Core Web API | 10.0 |
| ORM | Entity Framework Core + Pomelo MySQL | 10.x |
| 資料庫 | MySQL / MariaDB | 8.x+ |
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

演算法：AES-256-GCM。密鑰從環境變數 `ENCRYPTION_KEY` 讀取，不寫在 appsettings.json。

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

`PermissionHandler` 從 JWT 的 activeRole + DB 中的 RolePermission 表檢查權限。

### 4. 多角色 JWT

JWT Payload 包含所有角色，前端選擇 activeRole 後透過 Header 告知：

```
JWT Claims: { userId, organizationId, roles: ["therapist","client"], username }
Request Headers: X-Active-Role: therapist, X-Organization-Id: 1
```

### 5. 稽核

所有需稽核的操作在 Service 層呼叫 `IAuditService.LogAsync()`：

```csharp
await _audit.LogAsync("update", "session_note", noteId, "解鎖晤談紀錄",
    new { before = snapshot, after = updated });
```

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

## 資料模型（31 張表）

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
FeeSchedule, Package, ClientPackage, Discount, ClientDiscount, PaymentRecord（original + final amount）

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
| BR-11 | 折扣可綁定角色標籤自動套用 |
| BR-12 | 必要通知不可關閉 |
| BR-13 | 稽核紀錄不可刪除 |
| BR-14 | 同意書簽署保存內容快照與版本號 |
| BR-15 | 個案紀錄至少保存 10 年 |
| BR-16 | 個案備忘跟著個案走，接手心理師唯讀 |
| BR-17 | 定期考核預設 hidden，Owner 手動/排程開放 |
| BR-18 | 量化指標心理師可見，質性考核僅 Owner |
| BR-19 | 同一帳號可擁有多角色，身份空間隔離 |

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
- 資料庫欄位用 snake_case（EF Core `.ToSnakeCase()`）
- 稽核觸發點在 Service 層呼叫 `IAuditService.LogAsync()`

## 設定

```json
{
  "ConnectionStrings": { "DefaultConnection": "" },
  "Jwt": { "SecretKey": "", "Issuer": "ConsulCore", "ExpiryHours": 24 },
  "Encryption": { "Key": "" },
  "AllowedOrigins": ["http://localhost:5173"]
}
```

實際密鑰透過環境變數注入，不寫在設定檔。

## 常用指令

```bash
dotnet run                       # 啟動 API (http://localhost:5037)
dotnet ef migrations add <name>  # 新增 migration
dotnet ef database update        # 更新資料庫
dotnet build                     # 建置
dotnet test                      # 測試
```

## 最新異動記錄

| 日期 | 異動 |
|------|------|
| 2026-04-03 | 初始版本，Phase 1 完整架構 |
