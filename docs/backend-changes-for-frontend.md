# 後端異動通知（給前端 Claude Code）

> 對象：`consulcore-frontend`（Vue 3 + TypeScript）開發者 / Claude Code
> 來源：`consulcore-backend` 於 2026-08-15 完成一批**安全性修復與存取政策調整**。
> 本檔是 `frontend-requested-endpoints.md` 的反向清單（後端 → 前端）。
>
> **契約唯一事實來源仍是 `docs/frontend-integration.md`**，本檔只列「本次異動與前端有關的部分」。
> 處理完每一項請標 ✅ 並註明處理方式；有疑問請在對應項目下回覆，後端會據以調整。

---

## 摘要：這批異動會不會弄壞前端？

| 異動 | 對前端影響 |
|------|-----------|
| IDOR 防護改 fail-closed | **無**（合法流程行為不變） |
| `GET /client-profiles/{id}` 代診放寬 | **只放寬**（403 → 200，拿到更多資料，不會壞） |
| ClientMemo / FormSubmission 補檢視稽核 | **無**（純後端，契約不變）；僅稽核清單會多出 `view` 紀錄 |
| **`/api/form-submissions` 補權限閘** | ⚠️ **唯一可能弄壞的地方，需前端確認** → 見 P0-1 |

> 結論：除了 P0-1 需要你確認，其餘都不需要前端修改；P1 是「放寬後可順手優化 UI」的機會，非必要。

---

## P0 — 需前端確認（可能阻斷功能）

### 1. ✅ `/api/form-submissions` 三支端點補上權限閘（原本完全無權限檢查）

> **前端回覆（2026-08-15）：沒有病患自助填表單流程 → 無需修改，此項結案。**
>
> 查證方式（非憑印象）：全專案 grep `formService` / `formBuilderStore` / `FormRenderer` / `FormBuilder`，
> 消費端**只有** `views/admin/OrgSettingView.vue`（機構後台的表單範本管理）。
> 個案端六個視圖（Dashboard / Appointment / NewAppointment / Consent / Notification / Settings）
> 完全未引用表單相關 service / store / 元件。
>
> 表單填答目前一律由 staff（owner / admin / therapist，皆有 `client.view` / `client.update`）操作，
> 補上的 Policy 不影響任何既有前端流程。**若日後要做病患自助初診評估，前端會先回報後端**，
> 不會自行繞過（理解：給 client 角色 `client.view` 會造成看得到別人資料的隱私漏洞，
> 正解是後端加「client 只能存取自己 clientProfileId 的填答」）。

**後端做了什麼**

這三支端點原本只有 class 級 `[Authorize]`（只擋未登入），**沒有任何 Policy**，
等於任何登入帳號都能讀取任意個案的表單填答——而初診評估表含
「自殺意念 / 自我傷害 / 精神科用藥」等高敏感欄位。這是既有安全漏洞，已補上：

| 端點 | 新增 Policy | 所需權限 |
|------|------------|---------|
| `POST /api/form-submissions` | `CanUpdateClients` | `client.update` |
| `GET /api/form-submissions/{id}` | `CanViewClients` | `client.view` |
| `GET /api/form-submissions/by-client/{clientProfileId}` | `CanViewClients` | `client.view` |

**各角色預設權限現況**（後端 `AuthService.GetDefaultRolePermissions()`）

| 角色 | 有 `client.view` / `client.update`？ | 呼叫上述端點結果 |
|------|-----------------------------------|-----------------|
| owner / admin / therapist | ✅ 有 | 200，**不受影響** |
| **client（個案本人）** | ❌ **沒有**（只有 `appointment.view`、`consent.view`） | **403** |

**➡️ 需要前端回答的問題**

> **前端是否有「病患本人自行填寫 / 檢視表單」的流程？**（例如病患入口的自助初診評估表）

