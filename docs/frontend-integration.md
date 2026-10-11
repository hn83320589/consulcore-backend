# ConsulCore 前後端介接契約（前端開發指南）

> 對象：`consulcore-frontend`（Vue 3 + TypeScript）開發者 / Claude Code
> 目的：讓前端照著這份「唯一事實來源」開發，避免與後端資料結構對不上。
> 來源：直接從後端 `.NET 10` 程式碼萃取（Controllers / DTOs / Models / Middleware）。
> 最後同步：2026-08-02。後端有異動時，以程式碼與此文件為準，勿憑印象猜測欄位。

> 📣 **2026-08-15 有一批安全性異動需要前端確認** → 請先看 [`backend-changes-for-frontend.md`](./backend-changes-for-frontend.md)
> （其中 **P0-1『form-submissions 補權限閘』需前端回覆**，其餘多為不影響前端或放寬）。

**使用原則**
- 所有型別以本文件的 **TypeScript 定義**為準，直接複製到前端型別檔，欄位對不上時 TS 會編譯報錯——這正是防呆機制。
- 列舉（enum）值必須**一字不差**（全小寫 / snake_case），例如 `follow_up`、`no_show`、`in_app`、`fixed_amount`。
- 開發環境另有 Swagger UI：`http://localhost:5037/swagger`（僅開發環境開放），可作互動式對照。

---

## 1. 基本資訊

| 項目 | 值 |
|------|-----|
| 開發環境 Base URL | `http://localhost:5037` |
| API 路由前綴 | `/api` |
| 認證方式 | JWT Bearer Token（HS256） |
| 內容型別 | `application/json`（除稽核匯出為檔案下載） |
| CORS 允許來源 | `http://localhost:5173`（前端 dev server） |
| 日期時間序列化 | C# `DateTime` → ISO 8601 字串（如 `"2026-07-30T15:04:05Z"`） |
| 金額 | `decimal` → JSON number |

日期/時間中，部分欄位後端型別本身就是**字串**（非 DateTime）：
- `date` 類：`"yyyy-MM-dd"`（如 ScheduleSlot.date、Appointment.date）
- `startTime` / `endTime`：時間字串（送出時用 `"HH:mm"`，回應可能為 `"HH:mm:ss"`，前端當字串處理）
- `dateOfBirth` / `incidentDate`：`"yyyy-MM-dd"` 字串

---

## 2. 統一回應封套 `ApiResponse<T>`

**除了稽核匯出（檔案下載）外，所有 API 回應都是這個形狀。** 前端應統一在 axios/fetch 攔截器拆封。

```ts
interface ApiResponse<T> {
  success: boolean;      // 操作是否成功
  message: string;       // 提示訊息（可直接顯示給使用者）
  data: T | null;        // 成功時的資料；失敗時為 null
  errors: string[] | null; // 失敗時的細節（多半為 null，開發環境 500 會帶 stack）
}
```

實際範例：
```json
{ "success": true, "message": "操作成功", "data": { "id": 1 }, "errors": null }
{ "success": false, "message": "帳號或密碼錯誤", "data": null, "errors": null }
```

> JSON key 一律 **camelCase**（後端全站設定 `JsonNamingPolicy.CamelCase`）。本文件的 TS 型別已是 camelCase。

建議前端封裝：
```ts
async function apiCall<T>(...): Promise<T> {
  const res = await http.request<ApiResponse<T>>(...);
  if (!res.data.success) throw new ApiError(res.data.message, res.status, res.data.errors);
  return res.data.data as T;
}
```

---

## 3. 錯誤處理（HTTP 狀態碼）

後端全域例外中介層依例外型別對應狀態碼，body 仍是 `ApiResponse`（`success:false`）：

| HTTP | 意義 | 前端建議處理 |
|------|------|-------------|
| 400 | 參數/業務規則錯誤（ArgumentException / InvalidOperationException） | 顯示 `message` |
| 401 | 未認證 / 帳密錯誤（UnauthorizedAccessException） | 導向登入 |
| 403 | 已登入但無權限，或越權存取 | 顯示「無權限」，不要當登入失效 |
| 404 | 資源不存在（KeyNotFoundException） | 顯示 `message` |
| 500 | 未預期錯誤 | 顯示通用錯誤；`message` 生產環境為固定文案 |

> 400/401/404 的 `message` 是後端業務訊息，可安全顯示。403 可能來自 policy 授權，也可能來自 controller 主動回傳（如個案越權），body 都是 `ApiResponse.Fail`。

---

## 4. 認證與身份模型

### 4.1 登入 / 註冊

```ts
// POST /api/auth/login
interface LoginRequest { username: string; password: string; }

// POST /api/auth/register（建立新機構 + Owner 帳號）
interface RegisterRequest {
  organizationName: string;   // 必填, ≤100
  username: string;           // 必填, ≤50
  email: string;              // 必填, email, ≤200
  password: string;           // 必填, 8–100
  displayName: string;        // 必填, ≤50
  phone?: string | null;      // ≤20
}

// login 與 register 皆回傳 LoginResponse
interface LoginResponse {
  token: string;
  userId: number;
  username: string;
  displayName: string;
  email: string;
  organizationId: number;
  organizationName: string;
  roles: string[];              // 使用者擁有的所有角色
  mustChangePassword: boolean;  // 首次登入預設密碼時為 true
}
```

### 4.2 JWT Claims（前端解 token 時可用的 key）

```
userId              : "12"                      （字串化整數）
organizationId      : "3"                       （字串化整數）
username            : "alice"
roles               : "[\"owner\",\"therapist\"]"  （JSON 陣列「字串」，需 JSON.parse）
mustChangePassword  : "true" | "false"          （字串）
```

> ⚠️ `roles` 是**序列化後的 JSON 陣列字串**，不是多個 claim、也不是逗號分隔。解讀要先 `JSON.parse`。Token 有效期 24 小時。

### 4.3 每次請求要帶的 Header

```
Authorization: Bearer <token>
X-Active-Role: <目前作用中的角色>      // 例如 owner / admin / therapist / client
```

- **多角色身份隔離（BR-19）**：一個帳號可有多角色（如某人同時是 therapist 又是 client）。前端讓使用者選擇「目前身份」，並在每次請求帶 `X-Active-Role`。後端據此決定權限與資料範圍。
- **安全保證**：後端會驗證 `X-Active-Role` 必須是該使用者 JWT `roles` 內真的擁有的角色，**偽造無效**。送一個自己沒有的角色 → 該請求視同無權限（403）。所以前端只能在使用者實際擁有的 `roles` 中切換。

### 4.4 首次登入強制改密碼流程

個案帳號由機構代建時，帳號（`client_{機構}_{隨機}`）與**隨機初始密碼**只在建立的回應 `initialCredentials` 中回傳一次（明文不保存），由櫃檯當場交付；`mustChangePassword=true`。

- 此狀態下，後端中介層會**攔截除下列以外的所有請求並回 403**（message：`首次登入請先變更密碼後才能使用其他功能`）：
  - `PUT /api/auth/change-password`
  - `GET /api/auth/me`
  （沒有 `/api/auth/logout` 端點：登出是純前端清除狀態）
- 前端流程：登入 → 若 `mustChangePassword` → 強制導到改密碼頁 → 改成功後**重新登入**（改密碼不會即時換發 token，需重登才拿到 `mustChangePassword=false` 的新 token）。

