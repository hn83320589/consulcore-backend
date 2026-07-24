# Sprint 8 — 後端：整合測試 + 收尾

## 目標
全流程驗證、權限測試、加密驗證、edge case 處理。

---

## 全流程測試

### 情境 1：新機構從零開始
POST /api/auth/register → 建立機構+Owner → 建診間 → 建心理師+行政帳號 → 指派診間 → 設權限 → 建初診表範本 → 建同意書範本 → 設費率 → 建套餐 → 設通知範本

### 情境 2：完整個案生命週期
建個案(代建帳號) → 預設密碼登入+強制改密碼 → 代預約初診 → 簽同意書 → 填初診評估 → 寫晤談紀錄(SOAP)+鎖定 → 自動產生PaymentRecord → 確認收款 → 標記常態性預約 → 產生後續預約 → 逐次完成+寫紀錄+自動收費 → 購買套餐 → 套餐扣次 → 剩餘2次觸發通知 → 記備忘 → 結案

### 情境 3：心理師互相諮商
心理師A有Therapist+Client角色 → A以Client身份被B諮商 → B只看到A是普通個案 → API 層確認身份隔離正確

### 情境 4：心理師離職
個案有接手者→transferred+接手者可看紀錄 / 無接手者→closed+Owner可看 → 未來預約取消→通知 → 帳號停用 → 常態性預約結束

---

## 權限測試
用 4 種角色各呼叫所有 API，驗證：
- Owner：全部可存取
- Therapist：僅自己的個案/紀錄/排班，無法存取人員管理/診間/費率設定
- Admin：預約+收費+排班檢視，無法存取諮商紀錄/備忘
- Client：僅自己的預約/通知，無法存取任何管理功能
- 所有 ❌ 回傳 403，不是 404

---

## 加密驗證
直接查 DB 確認以下欄位為密文非明文：
1. ClientProfile: name, gender, birth_date, id_number, address 等 14 欄位
2. SessionNote: soap_subjective/objective/assessment/plan, free_text_content
3. ConsentRecord: content_snapshot, signature_image
4. AuditLog: changes
5. PerformanceReview: comment
6. ViolationRecord: description, action_taken
7. ClientMemo: content
8. FormFieldValue: value

確認 ENCRYPTION_KEY 不在 appsettings.json（只在環境變數）。

---

## Edge Case
| Case | 預期行為 |
|------|---------|
| 展班時心理師數 > 診間數 | 回傳警告，不阻擋 |
| 已完成預約的時段被 block | 不影響已完成，僅影響未來 |
| 套餐到期仍有剩餘次數 | expired，剩餘作廢 |
| 常態性預約時段被 block | 該次需手動取消或改期 |
| 個案同時段預約兩心理師 | 驗證攔截回傳錯誤 |
| 同心理師同時段兩預約 | 驗證攔截回傳錯誤 |
| 解鎖→修改→再鎖定 | 完整稽核軌跡 |
| 同意書範本更新 | 舊簽署保留快照不受影響 |
| 刪除心理師 | 不允許刪除僅停用 |
| 預設密碼登入不改密碼 | API 層攔截所有非改密碼操作 |

---

## 收尾
- [ ] dotnet build 無 warning
- [ ] 所有 API 回傳格式統一（ApiResponse）
- [ ] Swagger 文件完整（所有端點可見）
- [ ] 環境變數文件（.env.example）完整
- [ ] README.md 含啟動說明