- **答「沒有」（表單一律由心理師 / 櫃檯填）** → **無需任何修改**，此項可直接標 ✅ 結案。
  後端判斷應屬此情況（已知病患端視圖為 Dashboard / Appointment / NewAppointment / Consent 四個，不含表單），但需你確認。

- **答「有」（病患會自己填或看）** → **請回報後端，不要自行繞過**。
  正確解法不是給 client 角色 `client.view`（那會讓病患看得到**別的個案**資料，反而製造隱私漏洞），
  而是比照本次 IDOR 修復模式：後端加「client 角色只能存取自己 `clientProfileId` 的填答」判斷。
  後端收到回覆後會補這段，前端不需改。

---

## P1 — 放寬帶來的 UI 調整機會（非必要，不做也不會壞）

### 2. ✅ `GET /api/client-profiles/{id}`：心理師查「非自己指派」個案，403 → 200（BR-20 代診/交接）

> **前端已採用「建議」做法（加代診提示），並額外收斂了寫入類 UI。**
>
> 1. **代診提示已實作**：`ClientDetailView` 新增 `isProxyView` computed，判斷依你建議的方式——
>    `therapists` 中不存在 `therapistUserId === 當前登入 userId && status === 'active'`。
>    成立時於頁首顯示：「⚠ 此個案非您負責，您正以代診身分唯讀檢視，本次檢視已記錄稽核。」
> 2. **寫入 UI 一併收斂**（依你「代診能看單筆，不能改任何東西」的邊界）：
>    - 指派心理師 / 暫停 / 恢復 / 結案 → 改為 `client.manage` 權限才顯示（therapist 無此權限）。
>    - 基本資料卡新增 `readonly` prop：無 `client.update` **或處於代診狀態**時隱藏編輯入口。
>
> **實測**（真後端 + Playwright，以 therapist 身分開啟未指派給自己的個案 id=2）：
> BR-20 放寬生效（拿到 200，原本 403）、代診橫幅正確顯示、寫入按鈕正確消失、Tab 權限正確
> （therapist 見晤談/備忘、不見稽核）。

**後端做了什麼**

新增 **BR-20**：代診 / 交接情境下，therapist 可**唯讀**非自己被指派個案的個別紀錄。
原本 `GET /client-profiles/{id}` 對非指派個案硬性回 403，現改為放行唯讀，
且後端會在稽核描述標示 `（代診/非指派存取）` 以利事後追溯。

| 項目 | 變更前 | 變更後 |
|------|-------|-------|
| therapist 查自己指派的個案 | 200 | 200（不變） |
| therapist 查**非自己指派**的個案 | **403** | **200**（唯讀，留稽核） |
| owner / admin | 200 | 200（不變） |

**前端可做的調整（擇一即可）**

1. 若前端目前對此端點的 403 有特殊處理（如導回列表、顯示「無權存取此個案」），
   該分支在代診情境**不會再觸發**，可保留（無害）或簡化。
2. **建議**：因為這種存取會被記錄稽核，若能判斷「此個案非當前心理師被指派」，
   可在畫面上加一則輕量提示。判斷方式（契約 §7.3）：
   `ClientProfileResponse.therapists`（`TherapistInfo[]`）中，
   是否存在 `therapistUserId === 當前登入 userId && status === 'active'`；
   不存在即代表是代診檢視。例如：
   > ⚠️ 此個案非您負責，您正以代診身分唯讀檢視，本次檢視已記錄稽核。

   這能避免心理師誤以為自己有編輯權，也符合「留痕」的設計意圖。

**重要邊界（前端請勿誤解為全面開放）**

| 端點 | 是否放寬 |
|------|---------|
| `GET /client-profiles/{id}`（**單筆**） | ✅ 放寬為唯讀 |
| `GET /client-profiles`（清單） | ❌ 不變，therapist 仍只看到自己被指派的 |
| `GET /client-profiles/search` | ❌ 不變，仍只搜自己被指派的 |
| `GET /client-profiles/by-therapist/{id}` | ❌ 不變，查他人仍 **403** |
| 所有**寫入**端點（PUT / DELETE / assign / transfer） | ❌ 不變，代診**不能改** |
| 晤談紀錄 / 備忘的**寫入** | ❌ 不變，仍限**紀錄建立者本人** |