```ts
// PUT /api/auth/change-password（需 Bearer）
interface ChangePasswordRequest { currentPassword: string; newPassword: string; } // newPassword 8–100

// GET /api/auth/me（需 Bearer + X-Active-Role）
interface CurrentUser {
  id: number;
  username: string;
  email: string;
  displayName: string;
  phone?: string | null;
  organizationId: number;
  organizationName: string;
  roles: string[];              // 全部角色
  activeRole?: string | null;   // 目前 X-Active-Role
  permissions: string[];        // 依 activeRole 對應的權限碼（見第 5 節）
  clientProfileId?: number | null; // 若此帳號是個案，對應的 ClientProfile.id（個案端查自己資料用）
}
```

> ⚠️ **個案端查詢用 `clientProfileId`，不要用 `userId`**。使用者的 `userId`（帳號 PK）與其
> `clientProfileId`（個案檔 PK）是**不同的值**。client 角色要查自己的預約/同意書/套餐時，
> 一律用 `/me` 回傳的 `clientProfileId` 當作 API 的 `clientProfileId` 參數。若帳號沒有對應個案檔則為 `null`。

**Auth 端點總表**

| Method | 路由 | Body | Response data | 需 Bearer |
|--------|------|------|---------------|-----------|
| POST | `/api/auth/login` | `LoginRequest` | `LoginResponse` | 否 |
| POST | `/api/auth/register` | `RegisterRequest` | `LoginResponse` | 否 |
| GET | `/api/auth/me` | — | `CurrentUser` | 是 |
| PUT | `/api/auth/change-password` | `ChangePasswordRequest` | `{}` | 是 |

---

## 5. 權限模型（前端做 UI 權限控制用）

前端**不要**用角色名稱硬判斷，而是用 `GET /api/auth/me` 回傳的 `permissions: string[]`（權限碼）來 gate UI（顯示/隱藏按鈕）。實際的存取控制由後端強制，前端僅為體驗。

### 5.1 全部權限碼（32 個）

```
client.view  client.create  client.update  client.delete  client.manage
appointment.view  appointment.create  appointment.update  appointment.delete
schedule.view  schedule.create  schedule.update  schedule.delete
session.view  session.create  session.update  session.lock
consent.view  consent.create  consent.update  consent.void
billing.view  billing.create  billing.update  billing.refund
notification.manage  staff.manage  room.manage  audit.view
performance.view  performance.manage  org.settings
```

### 5.2 預設 4 角色的權限

| 角色 | 權限數 | 摘要 |
|------|--------|------|
| **owner** | 32 | 全部權限 |
| **admin** | 24 | 個案（無 delete）、預約、排班、收費（無 refund）、人員/診間/通知管理、稽核檢視、考核檢視；**無**：`client.delete`、`session.create/update/lock`、`consent.void`、`billing.refund`、`performance.manage`、`org.settings` |
| **therapist** | 13 | `client.view/create/update`、`appointment.view/create/update`、`schedule.view`、`session.view/create/update/lock`、`consent.view/create`（不含 `billing.view`：收費 / 報表為全機構資料） |
| **client** | 2 | `appointment.view`、`consent.view` |

> 角色權限可由 Owner 透過 `PUT /api/roles/{id}/permissions` 調整，所以前端務必以 `/me` 的即時 `permissions` 為準，不要寫死上表。

---

## 6. 列舉（Enum）字串值 — 最容易對錯，務必照抄

後端所有 enum 以**字串**儲存。前端建議用 TS union type 或 const 物件鎖定合法值。

```ts
// 預約
type SessionType = 'individual' | 'couple' | 'family';           // 預設 individual
type VisitType   = 'initial' | 'follow_up';                      // 預設 follow_up
type AppointmentStatus = 'scheduled' | 'completed' | 'cancelled' | 'no_show'; // 預設 scheduled

// 個案
type ClientStatus = 'pending' | 'active' | 'suspended' | 'closed';           // 預設 pending
type TherapistClientStatus = 'active' | 'transferred' | 'closed';            // 預設 active

// 同意書
type ConsentTriggerEvent = 'manual' | 'first_visit' | 'annual_renewal' | 'service_change'; // 預設 manual
type ConsentRecordStatus = 'pending' | 'signed' | 'voided';                  // 預設 pending

// 排班 / 常態預約
type SlotStatus = 'available' | 'reserved' | 'blocked' | 'cancelled';        // 預設 available
type SeriesFrequency = 'weekly' | 'biweekly';                                // 預設 weekly
type SeriesStatus = 'active' | 'pending_confirmation' | 'ended';             // 預設 active

// 動態表單 / 晤談紀錄
type FieldType = 'text' | 'textarea' | 'select' | 'radio' | 'checkbox' | 'date' | 'number' | 'scale'; // 預設 text
type FormTemplateType = 'intake_assessment' | 'custom';                      // 預設 intake_assessment
type SessionNoteType = 'session' | 'closure';                                // 預設 session
type SessionNoteFormat = 'soap' | 'free_text';                               // 預設 soap
type PhraseCategory = 'subjective' | 'objective' | 'assessment' | 'plan' | 'general'; // 預設 general
type MemoCategory = 'personality' | 'trigger' | 'preference' | 'family' | 'other';    // 預設 other

// 收費
type PaymentMethod = 'cash' | 'transfer' | 'credit_card' | 'line_pay' | 'other';     // 預設 cash
type PaymentType = 'single_session' | 'package_purchase';                    // 預設 single_session
type PaymentStatus = 'paid' | 'pending' | 'refunded' | 'waived';             // 預設 pending
type DiscountType = 'percentage' | 'fixed_amount';                           // 預設 percentage
type ClientPackageStatus = 'active' | 'expired' | 'completed';               // 預設 active

// 通知
type NotificationEventType =
  | 'account_created' | 'appointment_created' | 'appointment_cancelled'
  | 'appointment_reminder' | 'therapist_leave' | 'series_renewal'
  | 'package_low' | 'package_expiring';
type NotificationChannel = 'email' | 'sms' | 'in_app';                       // 預設 in_app
type NotificationStatus = 'queued' | 'sent' | 'failed' | 'read';             // 預設 queued

// 考核 / 違規
type ReviewVisibility = 'hidden' | 'visible';                               // 預設 hidden
type ViolationSeverity = 'low' | 'medium' | 'high' | 'critical';            // 預設 low
type ViolationStatus = 'open' | 'resolved' | 'dismissed';                   // 預設 open
```

---

## 7. 端點與型別參考（依領域）

> 慣例：
> - 所有 `organizationId` / `userId` / `reviewerId` 由後端從 JWT 取得，**不放在 request body**。
> - 除 Auth 外所有端點需 `Authorization: Bearer` 與 `X-Active-Role`。
> - 下表「Policy」為後端授權所需權限；前端用 `/me` 的 `permissions` 判斷是否顯示對應功能。Response 皆包在 `ApiResponse<T>`，下方標的是 `T`。

### 7.1 使用者 User（`/api/users`，Policy: staff.manage）

