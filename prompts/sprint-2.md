# Sprint 2 — 後端：個案與同意書

## Entity
- **ClientProfile**：14 欄位全加密, user_id nullable, status enum (pending/active/suspended/closed)
- **TherapistClient**：status enum (active/transferred/closed), ended_at nullable, ended_reason nullable
- **ConsentTemplate**：content 加密, version int, trigger_event enum, is_active
- **ConsentRecord**：content_snapshot+signature_image 加密, template_version, signed_remotely bool, status enum (pending/signed/voided)

## Service
- **ClientService**：CRUD + CreateWithAccount（預設密碼：身分證後4碼+生日MMDD，首次登入強制改）+ CreateWithoutAccount + AssignTherapist + TransferTherapist + GetByTherapist + 搜尋（姓名解密比對+身分證末四碼）+ 狀態變更
- **ConsentService**：Template CRUD（更新content時version++）+ SignConsent（快照當下content+version）+ VoidConsent + GetByTriggerEvent + GetByClient

## Controller
- ClientProfilesController：CRUD + assign/transfer/search + status
- ConsentTemplatesController：CRUD
- ConsentRecordsController：sign/void/list

## 交付標準
- [ ] 個案建檔（含/不含帳號）+ 14欄位加密正確
- [ ] 預設密碼登入 + 首次強制改密碼
- [ ] 心理師指派/移轉 + TherapistClient 狀態正確
- [ ] Therapist 只查到自己的個案
- [ ] 同意書範本版本控制 + 簽署快照 + 簽名圖加密儲存