> 一句話：**代診能看單筆，不能瀏覽清單，也不能改任何東西。**

---

## P2 — 既有行為確認（不需修改，但請確保前端沒踩到）

### 3. ✅ 個案（client）角色的存取範圍已強制限縮

為修復「個案可用任意 id 讀到**其他病患**資料」的 IDOR 漏洞，後端對 client 角色做了強制範圍化。
**若前端病患端視圖已照 `frontend-integration.md` 使用 `/me` 的 `clientProfileId`，這裡不需要任何修改。**

| 端點 | client 角色行為 |
|------|----------------|
| `GET /appointments/list` | 後端**強制覆寫**為自己的 `clientProfileId`，並忽略傳入的 `clientProfileId` / `therapistUserId` |
| `GET /appointments/{id}` | 非自己的預約 → **403** |
| `GET /consent-records` | 後端**強制覆寫**為自己的 `clientProfileId` |
| `GET /consent-records/by-client/{id}` | 非自己 → **403** |
| `GET /appointment-series/by-client/{id}` | 非自己 → **403** |
| `GET /appointments/calendar` | **一律 403**（staff 聚合視圖，病患請改用 `list`） |
| `GET /appointment-series/pending-renewal` | **一律 403**（staff 視圖） |
| `GET /consent-records/by-trigger-event/{e}` | **一律 403**（staff 跨個案視圖） |

**前端自查點**：病患端視圖（Dashboard / Appointment / NewAppointment / Consent）
**不應**呼叫 `calendar`、`pending-renewal`、`by-trigger-event` 這三支，否則會拿到 403。
若目前有呼叫，請改用對應的 client 可用端點（如 `calendar` → `list`）。

> **前端自查結果（2026-08-15）：已確認乾淨，無需修改。**
>
> 個案端六個視圖呼叫的 store 方法全清單：`fetchAppointments`(→`/appointments/list`)、
> `fetchAvailableSlots`、`createAppointment`、`cancel`、`fetchRecordsByClient`(自己的 id)、
> 通知偏好、`changePassword`。
>
> 三支禁用端點的呼叫者：
> - `fetchCalendar` → 僅 `AppointmentManageView`（staff）
> - `fetchPendingRenewals` → 僅 `OwnerDashboard` / `TherapistDashboard`（staff）
> - `fetchRecordsByTriggerEvent` → 目前無任何呼叫端
>
> 另確認個案端一律使用 `/me` 的 `clientProfileId`（非 `userId`）——此為先前
> `frontend-requested-endpoints.md` B 項回覆後已修正的部分。

### 4. ✅ `X-Active-Role` 必帶，且必須是 JWT `roles` 內確實擁有的角色

後端的角色判斷已全面改為 **fail-closed**：`X-Active-Role` 會與 JWT 的 `roles` claim 交叉驗證，
**偽造或不擁有的角色一律視為無效**（不會提權，只會讓存取更受限）。

- 每個 API 請求都必須帶 `Authorization: Bearer <token>` + `X-Active-Role: <role>`。
- `X-Active-Role` 的值必須存在於登入回應 / `/me` 回傳的 `roles` 陣列中。
- 多重身份使用者（例如同時是 therapist 又是 client）切換身份時，**務必同步更新此 header**，
  否則會以「非 staff」身分被限縮。

> 這是既有規範（見契約 §4），此處僅重申，因為後端現在會嚴格執行。