```ts
interface CreateUserRequest {
  username: string;    // 必填, ≤50
  email: string;       // 必填, email, ≤200
  password: string;    // 必填, 8–100
  displayName: string; // 必填, ≤50
  phone?: string | null;
  roles: string[];     // 必填, 至少 1 個角色名
}
interface UpdateUserRequest {   // 皆選填
  email?: string | null;
  displayName?: string | null;
  phone?: string | null;
  roles?: string[] | null;
}
interface ResetPasswordRequest { newPassword: string; } // 8–100
interface UserResponse {
  id: number; username: string; email: string; displayName: string;
  phone?: string | null; isActive: boolean; roles: string[];
  createdAt: string; updatedAt: string;
}
```

| Method | 路由 | Body | Response |
|--------|------|------|----------|
| GET | `/api/users` | — | `UserResponse[]` |
| GET | `/api/users/{id}` | — | `UserResponse` |
| POST | `/api/users` | `CreateUserRequest` | `UserResponse` |
| PUT | `/api/users/{id}` | `UpdateUserRequest` | `UserResponse` |
| DELETE | `/api/users/{id}` | — | `{}`（不可刪除 owner） |
| POST | `/api/users/{id}/reset-password` | `ResetPasswordRequest` | `{}` |
| PATCH | `/api/users/{id}/toggle-active` | — | `UserResponse` |

### 7.2 角色權限 Role（`/api/roles`，Policy: staff.manage）

```ts
interface UpdateRolePermissionsRequest { permissionCodes: string[]; }
interface RoleResponse {
  id: number; name: string; description?: string | null;
  isSystemDefault: boolean; permissions: string[]; // 權限碼
}
```

| Method | 路由 | Body | Response |
|--------|------|------|----------|
| GET | `/api/roles` | — | `RoleResponse[]` |
| PUT | `/api/roles/{id}/permissions` | `UpdateRolePermissionsRequest` | `RoleResponse` |

### 7.3 個案 ClientProfile（`/api/client-profiles`）

```ts
interface CreateClientProfileRequest {
  name: string;                 // 必填（加密儲存）
  idNumber?: string | null;
  gender?: string | null;
  dateOfBirth?: string | null;  // "yyyy-MM-dd"
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  emergencyContactName?: string | null;
  emergencyContactPhone?: string | null;
  emergencyContactRelationship?: string | null;
  occupation?: string | null;
  referralSource?: string | null;
  notes?: string | null;
  medicalHistory?: string | null;
  createAccount: boolean;       // true=同時建個案帳號（帳密見回應的 initialCredentials，只回傳一次）
  therapistUserId?: number | null; // 建檔時指派心理師
}
// UpdateClientProfileRequest：同上前 14 個欄位，皆選填，無 createAccount/therapistUserId
interface ChangeStatusRequest { status: ClientStatus; }
interface AssignTherapistRequest { therapistUserId: number; }
interface TransferTherapistRequest { newTherapistUserId: number; reason?: string | null; }

interface TherapistInfo { therapistUserId: number; therapistName: string; status: string; assignedAt: string; }
interface ClientProfileResponse {
  id: number; name: string;
  idNumber?: string | null; gender?: string | null; dateOfBirth?: string | null;
  phone?: string | null; email?: string | null; address?: string | null;
  emergencyContactName?: string | null; emergencyContactPhone?: string | null;
  emergencyContactRelationship?: string | null; occupation?: string | null;
  referralSource?: string | null; notes?: string | null; medicalHistory?: string | null;
  status: ClientStatus; userId?: number | null; mustChangePassword: boolean;
  initialCredentials?: { username: string; password: string }; // 只出現在「建立附帳號個案」的回應
  therapists: TherapistInfo[]; createdAt: string; updatedAt: string;
}
```

| Method | 路由 | Policy | Body / Query | Response |
|--------|------|--------|--------------|----------|
| GET | `/api/client-profiles` | client.view | — | `ClientProfileResponse[]` |
| GET | `/api/client-profiles/{id}` | client.view | — | `ClientProfileResponse` |
| GET | `/api/client-profiles/by-therapist/{therapistUserId}` | client.view | — | `ClientProfileResponse[]` |
| GET | `/api/client-profiles/my-clients` | client.view | — | `ClientProfileResponse[]` |
| GET | `/api/client-profiles/search` | client.view | `?keyword=&idNumberLast4=` | `ClientProfileResponse[]` |
| POST | `/api/client-profiles` | client.create | `CreateClientProfileRequest` | `ClientProfileResponse` |
| PUT | `/api/client-profiles/{id}` | client.update | `UpdateClientProfileRequest` | `ClientProfileResponse` |
| DELETE | `/api/client-profiles/{id}` | client.delete | — | `{}` |
| POST | `/api/client-profiles/{id}/assign-therapist` | client.manage | `AssignTherapistRequest` | `ClientProfileResponse` |
| POST | `/api/client-profiles/{id}/transfer-therapist/{currentTherapistUserId}` | client.manage | `TransferTherapistRequest` | `ClientProfileResponse` |
| PATCH | `/api/client-profiles/{id}/status` | client.manage | `ChangeStatusRequest` | `ClientProfileResponse` |

> ⚠️ **心理師個案隔離（BR-01）**：當 `X-Active-Role: therapist` 時：
> - `GET /` 與 `/search` 只會回傳「自己被指派」的個案（後端自動過濾，非報錯）。
> - `GET /{id}`、`GET /by-therapist/{其他人}` 若存取非自己的個案 → **403**。
> - owner/admin 則可見全機構。前端可據此決定是否顯示「查看全部/其他心理師」入口。

### 7.4 同意書 Consent（`/api/consent-templates`、`/api/consent-records`）

```ts
interface CreateConsentTemplateRequest { title: string; content: string; triggerEvent?: ConsentTriggerEvent; }
interface UpdateConsentTemplateRequest { title?: string | null; content?: string | null; triggerEvent?: ConsentTriggerEvent | null; isActive?: boolean | null; }
interface ConsentTemplateResponse {
  id: number; title: string; content: string; version: number;
  triggerEvent: ConsentTriggerEvent; isActive: boolean; createdAt: string; updatedAt: string;
}
// 更新 content 時後端會自動 version++；舊簽署紀錄保留當時快照不受影響。

interface SignConsentRequest {
  consentTemplateId: number; clientProfileId: number;
  signatureImage?: string | null; // Base64
  signedRemotely: boolean;
}
interface VoidConsentRequest { reason: string; }
interface ConsentRecordResponse {
  id: number; consentTemplateId: number; templateTitle: string;
  clientProfileId: number; clientName: string;
  contentSnapshot: string; templateVersion: number;
  signatureImage?: string | null; signedRemotely: boolean;
  status: ConsentRecordStatus; signedAt?: string | null;
  voidedAt?: string | null; voidReason?: string | null; createdAt: string;
}
```

