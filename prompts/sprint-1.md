# Sprint 1 — 後端：帳號與組織

## 目標
完整帳號系統 API：Organization、User、UserRole、Role、Permission、RolePermission、AuditLog Entity + AuthService + UserService + RolePermissionService。

## Entity
依 CLAUDE.md 資料模型建立 6 + 1 個 Entity（含 AuditLog）。詳細欄位見規格書 4.2 節。
DB連線資訊（實際值以環境變數 / appsettings.Development.json 注入，勿寫入版控）
host=YOUR_DB_HOST;
port=YOUR_DB_PORT;
database=consulcore;
username=YOUR_DB_USER;
password=YOUR_DB_PASSWORD;



重點：
- User 沒有 role_id，角色走 UserRole 多對多
- AuditLog 的 changes 欄位需加密
- 所有 Entity 加 Global Query Filter（除 Permission 外，Permission 是全域共用）

## Service
- **AuthService**：Login（驗密碼→產JWT含roles陣列）、Register（建Organization+Owner+預設4 Role+預設Permission）、GetCurrentUser、ChangePassword
- **UserService**：CRUD (Owner)、CreateUser（BCrypt hash+指派角色）、ResetPassword、ToggleActive
- **RolePermissionService**：GetRolesForUser、GetPermissionsForRole、UpdateRolePermissions

## Controller
- AuthController：POST /login, POST /register, GET /me, PUT /change-password
- UsersController：CRUD [Authorize Owner]
- RolesController：GET roles, PUT permissions

## DatabaseSeeder
預設 Permission 清單（client.view/create/update/delete/manage, appointment.*, schedule.*, session.*, consent.*, billing.*, notification.manage, staff.manage, room.manage, audit.view, performance.view/manage, org.settings）+ 4 個預設 Role 的權限映射。

## Policy 註冊
Program.cs 為每個 Permission.code 註冊 Authorization Policy。

## 交付標準
- [ ] POST /api/auth/register → 建立機構+Owner+預設角色+權限
- [ ] POST /api/auth/login → 回傳 JWT（含 roles 陣列）
- [ ] GET /api/auth/me → 回傳當前用戶（依 X-Active-Role）
- [ ] Users CRUD + ResetPassword + ToggleActive
- [ ] RolePermission 更新正常
- [ ] AuditLog Entity 建立 + IAuditService 正式接上