> **前端回覆：這項重申讓我們抓到並修好了一個真正的 bug — 感謝提醒。**
>
> **問題**：前端的 axios 請求攔截器原本是從 `localStorage` 讀 `token` / `activeRole`。
> 但 `pinia-plugin-persistedstate` 是以 `$subscribe(cb, { detached: true })` 寫入，
> 未指定 `flush`，即 Pinia 預設的 `flush: 'pre'`（微任務才執行），**不是**賦值當下同步寫入。
> 而攔截器是在建立請求時同步執行的，於是：
>
> ```js
> activeRole.value = role   // 尚未寫進 localStorage
> await fetchMe()           // 攔截器此刻讀 localStorage → 舊值，或多角色首次選身份時「根本還沒有值」
> ```
>
> 後果：多角色帳號在 `/select-role` 首次選身份時，`X-Active-Role` **會整個漏送**；
> 用 RoleSwitcher 切換時會送出**上一個角色**。在後端改為 fail-closed 後，這會直接導致
> 權限錯誤或 403（在後端寬鬆時期則是「碰巧沒事」，所以一直沒被發現）。
>
> **修法**：httpService 改為由 `authStore` 註冊 provider（沿用既有 `registerToast` 模式，避免循環相依），
> 攔截器直接讀 store 的 ref 當下值；保留 localStorage 作為 store 尚未初始化時的後備。
> 這讓「寫入」與「讀取」落在同一個 tick，race 完全消除。
>
> **實測**（真後端 + Playwright，建立 `multi_test` 帳號，roles = `['admin','therapist']`）：
> - 登入 → 正確導向 `/select-role` → 選「心理師」→ 進 TherapistDashboard，
>   側欄顯示 **8 項**（therapist 權限子集），證明 `/me` 是以 `X-Active-Role: therapist` 取回。
> - RoleSwitcher 切到「行政人員」→ 側欄變 **13 項**，且**「機構設定」正確不顯示**
>   （admin 無 `org.settings`），證明切換後 header 同步更新、權限正確重取。
> - 全程 0 console error。
>
> 這條路徑（多角色選身份 / 切換）先前從未被測過（既有測試帳號都是單一角色），
> 因此這個 bug 一直潛伏。

---

## 附帶：稽核紀錄會變多（AuditLog 相關 UI）

為配合 BR-20「跨心理師檢視必須留痕」，後端補了幾處**檢視稽核**。
若前端有稽核紀錄檢視畫面（AuditLogView / ClientDetail 稽核分頁），會看到新增的 `view` 紀錄：

| `resourceType` | `resourceId` 帶的是什麼 | 觸發時機 |
|---------------|----------------------|---------|
| `client_profile` | 個案 id | 檢視個案單筆（代診時描述含「（代診/非指派存取）」） |
| `client_memo` | **個案 id**（非備忘 id） | 檢視某個案的備忘清單 |
| `form_submission` | 單筆查詢＝**填答 id**；依個案查詢＝**個案 id** | 檢視表單填答 |

> ⚠️ 注意 `client_memo` 與 `form_submission`（by-client）的 `resourceId` 帶的是**個案 id**，
> 不是該筆紀錄自己的 id。前端若用 `?resourceType=...&resourceId=...` 精準查詢，請依上表對應。
>
> 這兩者的 `resourceType` 已存在於契約 §7.15 的合法值清單，無需新增。

---

## 完成後

1. 在本檔對應項目標 ✅ 並註明處理方式（或「確認無需修改」）。
2. **P0-1 請務必回覆**（有 / 沒有病患自助填表單流程）——這是唯一可能弄壞功能的項目。
3. 若發現本檔與 `docs/frontend-integration.md` 有出入，以**後端程式碼**為準並回報後端修正文件。

---

## ✅ 前端處理總結（2026-08-15，四項全數完成）

| 項目 | 結果 |
|------|------|
| P0-1 form-submissions 權限閘 | **沒有病患自助填表單流程** → 無需修改，結案 |
| P1-2 代診放寬 | 已加代診提示橫幅，並額外收斂寫入類 UI |
| P2-3 client 存取限縮 | 自查乾淨，個案端未呼叫三支禁用端點 |
| P2-4 X-Active-Role | **抓到並修好 race bug**（詳見該項），已實測多角色切換 |