| Method | 路由 | Policy | Body | Response |
|--------|------|--------|------|----------|
| GET | `/api/consent-templates` | consent.view | — | `ConsentTemplateResponse[]` |
| GET | `/api/consent-templates/{id}` | consent.view | — | `ConsentTemplateResponse` |
| POST | `/api/consent-templates` | consent.create | `CreateConsentTemplateRequest` | `ConsentTemplateResponse` |
| PUT | `/api/consent-templates/{id}` | consent.update | `UpdateConsentTemplateRequest` | `ConsentTemplateResponse` |
| DELETE | `/api/consent-templates/{id}` | consent.update | — | `{}` |
| POST | `/api/consent-records/sign` | consent.create | `SignConsentRequest` | `ConsentRecordResponse` |
| POST | `/api/consent-records/{id}/void` | consent.void | `VoidConsentRequest` | `ConsentRecordResponse` |
| GET | `/api/consent-records` | consent.view | `?clientProfileId=&status=&triggerEvent=`（皆選填） | `ConsentRecordResponse[]` |
| GET | `/api/consent-records/by-client/{clientProfileId}` | consent.view | — | `ConsentRecordResponse[]` |
| GET | `/api/consent-records/by-trigger-event/{triggerEvent}` | consent.view | — | `ConsentRecordResponse[]` |

### 7.5 診間 Room（`/api/rooms`，Policy: room.manage）

```ts
interface CreateRoomRequest { name: string; }
interface UpdateRoomRequest { name?: string | null; isActive?: boolean | null; }
interface AssignTherapistToRoomRequest { therapistUserId: number; }
interface TherapistRoomInfo { therapistUserId: number; therapistName: string; assignedAt: string; }
interface RoomResponse { id: number; name: string; isActive: boolean; therapists: TherapistRoomInfo[]; createdAt: string; updatedAt: string; }
```

| Method | 路由 | Body | Response |
|--------|------|------|----------|
| GET | `/api/rooms` | — | `RoomResponse[]` |
| GET | `/api/rooms/{id}` | — | `RoomResponse` |
| GET | `/api/rooms/active-count` | — | `{ activeCount: number }` |
| POST | `/api/rooms` | `CreateRoomRequest` | `RoomResponse` |
| PUT | `/api/rooms/{id}` | `UpdateRoomRequest` | `RoomResponse` |
| DELETE | `/api/rooms/{id}` | — | `{}` |
| POST | `/api/rooms/{id}/assign-therapist` | `AssignTherapistToRoomRequest` | `RoomResponse` |
| DELETE | `/api/rooms/{id}/therapist/{therapistUserId}` | — | `RoomResponse` |

### 7.6 排班 Schedule（`/api/schedule-templates`、`/api/schedule-slots`）

```ts
interface CreateScheduleTemplateRequest {
  therapistUserId: number; dayOfWeek: number; // 0–6（0=週日）
  startTime: string; endTime: string;         // "HH:mm"
  slotDuration?: number;                       // 分鐘，10–120，預設 50
}
interface UpdateScheduleTemplateRequest { startTime?: string | null; endTime?: string | null; slotDuration?: number | null; isActive?: boolean | null; }
interface ScheduleTemplateResponse {
  id: number; therapistUserId: number; therapistName: string; dayOfWeek: number;
  startTime: string; endTime: string; slotDuration: number; isActive: boolean; createdAt: string; updatedAt: string;
}

interface ExpandScheduleRequest { startDate: string; endDate: string; } // "yyyy-MM-dd"
interface ManualExpandRequest { year: number; month: number; }
interface BlockSlotRequest { note?: string | null; }
interface ExpandResult { createdCount: number; skippedCount: number; warnings: string[]; }
interface ScheduleSlotResponse {
  id: number; therapistUserId: number; therapistName: string;
  roomId?: number | null; roomName?: string | null;
  date: string; startTime: string; endTime: string; status: SlotStatus; note?: string | null;
}
```

| Method | 路由 | Policy | Body / Query | Response |
|--------|------|--------|--------------|----------|
| GET | `/api/schedule-templates` | schedule.view | — | `ScheduleTemplateResponse[]` |
| GET | `/api/schedule-templates/{id}` | schedule.view | — | `ScheduleTemplateResponse` |
| POST | `/api/schedule-templates` | schedule.create | `CreateScheduleTemplateRequest` | `ScheduleTemplateResponse` |
| PUT | `/api/schedule-templates/{id}` | schedule.update | `UpdateScheduleTemplateRequest` | `ScheduleTemplateResponse` |
| DELETE | `/api/schedule-templates/{id}` | schedule.delete | — | `{}` |
| GET | `/api/schedule-slots` | schedule.view | `?startDate=&endDate=&therapistUserId=` | `ScheduleSlotResponse[]` |
| POST | `/api/schedule-slots/expand` | schedule.create | `ExpandScheduleRequest` | `ExpandResult` |
| POST | `/api/schedule-slots/manual-expand` | schedule.create | `ManualExpandRequest` | `ExpandResult` |
| POST | `/api/schedule-slots/{id}/block` | schedule.update | `BlockSlotRequest` | `ScheduleSlotResponse` |
| POST | `/api/schedule-slots/{id}/unblock` | schedule.update | — | `ScheduleSlotResponse` |

> - 展班（expand）不覆蓋既有時段（BR-04），回傳 `createdCount`/`skippedCount`；`warnings` 含「同時段診間不足」等提醒（不阻擋）。
> - 過去日期不可 block/unblock（BR-05），會回 400。
> - **封鎖已預約時段**：若該時段已有預約，`block` 會取消該預約並通知個案（therapist_leave）。前端封鎖前宜提示使用者。

### 7.7 預約 Appointment（`/api/appointments`、`/api/appointment-series`）

```ts
interface CreateAppointmentRequest {
  clientProfileId: number; scheduleSlotId: number;
  sessionType?: SessionType; visitType?: VisitType; note?: string | null;
}
interface RescheduleRequest { newScheduleSlotId: number; }
interface AppointmentNoteRequest { note?: string | null; } // cancel 用，可整個省略
interface AppointmentResponse {
  id: number; clientProfileId: number; clientName: string;
  therapistUserId: number; therapistName: string;
  roomId?: number | null; roomName?: string | null;
  scheduleSlotId: number; date: string; startTime: string; endTime: string;
  seriesId?: number | null; sessionType: SessionType; visitType: VisitType;
  status: AppointmentStatus; note?: string | null;
  hints: string[];   // 溫和提示（BR-06），如個案預約非慣用心理師；空陣列代表無提示
  createdAt: string;
}

interface CreateAppointmentSeriesRequest {
  clientProfileId: number; therapistUserId: number;
  dayOfWeek: number; startTime: string;      // "HH:mm"
  frequency?: SeriesFrequency; months?: number; // 1–3（BR-07 上限 3 個月）
}
interface RenewSeriesRequest { months?: number; } // 1–3
interface AppointmentSeriesResponse {
  id: number; clientProfileId: number; clientName: string;
  therapistUserId: number; therapistName: string;
  dayOfWeek: number; startTime: string; frequency: SeriesFrequency; status: SeriesStatus;
  expiresAt: string; totalAppointments: number; remainingAppointments: number; createdAt: string;
}
```

