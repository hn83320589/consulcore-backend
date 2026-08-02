# 前端請求的後端待補項目（給後端 Claude Code）

> 對象：`consulcore-backend`（.NET）開發者 / Claude Code
> 來源：前端 `consulcore-frontend` 已於 2026-08-02 全面對齊契約 `docs/frontend-integration.md`。
> 過程中發現以下端點/欄位「前端需要、但後端目前沒有」。前端已先以「隱藏或降級」處理並標記 `TODO(backend-backlog)`；後端補上後前端即可恢復完整功能。
>
> **完成每一項後，請同步更新 `docs/frontend-integration.md`（唯一事實來源），並在本檔該項標記 ✅。**
> 慣例照契約：回應一律包 `ApiResponse<T>`；`organizationId`/`userId` 由 JWT 取得不放 body；需 `Authorization` + `X-Active-Role`；JSON key camelCase；enum 用契約既有字串值。

---

## ✅ 後端處理狀態（2026-08-02，已完成）

**P1（1–6）+ P2（7–9）全部實作完成，A/B 兩問已回覆並修正。** `docs/frontend-integration.md` 已同步更新（4.4、7.4、7.7、7.9、7.11、7.13、7.15 及雷區 checklist）。`dotnet build` 0 warning。前端可移除對應 `TODO(backend-backlog)` 並恢復功能。

- **B（優先）已修**：`userId ≠ clientProfileId`。後端在 `/me` 加了 `clientProfileId`，詳見下方 B。
- **A 已回覆**：`resourceType` 是 snake_case（非 PascalCase），詳見下方 A。
- 命名決定：item 6 採 `GET /api/appointment-series/by-client/{clientProfileId}?status=`（非 query 版）。

---

## P1 — 使用者功能因缺這些端點而降級（優先）

### 1. ✅ `GET /api/billing` — 付款紀錄完整清單（含篩選）
- **目的**：BillingManageView「付款紀錄」分頁需要完整付款紀錄，目前只有 `GET /api/billing/pending`，前端已降級為「僅待收款」。
- **建議端點**：`GET /api/billing?startDate=&endDate=&clientProfileId=&therapistUserId=&status=&page=&pageSize=`
- **Query**：皆選填；`status` 為 `PaymentStatus`（paid/pending/refunded/waived）。
- **回應**：`PaymentRecordResponse[]`（已存在的 DTO，見契約 7.11）。
- **Policy**：`billing.view`。
- **前端現況**：BillingManageView Tab1 用 `/billing/pending` 頂著；補上後改回完整清單 + 篩選。

### 2. ✅ `GET /api/billing/packages` — 個案套餐總覽（含篩選）
- **目的**：BillingManageView「套餐總覽」需要列出所有個案套餐購買紀錄（剩餘次數、狀態）。目前無此端點，前端改用 `GET /api/reports/package-stats` 的 `lowRemaining`（只有低剩餘那些）頂著。
- **建議端點**：`GET /api/billing/packages?clientProfileId=&status=`
- **Query**：選填；`status` 為 `ClientPackageStatus`（active/expired/completed）。
- **回應**：`ClientPackageResponse[]`（已存在，見契約 7.11）。
- **Policy**：`billing.view`。
- **前端現況**：Tab2 顯示 package-stats 統計卡 + lowRemaining；補上後改為完整套餐清單。
- **附帶**：ClientDetailView 收費分頁也需要「某個案的套餐清單」，此端點加上 `clientProfileId` 篩選即可同時滿足（目前該處顯示「功能開發中」佔位）。

### 3. ✅ `GET /api/billing/export` — 付款紀錄匯出（CSV）
- **目的**：付款紀錄匯出。目前無，前端已移除匯出按鈕。
- **建議端點**：`GET /api/billing/export?startDate=&endDate=&clientProfileId=&therapistUserId=&status=&format=csv`
- **回應**：**檔案下載**（非 ApiResponse），UTF-8 BOM（Excel 中文正常），比照 `GET /api/audit-logs/export` 的做法。
- **Policy**：`billing.view`。
- **前端現況**：按鈕已隱藏；補上後恢復。

### 4. ✅ `GET /api/performance-reviews/metrics/{therapistUserId}/trend` — 績效趨勢
- **目的**：PerformanceView 的 MetricsTrend 趨勢圖需要「同一指標隨時間變化」的序列。目前只有單期 `GET /api/performance-reviews/metrics/{therapistUserId}`（回單一 `TherapistMetrics`），前端已隱藏趨勢圖、改顯示單期卡片。
- **建議端點**：`GET /api/performance-reviews/metrics/{therapistUserId}/trend?metric=&startDate=&endDate=&interval=month`
- **Query**：`metric`＝某個量化指標鍵（如 `appointmentCompletionRate`）；`interval` 預設 month。
- **回應建議**：`{ metric: string; points: { period: string; value: number }[] }`（新 DTO；請於契約 7.13 定義）。
- **Policy**：`performance.view`。
- **前端現況**：MetricsTrend.vue 保留但未接線並標 backlog；補上後接回。