**額外修正（本次連帶發現）**：`ClientDetailView` 原本無條件呼叫 `GET /users`（需 `staff.manage`）
以填充指派心理師下拉，therapist 開啟頁面時必然 403。已改為僅在具 `staff.manage` 時呼叫。
這是前端先前自己引入的缺陷，由本次 therapist 角色實測暴露。

**驗證**：`type-check` / `lint`(0 problems) / `build` 全綠；真後端 + Playwright 實測
（多角色登入→選身份→切換身份、代診檢視、個案/收費/機構設定頁）全程 0 console error。

### 稽核 `resourceId` 語意（附帶事項）已知悉
`client_memo` 與 `form_submission`(by-client) 的 `resourceId` 帶的是**個案 id**、非該筆紀錄 id —— 已記錄。
前端目前 `ClientDetailView` 稽核分頁只查 `resourceType=client_profile&resourceId={個案id}`，
語意一致、不受影響；`AuditLogView` 為通用查詢（不預設 resourceId），亦不受影響。
若日後要在個案頁一併呈現備忘/表單的檢視軌跡，會依上表用個案 id 查詢。

### 回覆後端待辦最後一項（寫入授權）
> 「交接後的新心理師無法編輯前手留下的紀錄（只能另寫新紀錄）——若實務上需要『目前指派心理師可編輯』請提出。」

前端**目前沒有**這個需求，維持現狀即可。理由：晤談紀錄在臨床上屬「作者責任」文件，
由建立者本人負責、他人另寫新紀錄，符合 BR-08 的鎖定/留痕設計，也較不易產生爭議。
若之後實務端提出需求，前端會另行回報，不會自行放寬 UI。

---

## 後端待辦（供前端知悉，無需處理）

- 整合測試 `tests/integration-test.sh` 已更新（代診唯讀、client IDOR 正反向、偽造 header 案例），
  但尚未在有 DB 的環境完整跑過，後端會補驗。
- 寫入授權目前維持「紀錄建立者本人」——即**交接後的新心理師無法編輯前手留下的紀錄**（只能另寫新紀錄）。
  若實務上需要「目前指派心理師可編輯」，屬另一個決策，前端若有相關需求請提出。

---

# 2026-10-11 批次（重構 Phase 2：資安修補）

處理完請在項目標 ✅ 並註明處理方式。