| Method | 路由 | Policy | Body / Query | Response |
|--------|------|--------|--------------|----------|
| POST | `/api/appointments` | appointment.create | `CreateAppointmentRequest` | `AppointmentResponse` |
| POST | `/api/appointments/{id}/complete` | appointment.update | — | `AppointmentResponse` |
| POST | `/api/appointments/{id}/cancel` | appointment.update | `AppointmentNoteRequest?` | `AppointmentResponse` |
| POST | `/api/appointments/{id}/no-show` | appointment.update | — | `AppointmentResponse` |
| POST | `/api/appointments/{id}/reschedule` | appointment.update | `RescheduleRequest` | `AppointmentResponse` |
| GET | `/api/appointments/calendar` | appointment.view | `?startDate=&endDate=&therapistUserId=`（前二必填） | `AppointmentResponse[]` |
| GET | `/api/appointments/list` | appointment.view | `?clientProfileId=&therapistUserId=&status=`（皆選填） | `AppointmentResponse[]` |
| GET | `/api/appointments/available-slots` | appointment.view | `?startDate=&endDate=&therapistUserId=`（前二必填） | `ScheduleSlotResponse[]` |
| GET | `/api/appointments/{id}` | appointment.view | — | `AppointmentResponse` |
| POST | `/api/appointment-series` | appointment.create | `CreateAppointmentSeriesRequest` | `AppointmentSeriesResponse` |
| POST | `/api/appointment-series/{id}/renew` | appointment.create | `RenewSeriesRequest` | `AppointmentSeriesResponse` |
| POST | `/api/appointment-series/{id}/end` | appointment.update | — | `AppointmentSeriesResponse` |
| GET | `/api/appointment-series/pending-renewal` | appointment.view | — | `AppointmentSeriesResponse[]` |
| GET | `/api/appointment-series/by-client/{clientProfileId}` | appointment.view | `?status=` | `AppointmentSeriesResponse[]` |

> - `available-slots` 回傳的是 **`ScheduleSlotResponse[]`**（不是 Appointment），供個案挑時段用。
> - `hints` 是溫和提示、**不阻擋**建立，前端顯示為提示即可。
> - `cancel` 的 body 可整個省略。

### 7.8 動態表單 Form（`/api/form-templates`、`/api/form-submissions`）

巢狀結構：Template → Section[] → Field[]；Submission → Value[]。

```ts
interface CreateFormFieldRequest {
  label: string; fieldType?: FieldType; // 預設 text
  options?: string | null;              // JSON 字串（select/radio/checkbox 選項）
  isRequired: boolean; sortOrder: number;
  placeholder?: string | null; validationRules?: string | null; // JSON 字串
}
interface CreateFormSectionRequest { title: string; sortOrder: number; isRequired?: boolean; fields?: CreateFormFieldRequest[]; }
interface CreateFormTemplateRequest { name: string; type?: FormTemplateType; sections?: CreateFormSectionRequest[]; }
interface UpdateFormTemplateRequest { name?: string | null; isActive?: boolean | null; sections?: CreateFormSectionRequest[] | null; } // 傳 sections 會整組覆蓋並 version++

interface FormFieldResponse { id: number; label: string; fieldType: FieldType; options?: string | null; isRequired: boolean; sortOrder: number; placeholder?: string | null; validationRules?: string | null; }
interface FormSectionResponse { id: number; title: string; sortOrder: number; isRequired: boolean; fields: FormFieldResponse[]; }
interface FormTemplateResponse {
  id: number; name: string; type: FormTemplateType; isSystemDefault: boolean;
  version: number; isActive: boolean; sections: FormSectionResponse[]; createdAt: string; updatedAt: string;
}

interface FieldValueRequest { fieldId: number; value?: string | null; } // value 為 JSON 字串
interface SubmitFormRequest { templateId: number; clientProfileId: number; values: FieldValueRequest[]; }
interface FieldValueResponse { fieldId: number; fieldLabel: string; fieldType: FieldType; value?: string | null; }
interface FormSubmissionResponse {
  id: number; templateId: number; templateName: string; templateVersion: number;
  clientProfileId: number; clientName: string; therapistUserId: number; therapistName: string;
  submittedAt: string; values: FieldValueResponse[];
}
```

| Method | 路由 | Body | Response |
|--------|------|------|----------|
| GET | `/api/form-templates` | — | `FormTemplateResponse[]` |
| GET | `/api/form-templates/{id}` | — | `FormTemplateResponse` |
| POST | `/api/form-templates` | `CreateFormTemplateRequest` | `FormTemplateResponse` |
| PUT | `/api/form-templates/{id}` | `UpdateFormTemplateRequest` | `FormTemplateResponse` |
| DELETE | `/api/form-templates/{id}` | — | `{}` |
| POST | `/api/form-templates/{id}/clone` | — | `FormTemplateResponse` |
| POST | `/api/form-submissions` | `SubmitFormRequest` | `FormSubmissionResponse` |
| GET | `/api/form-submissions/{id}` | — | `FormSubmissionResponse` |
| GET | `/api/form-submissions/by-client/{clientProfileId}` | — | `FormSubmissionResponse[]` |

> - **權限（2026-08-15 起）**：`GET` 兩支需 `client.view`、`POST` 需 `client.update`（原本無權限閘，現已補上）。若某角色（如個案本人）需自行填/看表單，請確認該角色具備對應權限。
> - 系統預設表單（`isSystemDefault=true`）**不可修改/刪除** → 需先 `clone` 再改，否則回 400。
> - 已有填答紀錄的表單**不可再改結構**（傳 `sections`）→ 回 400，請 clone 為新版。前端編輯 UI 應對這兩種情況給明確引導。

### 7.9 晤談紀錄 SessionNote（`/api/session-notes`）

```ts
interface CreateSessionNoteRequest {
  clientProfileId: number; appointmentId?: number | null;
  type?: SessionNoteType; format?: SessionNoteFormat;
  soapS?: string | null; soapO?: string | null; soapA?: string | null; soapP?: string | null;
  freeTextContent?: string | null;
}
interface UpdateSessionNoteRequest { // 僅內容，不含 type/format
  soapS?: string | null; soapO?: string | null; soapA?: string | null; soapP?: string | null;
  freeTextContent?: string | null;
}
interface SessionNoteResponse {
  id: number; clientProfileId: number; clientName: string;
  therapistUserId: number; therapistName: string; appointmentId?: number | null;
  type: SessionNoteType; format: SessionNoteFormat;
  soapS?: string | null; soapO?: string | null; soapA?: string | null; soapP?: string | null;
  freeTextContent?: string | null; intakeSummaryImported: boolean;
  isLocked: boolean; lockedAt?: string | null; lockedBy?: number | null;
  createdAt: string; updatedAt: string;
}
```

| Method | 路由 | Policy | Body | Response |
|--------|------|--------|------|----------|
| POST | `/api/session-notes` | session.create | `CreateSessionNoteRequest` | `SessionNoteResponse` |
| PUT | `/api/session-notes/{id}` | session.update | `UpdateSessionNoteRequest` | `SessionNoteResponse` |
| DELETE | `/api/session-notes/{id}` | session.update | — | `{}` |
| POST | `/api/session-notes/{id}/lock` | session.lock | — | `SessionNoteResponse` |
| POST | `/api/session-notes/{id}/unlock` | session.lock | — | `SessionNoteResponse` |
| GET | `/api/session-notes/by-client/{clientProfileId}` | session.view | — | `SessionNoteResponse[]` |
| GET | `/api/session-notes/by-appointment/{appointmentId}` | session.view | — | `SessionNoteResponse \| null` |
| GET | `/api/session-notes/{id}` | session.view | — | `SessionNoteResponse` |

