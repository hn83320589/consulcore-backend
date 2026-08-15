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

### 1. ⬜ `/api/form-submissions` 三支端點補上權限閘（原本完全無權限檢查）

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

### 2. ⬜ `GET /api/client-profiles/{id}`：心理師查「非自己指派」個案，403 → 200（BR-20 代診/交接）

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

### 3. ⬜ 個案（client）角色的存取範圍已強制限縮

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

### 4. ⬜ `X-Active-Role` 必帶，且必須是 JWT `roles` 內確實擁有的角色

後端的角色判斷已全面改為 **fail-closed**：`X-Active-Role` 會與 JWT 的 `roles` claim 交叉驗證，
**偽造或不擁有的角色一律視為無效**（不會提權，只會讓存取更受限）。

- 每個 API 請求都必須帶 `Authorization: Bearer <token>` + `X-Active-Role: <role>`。
- `X-Active-Role` 的值必須存在於登入回應 / `/me` 回傳的 `roles` 陣列中。
- 多重身份使用者（例如同時是 therapist 又是 client）切換身份時，**務必同步更新此 header**，
  否則會以「非 staff」身分被限縮。

> 這是既有規範（見契約 §4），此處僅重申，因為後端現在會嚴格執行。

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

## 後端待辦（供前端知悉，無需處理）

- 整合測試 `tests/integration-test.sh` 已更新（代診唯讀、client IDOR 正反向、偽造 header 案例），
  但尚未在有 DB 的環境完整跑過，後端會補驗。
- 寫入授權目前維持「紀錄建立者本人」——即**交接後的新心理師無法編輯前手留下的紀錄**（只能另寫新紀錄）。
  若實務上需要「目前指派心理師可編輯」，屬另一個決策，前端若有相關需求請提出。