| 異動 | 對前端影響 |
|------|-----------|
| `/api/form-templates` 補權限閘 | **無**（只有 OrgSettingView 使用，選單已以 `org.settings` 控制） |
| 擁有者保護：非 owner 不可指派 owner 角色、不可修改/重設密碼/停用 owner 帳號、不可調整系統角色權限 | ⚠️ 會出現**新的 403**，需調整 UI → 見 5 |
| 非 Development 環境拒絕以 placeholder JWT 金鑰啟動 | **無**（部署設定） |
| 個案初始密碼改為隨機，帳密只在 `POST /api/client-profiles` 回應的 `initialCredentials` 回傳一次；帳號尾碼改為隨機 | ✅ **前端已先完成**（`763b67a`：InitialCredentialsModal） |
| `POST /api/billing/on-completed/{id}`：預約須為 completed（否則 400）；同一預約重複呼叫回傳既有收費單，不再重複扣次 | **目前無**：`billingService.onCompleted()` 有定義但沒有任何 store / 頁面呼叫；日後接上時，請只在預約完成後呼叫，並可安全重試 |
| 同一時段被同時預約：後寫者回 **409**（`ApiResponse`，訊息可直接顯示）；其他併發寫入衝突同樣回 409 | ✅ 前端已處理（`34ad953`：httpService 對 409 顯示訊息） |
| 「非建立者」編輯/刪除/鎖定晤談紀錄、備忘、常用語：**401 → 403**；改密碼時目前密碼錯誤：**401 → 400** | **無**：前端 F3（`f099d77`）已先相容，兩者都只顯示訊息、不登出 |
| 模型驗證失敗（必填、長度等）改回 `ApiResponse`（`message` 為第一個錯誤、`errors` 為全部），原本是 ASP.NET 的 ValidationProblemDetails | **改善**：原本攔截器讀不到 `message` 只顯示「請求無法完成」，現在會顯示具體原因 |
| 框架 / EF 內部的例外不再以 400 回傳原始訊息，改為 500 +「伺服器發生錯誤」 | **無** |
| `PUT /api/client-profiles/{id}`：therapist 只能修改自己指派中的個案，否則 **403**；建立 / 指派 / 移轉時指定的心理師須為本機構啟用中的 therapist，否則 **400** | **無**：ClientDetailView 對代診（非指派）心理師本來就唯讀（`canEditClient`） |
| therapist 預設權限移除 `billing.view`（收費、費率、套餐、折扣、報表、匯出皆 403）；收費與稽核匯出寫入稽核 `export` | ✅ **前端已先完成**（`17bbd08`：個案詳情依 `billing.view` 才顯示 / 抓收費）；收費管理、費率設定頁本來就以路由權限擋下 |
| 帳號或機構停用後，既有 token 立即失效：任何請求回**空內容的 401** | **無**：F3 規則（帶 token + 空內容的 401 → 登入過期）正好處理 |
| `POST /api/auth/login` 限流：同一 IP 每分鐘 10 次，超過回 **429**（`ApiResponse`） | **無**（`parseLoginError` 已處理 429） |
| 背景排程（套餐到期 `expire`、考核依排程開放 / 收回 `visibility_open` / `visibility_close`）寫入稽核；這類紀錄 `userId` 為 **null**、`userName` 為「系統」 | **無**：畫面只顯示 `userName`；`AuditLog.userId` 型別應放寬為 `number \| null` |
| 刪除使用者 / 個案改為軟刪除：從清單與查詢消失（GET 單筆回 404），但預約、紀錄、收費等歷史仍顯示其姓名；附帳號的個案刪除後帳號無法登入；已刪除的帳號名稱不可再使用 | **無** |
| `POST /api/appointments`：個案不存在、已刪除或屬於其他機構時回 **404**（原本不檢查） | **無**（前端只會帶本機構個案） |
| `GET /api/audit-logs`、`GET /api/billing`：`page` 須 ≥ 1、`pageSize` 須介於 1–100，超出範圍回 **400** | **無**（前端固定 pageSize 50） |
| `GET /api/audit-logs`、`GET /api/billing`、`GET /api/notifications` 的 `data` 由陣列改為 `{ items, totalCount, page, pageSize }`；`/api/notifications` 新增 `page` / `pageSize`（原本固定回最新 50 筆） | ✅ **前端已先完成**（PR #9：兩種格式都能處理，顯示總頁數） |

### 5. ✅ 擁有者保護：新的 403 情境

> **前端已處理（`2a0e88c`）**：非 owner 時角色勾選不含 owner、擁有者那一列不顯示編輯 / 停用 / 重設密碼；權限矩陣原本就對系統角色唯讀。

| 端點 | 何時 403 |
|------|---------|
| `POST /api/users`、`PUT /api/users/{id}` | 非 owner 身份送出的 `roles` 含 `"owner"` |
| `PUT /api/users/{id}`、`POST /api/users/{id}/reset-password`、`PATCH /api/users/{id}/toggle-active` | 非 owner 身份操作擁有者帳號 |
| `PUT /api/roles/{id}/permissions` | 非 owner 身份修改系統角色（`isSystemDefault=true`，目前所有角色皆是） |

「owner 身份」= `X-Active-Role: owner` 且 JWT `roles` 內確實有 owner。403 回應為 `ApiResponse`，`message` 可直接顯示。

**建議前端調整**（StaffManageView）：
- 非 owner 時，角色下拉不提供 `owner` 選項
- 非 owner 時，擁有者那一列不顯示編輯 / 重設密碼 / 停用按鈕
- 非 owner 時，權限矩陣改為唯讀