### 5. ✅ `GET /api/audit-logs` 增加 `resourceId` 篩選
- **目的**：ClientDetailView 稽核分頁要顯示「某個案」的稽核紀錄。目前 `GET /api/audit-logs` 的 query 無 `resourceId`，前端只能抓一批再前端過濾（首頁 50 筆外會漏）。
- **建議**：`GET /api/audit-logs` 的 query 增加 `resourceId`（number，選填），與 `resourceType` 搭配過濾。
- **回應**：`AuditLogResponse[]`（不變）。
- **Policy**：`audit.view`。
- **前端現況**：`clientAuditLogs` 用 `resourceType='ClientProfile'` + 前端過濾 `resourceId`；後端支援後改為直接帶 `resourceId` query。

### 6. ✅ `GET /api/appointment-series?clientProfileId=` — 依個案查常態預約
- **目的**：ClientDetailView 要顯示「某個案的常態性預約」。目前只有 `GET /api/appointment-series/pending-renewal`，前端只能顯示「待續約」那些，非全部 active series。
- **建議端點**：`GET /api/appointment-series?clientProfileId=&status=`（或 `GET /api/appointment-series/by-client/{clientProfileId}`，命名擇一並寫進契約）。
- **回應**：`AppointmentSeriesResponse[]`（已存在，見契約 7.7）。
- **Policy**：`appointment.view`。
- **前端現況**：ClientDetail 常態預約區用 pending-renewal 過濾頂著；補上後改為完整清單。

---

## P2 — 便利型單筆查詢（非阻斷，前端已用 store/清單頂替）

這些不急，補上可讓前端邏輯更乾淨、支援深連結（直接以 URL 進入詳情頁時能載入資料）：

- **7. ✅ `GET /api/appointments/{id}`** → `AppointmentResponse`（Policy `appointment.view`）。已實作。
- **8. ✅ `GET /api/session-notes/{id}`** → `SessionNoteResponse`（Policy `session.view`）。已實作（含檢視稽核）。
- **9. ✅ `GET /api/consent-records`**（純清單，含篩選 `?clientProfileId=&status=&triggerEvent=`）→ `ConsentRecordResponse[]`（Policy `consent.view`）。已實作，簽署紀錄總表可改回完整清單。

---

## 需要後端「確認/回覆」的事項（非開發）

### A. `AuditLog.resourceType` 的實際字串值 — ✅ 已回覆

**前端假設錯誤：實際是 snake_case，不是 PascalCase。** 用 `ClientProfile` 篩選會**完全撈不到**，
必須改用 `client_profile`。完整合法值已寫入契約 7.15（含 `action` 常見值），節錄：

`client_profile`、`session_note`、`consent_record`、`consent_template`、`appointment`、
`appointment_series`、`schedule_slot`、`schedule_template`、`payment_record`、`client_package`、
`discount`、`client_discount`、`fee_schedule`、`package`、`form_template`、`form_submission`、
`client_memo`、`user`、`role`、`organization`、`performance_review`、`violation_record`、`room`。

> 搭配新增的 `resourceId` query（item 5），前端可用 `?resourceType=client_profile&resourceId={id}` 精準查某個案的稽核，不必抓一批再前端過濾。

### B. 個案端查詢的 `clientProfileId` 來源 — ✅ 已修正（重要）

**前端假設錯誤：`userId` ≠ `clientProfileId`。** `User.Id`（帳號 PK）與 `ClientProfile.Id`（個案檔 PK）
是不同的值，拿 `userId` 當 `clientProfileId` 會**抓到別的個案**（正確性 / 隱私問題）。

**後端已修**：`GET /api/auth/me`（`CurrentUser`）新增 `clientProfileId?: number | null`。
client 帳號登入後，用 `/me` 回傳的 `clientProfileId` 去查自己的預約/同意書/套餐/常態預約
（如 `GET /api/consent-records?clientProfileId=...`、`GET /api/billing/packages?clientProfileId=...`、
`GET /api/appointment-series/by-client/{clientProfileId}`）。無對應個案檔時為 `null`。契約 4.4 已更新並加警示。

---

## 完成後

1. 更新 `docs/frontend-integration.md`（新增端點的路由/query/DTO/Policy）。
2. 在本檔對應項目標 ✅ 並註明契約已更新。
3. 通知前端：前端會移除對應的 `TODO(backend-backlog)` 標記並恢復功能（BillingManageView Tab1/Tab2/匯出、MetricsTrend、ClientDetail 稽核/套餐/常態預約分頁）。