> - **僅紀錄建立者（該 therapist）可編輯/刪除/鎖定/解鎖**，他人操作回 403（UnauthorizedAccessException → 401/403 視情況，實務為權限拒絕）。
> - 已鎖定（`isLocked=true`）不可編輯/刪除；解鎖可逆但全程留稽核（BR-08）。
> - `by-appointment` 可能回 `null`（該預約尚無紀錄）。

### 7.10 常用語 Phrase / 個案備忘 Memo

```ts
interface CreatePhraseTemplateRequest { category?: PhraseCategory; title: string; content: string; sortOrder: number; }
interface UpdatePhraseTemplateRequest { category?: PhraseCategory | null; title?: string | null; content?: string | null; sortOrder?: number | null; }
interface PhraseTemplateResponse { id: number; category: PhraseCategory; title: string; content: string; sortOrder: number; createdAt: string; }

interface CreateClientMemoRequest { clientProfileId: number; category?: MemoCategory; content: string; }
interface UpdateClientMemoRequest { category?: MemoCategory | null; content?: string | null; }
interface ClientMemoResponse {
  id: number; clientProfileId: number; therapistUserId: number; therapistName: string;
  category: MemoCategory; content: string;
  isEditable: boolean; // true=當前心理師可編輯；false=他人所寫（BR-16 唯讀）
  createdAt: string; updatedAt: string;
}
```

| Method | 路由 | Policy | Body | Response |
|--------|------|--------|------|----------|
| GET | `/api/phrase-templates` | (登入) | — | `PhraseTemplateResponse[]`（僅自己的） |
| POST | `/api/phrase-templates` | (登入) | `CreatePhraseTemplateRequest` | `PhraseTemplateResponse` |
| PUT | `/api/phrase-templates/{id}` | (登入) | `UpdatePhraseTemplateRequest` | `PhraseTemplateResponse` |
| DELETE | `/api/phrase-templates/{id}` | (登入) | — | `{}` |
| GET | `/api/client-memos/by-client/{clientProfileId}` | client.view | — | `ClientMemoResponse[]` |
| POST | `/api/client-memos` | client.update | `CreateClientMemoRequest` | `ClientMemoResponse` |
| PUT | `/api/client-memos/{id}` | client.update | `UpdateClientMemoRequest` | `ClientMemoResponse` |
| DELETE | `/api/client-memos/{id}` | client.update | — | `{}` |

> 備忘跟著個案走：接手心理師看得到前手的備忘，但 `isEditable=false`（唯讀）。前端依此欄位決定是否允許編輯。

### 7.11 收費 Billing（費率 / 套餐 / 折扣 / 收款 / 報表）

Policy：檢視類 `billing.view`、建立 `billing.create`、更新 `billing.update`、退費 `billing.refund`。

```ts
interface CreateFeeScheduleRequest {
  name: string; sessionType?: SessionType; visitType?: VisitType;
  therapistUserId?: number | null; // null=機構通用；查找時心理師專屬優先（BR-09）
  amount: number;
}
interface UpdateFeeScheduleRequest { name?: string | null; amount?: number | null; isActive?: boolean | null; }
interface FeeScheduleResponse { id: number; name: string; sessionType: SessionType; visitType: VisitType; therapistUserId?: number | null; therapistName?: string | null; amount: number; currency: string; isActive: boolean; }

interface CreatePackageRequest { name: string; sessionType?: SessionType; totalSessions: number; totalAmount: number; validDays?: number; }
interface UpdatePackageRequest { name?: string | null; totalAmount?: number | null; validDays?: number | null; isActive?: boolean | null; }
interface PackageResponse { id: number; name: string; sessionType: SessionType; totalSessions: number; totalAmount: number; perSessionAmount: number; validDays: number; isActive: boolean; }

// 折扣：已改為可綁定心理師（therapist 層級）
interface CreateDiscountRequest {
  name: string; discountType?: DiscountType;
  percentageValue?: number | null; fixedAmount?: number | null;
  applicableRole?: string | null;    // 綁角色自動套用（BR-11）
  therapistUserId?: number | null;   // null=機構通用折扣；指定=心理師專屬（專屬優先）
}
interface UpdateDiscountRequest { name?: string | null; percentageValue?: number | null; fixedAmount?: number | null; isActive?: boolean | null; } // 注意：Update 不含 therapistUserId，綁定只在建立時設
interface AssignDiscountRequest { clientProfileId: number; discountId: number; note?: string | null; }
interface DiscountResponse { id: number; name: string; discountType: DiscountType; percentageValue?: number | null; fixedAmount?: number | null; applicableRole?: string | null; therapistUserId?: number | null; isActive: boolean; }

interface PurchasePackageRequest { clientProfileId: number; packageId: number; paymentMethod?: PaymentMethod; }
interface ConfirmPaymentRequest { paymentMethod?: PaymentMethod; }
interface RefundRequest { note?: string | null; }
interface PaymentRecordResponse {
  id: number; clientProfileId: number; clientName: string;
  appointmentId?: number | null; clientPackageId?: number | null;
  discountId?: number | null; discountName?: string | null;
  originalAmount: number; finalAmount: number;
  paymentMethod: PaymentMethod; paymentType: PaymentType; status: PaymentStatus;
  note?: string | null; paidAt?: string | null; createdAt: string;
}
interface ClientPackageResponse {
  id: number; clientProfileId: number; clientName: string;
  packageId: number; packageName: string;
  sessionsUsed: number; sessionsRemaining: number; status: ClientPackageStatus;
  purchasedAt: string; expiresAt: string;
}
```

| Method | 路由 | Policy | Body | Response |
|--------|------|--------|------|----------|
| GET | `/api/fee-schedules` | billing.view | — | `FeeScheduleResponse[]` |
| POST | `/api/fee-schedules` | billing.create | `CreateFeeScheduleRequest` | `FeeScheduleResponse` |
| PUT | `/api/fee-schedules/{id}` | billing.update | `UpdateFeeScheduleRequest` | `FeeScheduleResponse` |
| DELETE | `/api/fee-schedules/{id}` | billing.update | — | `{}` |
| GET | `/api/packages` | billing.view | — | `PackageResponse[]` |
| POST | `/api/packages` | billing.create | `CreatePackageRequest` | `PackageResponse` |
| PUT | `/api/packages/{id}` | billing.update | `UpdatePackageRequest` | `PackageResponse` |
| DELETE | `/api/packages/{id}` | billing.update | — | `{}` |
| GET | `/api/discounts` | billing.view | — | `DiscountResponse[]` |
| POST | `/api/discounts` | billing.create | `CreateDiscountRequest` | `DiscountResponse` |
| PUT | `/api/discounts/{id}` | billing.update | `UpdateDiscountRequest` | `DiscountResponse` |
| DELETE | `/api/discounts/{id}` | billing.update | — | `{}` |
| POST | `/api/discounts/assign` | billing.update | `AssignDiscountRequest` | `{}` |
| DELETE | `/api/discounts/{discountId}/client/{clientProfileId}` | billing.update | — | `{}` |
| POST | `/api/billing/on-completed/{appointmentId}` | billing.create | — | `PaymentRecordResponse`（無費率時回 `{}` + 訊息） |
| POST | `/api/billing/purchase-package` | billing.create | `PurchasePackageRequest` | `ClientPackageResponse` |
| POST | `/api/billing/{id}/confirm` | billing.update | `ConfirmPaymentRequest` | `PaymentRecordResponse` |
| POST | `/api/billing/{id}/refund` | billing.refund | `RefundRequest` | `PaymentRecordResponse` |
| POST | `/api/billing/{id}/waive` | billing.update | — | `PaymentRecordResponse` |
| GET | `/api/billing/pending` | billing.view | — | `PaymentRecordResponse[]` |
| GET | `/api/billing` | billing.view | `?startDate=&endDate=&clientProfileId=&therapistUserId=&status=&page=1&pageSize=50` | `PaymentRecordResponse[]` |
| GET | `/api/billing/packages` | billing.view | `?clientProfileId=&status=` | `ClientPackageResponse[]` |
| GET | `/api/billing/export` | billing.view | `?startDate=&endDate=&clientProfileId=&therapistUserId=&status=` | **檔案下載** CSV（UTF-8 BOM） |

**報表 Reports（`/api/reports`，Policy: billing.view）**

```ts
interface MonthlyRevenue { year: number; month: number; totalRevenue: number; totalTransactions: number; paidAmount: number; pendingAmount: number; refundedAmount: number; waivedAmount: number; }
interface TherapistRevenue { therapistUserId: number; therapistName: string; totalRevenue: number; totalSessions: number; }
interface PackageRemaining { clientPackageId: number; clientName: string; packageName: string; sessionsRemaining: number; expiresAt: string; }
interface PackageStats { activePackages: number; expiredPackages: number; completedPackages: number; totalPackageRevenue: number; lowRemaining: PackageRemaining[]; }
```

| Method | 路由 | Query | Response |
|--------|------|-------|----------|
| GET | `/api/reports/monthly-revenue` | `?year=&month=` | `MonthlyRevenue` |
| GET | `/api/reports/by-therapist` | `?year=&month=` | `TherapistRevenue[]` |
| GET | `/api/reports/pending` | — | `PaymentRecordResponse[]` |
| GET | `/api/reports/package-stats` | — | `PackageStats` |

### 7.12 通知 Notification

```ts
interface CreateNotificationTemplateRequest { eventType: NotificationEventType; channel?: NotificationChannel; subject: string; bodyTemplate: string; isRequired: boolean; }
interface UpdateNotificationTemplateRequest { subject?: string | null; bodyTemplate?: string | null; isActive?: boolean | null; }
interface NotificationTemplateResponse { id: number; eventType: NotificationEventType; channel: NotificationChannel; subject: string; bodyTemplate: string; isRequired: boolean; isActive: boolean; }
interface NotificationLogResponse { id: number; channel: NotificationChannel; subject: string; body: string; status: NotificationStatus; sentAt?: string | null; readAt?: string | null; createdAt: string; }
interface UpdatePreferenceRequest { eventType: NotificationEventType; emailEnabled: boolean; smsEnabled: boolean; inAppEnabled: boolean; }
interface PreferenceResponse { eventType: NotificationEventType; emailEnabled: boolean; smsEnabled: boolean; inAppEnabled: boolean; }
```

| Method | 路由 | Policy | Body | Response |
|--------|------|--------|------|----------|
| GET | `/api/notification-templates` | notification.manage | — | `NotificationTemplateResponse[]` |
| POST | `/api/notification-templates` | notification.manage | `CreateNotificationTemplateRequest` | `NotificationTemplateResponse` |
| PUT | `/api/notification-templates/{id}` | notification.manage | `UpdateNotificationTemplateRequest` | `NotificationTemplateResponse` |
| DELETE | `/api/notification-templates/{id}` | notification.manage | — | `{}` |
| GET | `/api/notifications` | (登入) | — | `NotificationLogResponse[]`（本人） |
| GET | `/api/notifications/unread-count` | (登入) | — | `{ unreadCount: number }` |
| POST | `/api/notifications/{id}/mark-read` | (登入) | — | `{}` |
| POST | `/api/notifications/mark-all-read` | (登入) | — | `{}` |
| GET | `/api/notification-preferences` | (登入) | — | `PreferenceResponse[]` |
| PUT | `/api/notification-preferences` | (登入) | `UpdatePreferenceRequest` | `{}` |

> ⚠️ **必要通知不可關閉（BR-12）**：若某 `eventType` 有 `isRequired` 範本，嘗試把任一管道設為 `false` 更新偏好 → 回 **400**，message：`此為必要通知，無法關閉`。前端對必要通知應把開關 disable，避免使用者誤觸。

### 7.13 考核 PerformanceReview（`/api/performance-reviews`）

```ts
interface CreatePerformanceReviewRequest {
  therapistUserId: number; period: string;
  professionalScore: number; clientFeedbackScore: number; teamworkScore: number; overallScore: number; // 1–5
  comment?: string | null;
}
interface UpdatePerformanceReviewRequest { professionalScore?: number | null; clientFeedbackScore?: number | null; teamworkScore?: number | null; overallScore?: number | null; comment?: string | null; }
interface SetVisibilityRequest { visibility: ReviewVisibility; visibleFrom?: string | null; visibleUntil?: string | null; }
interface PerformanceReviewResponse {
  id: number; therapistUserId: number; therapistName: string;
  reviewerId: number; reviewerName: string; period: string;
  professionalScore: number; clientFeedbackScore: number; teamworkScore: number; overallScore: number;
  comment?: string | null; // BR-18：非 Owner 檢視時後端會回 null
  visibility: ReviewVisibility; visibleFrom?: string | null; visibleUntil?: string | null; createdAt: string;
}
// 量化指標（8 項 metric + 2 識別欄位）
interface TherapistMetrics {
  therapistUserId: number; therapistName: string;
  appointmentCompletionRate: number; // 預約完成率 %
  noShowRate: number;                 // 未到率 %
  avgNoteDelayHours: number;          // 紀錄及時性（晤談後平均時差，小時）
  noteLockRate: number;               // 紀錄鎖定率 %
  clientRetentionRate: number;        // 個案留存率 %
  monthlySessionCount: number;        // 月諮商次數
  monthlySessionHours: number;        // 月諮商時數
  paymentCompletionRate: number;      // 收費完成率 %
}
// 單一指標的時間趨勢
interface MetricsTrend {
  metric: string;                     // 指標鍵（同 TherapistMetrics 的欄位名，如 'appointmentCompletionRate'）
  points: { period: string; value: number }[]; // period 為 "yyyy-MM"
}
```

有效的 `metric` 鍵（對應 `TherapistMetrics` 的量化欄位）：`appointmentCompletionRate`、`noShowRate`、`avgNoteDelayHours`、`noteLockRate`、`clientRetentionRate`、`monthlySessionCount`、`monthlySessionHours`、`paymentCompletionRate`。傳未知鍵回 400。

| Method | 路由 | Policy | Body / Query | Response |
|--------|------|--------|--------------|----------|
| GET | `/api/performance-reviews` | performance.manage | — | `PerformanceReviewResponse[]` |
| GET | `/api/performance-reviews/by-therapist/{therapistUserId}` | performance.view | — | `PerformanceReviewResponse[]`（僅已開放者） |
| POST | `/api/performance-reviews` | performance.manage | `CreatePerformanceReviewRequest` | `PerformanceReviewResponse` |
| PUT | `/api/performance-reviews/{id}` | performance.manage | `UpdatePerformanceReviewRequest` | `PerformanceReviewResponse` |
| DELETE | `/api/performance-reviews/{id}` | performance.manage | — | `{}` |
| PUT | `/api/performance-reviews/{id}/visibility` | performance.manage | `SetVisibilityRequest` | `PerformanceReviewResponse` |
| GET | `/api/performance-reviews/metrics/{therapistUserId}` | performance.view | `?startDate=&endDate=`（必填） | `TherapistMetrics` |
| GET | `/api/performance-reviews/metrics/{therapistUserId}/trend` | performance.view | `?metric=&startDate=&endDate=&interval=month`（metric/startDate/endDate 必填；interval 目前僅 month） | `MetricsTrend` |
| GET | `/api/performance-reviews/metrics-comparison` | performance.manage | `?startDate=&endDate=` | `TherapistMetrics[]` |

> 質性 `comment` 僅 Owner 可見（BR-18），心理師檢視時後端會把 `comment` 回成 `null`。量化 `metrics` 心理師可見自己。考核預設 `hidden`（BR-17），由 Owner 手動或排程開放。

### 7.14 違規 Violation（`/api/violation-records`，Policy: performance.manage）

```ts
interface CreateViolationRecordRequest { therapistUserId: number; incidentDate: string; severity?: ViolationSeverity; description: string; actionTaken?: string | null; }
interface UpdateViolationRecordRequest { severity?: ViolationSeverity | null; description?: string | null; actionTaken?: string | null; }
interface ResolveViolationRequest { actionTaken: string; }
interface ViolationRecordResponse { id: number; therapistUserId: number; therapistName: string; reportedBy: number; incidentDate: string; severity: ViolationSeverity; description: string; actionTaken?: string | null; status: ViolationStatus; resolvedAt?: string | null; createdAt: string; }
```

| Method | 路由 | Body | Response |
|--------|------|------|----------|
| GET | `/api/violation-records` | — | `ViolationRecordResponse[]` |
| POST | `/api/violation-records` | `CreateViolationRecordRequest` | `ViolationRecordResponse` |
| PUT | `/api/violation-records/{id}` | `UpdateViolationRecordRequest` | `ViolationRecordResponse` |
| POST | `/api/violation-records/{id}/resolve` | `ResolveViolationRequest` | `ViolationRecordResponse` |
| POST | `/api/violation-records/{id}/dismiss` | — | `ViolationRecordResponse` |
| DELETE | `/api/violation-records/{id}` | — | `{}` |

### 7.15 稽核 AuditLog（`/api/audit-logs`，Policy: audit.view）

```ts
interface AuditLogResponse { id: number; userId: number; userName?: string | null; action: string; resourceType: string; resourceId: number; description?: string | null; changes?: string | null; ipAddress?: string | null; createdAt: string; }
```

| Method | 路由 | Query | Response |
|--------|------|-------|----------|
| GET | `/api/audit-logs` | `?action=&resourceType=&resourceId=&userId=&startDate=&endDate=&page=1&pageSize=50` | `AuditLogResponse[]` |
| GET | `/api/audit-logs/export` | `?action=&resourceType=&startDate=&endDate=&format=csv\|pdf` | **檔案下載**（非 ApiResponse） |

> - 稽核不可刪除（BR-13），只有查詢與匯出。
> - `export` 回傳**檔案**：`format=csv`（預設，UTF-8 BOM，Excel 中文正常）或 `format=pdf`。前端用 blob 下載，勿當 JSON 解析。
> - export 的 query **沒有** `userId` / `page` / `pageSize` / `resourceId`。
> - `resourceId`（number，選填）與 `resourceType` 搭配可精準查「某資源的稽核」（如某個案）。

**⚠️ `resourceType` 的實際值為 snake_case（不是 PascalCase）**。前端篩選/比對務必用下列值：

| resourceType | 說明 |
|---|---|
| `client_profile` | 個案 |
| `session_note` | 晤談紀錄 |
| `consent_record` / `consent_template` | 同意書簽署 / 範本 |
| `appointment` / `appointment_series` | 預約 / 常態預約 |
| `schedule_slot` / `schedule_template` | 時段 / 班表範本 |
| `payment_record` / `client_package` / `discount` / `client_discount` / `fee_schedule` / `package` | 收費相關 |
| `form_template` / `form_submission` | 表單範本 / 填答 |
| `client_memo` | 個案備忘 |
| `user` / `role` / `organization` | 帳號 / 角色 / 機構 |
| `performance_review` / `violation_record` | 考核 / 違規 |
| `room` | 診間 |

（`action` 常見值：`create`/`update`/`delete`/`view`/`search`/`sign`/`void`/`lock`/`unlock`/`block`/`unblock`/`assign`/`transfer_therapist`/`complete`/`cancel`/`no_show`/`reschedule`/`confirm`/`refund`/`waive`/`package_deduct`/`purchase`/`resolve`/`dismiss`/`set_visibility`/`login`/`register`/`change_password`/`reset_password`/`toggle_active`/`update_permissions` 等。）

---

## 8. 前端常見雷區（Checklist）

1. **統一拆封套**：除稽核匯出外一律 `ApiResponse<T>`，`success=false` 要拋錯，別直接用 `data`。
2. **enum 一字不差**：`follow_up`、`no_show`、`in_app`、`fixed_amount`、`single_session`、`package_purchase`、`credit_card`、`line_pay`、`free_text`、`intake_assessment`、`pending_confirmation`、`annual_renewal`、`service_change`、`first_visit` 都是 snake_case，傳錯即 400。
3. **`roles` claim 要 `JSON.parse`**：它是 JSON 陣列「字串」。
4. **每個請求帶 `X-Active-Role`**：漏帶會導致權限判斷失敗（多數受保護端點回 403）。且只能帶使用者實際擁有的角色。
5. **首次改密碼**：`mustChangePassword=true` 時除 change-password/me/logout 外全部 403；改完要**重新登入**才拿到新 token。
6. **權限用 `permissions` 判斷**，別用角色名硬寫（Owner 可改角色權限）。
7. **心理師個案隔離**：therapist 身份下列表會自動只剩自己的個案；存取他人個案回 403——前端別預期看到全部。
8. **available-slots 回的是 `ScheduleSlotResponse`**，不是 Appointment。
9. **必要通知開關**要 disable（關閉會 400）。
10. **考核 `comment`**：心理師檢視時後端回 `null`，UI 要能容忍。
11. **稽核匯出用 blob 下載**，不要 JSON.parse。
12. **金額是 number、日期時間多為 ISO 字串**；但 `date`/`startTime`/`endTime`/`dateOfBirth`/`incidentDate` 是純字串（`yyyy-MM-dd` 或 `HH:mm`）。
13. **加密對前端透明**：14 個個資欄位、SOAP、同意書快照等後端自動加解密，前端拿到的是明文、送出也是明文，無需處理。
14. **個案端用 `clientProfileId`（來自 /me），不是 `userId`**：兩者是不同的值，用錯會抓到別人的資料。
15. **稽核 `resourceType` 是 snake_case**（`client_profile` 等），不是 PascalCase；篩選/比對要用實際值。

---

_本文件由後端程式碼萃取產生。若後端 DTO/enum/路由有變動，請同步更新此文件；前端 Claude Code 開發時以此為準。_

