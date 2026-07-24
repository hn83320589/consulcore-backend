# Sprint 5 — 後端：動態表單 + 個案紀錄

## Entity
- **FormTemplate**：organization_id, name, type(intake_assessment), is_system_default, version, is_active, created_by
- **FormSection**：template_id, title, sort_order, is_required
- **FormField**：section_id, label, field_type(text/textarea/select/radio/checkbox/date/number/scale), options(JSON), is_required, sort_order, placeholder, validation_rules(JSON)
- **FormSubmission**：organization_id, template_id, template_version, client_profile_id, therapist_id, submitted_at, is_encrypted(always true)
- **FormFieldValue**：submission_id, field_id, value(加密, JSON string)
- **SessionNote**：organization_id, client_profile_id, therapist_id, appointment_id(nullable), type(session/closure), format(soap/free_text), soap_S/O/A/P(各自加密), free_text_content(加密), intake_summary_imported(bool), is_locked(bool), locked_at, locked_by
- **PhraseTemplate**：therapist_id, category(subjective/objective/assessment/plan/general), title, content, sort_order
- **ClientMemo**：organization_id, client_profile_id, therapist_id, category(personality/trigger/preference/family/other), content(加密)

## Service
- **FormService**：Template CRUD（含巢狀 Section/Field）+ CloneTemplate + SubmitForm + GetSubmission。DatabaseSeeder 建系統預設初診評估表（7 區塊）。
- **SessionNoteService**：CRUD + Lock（is_locked=true+稽核）+ Unlock（解鎖前快照存 AuditLog.changes+稽核）。僅 note 的 therapist_id 可操作。GetByClient + GetByAppointment。
- **PhraseService**：CRUD，依 therapist_id 隔離。
- **ClientMemoService**：CRUD，GetByClient 回傳所有備忘並標記哪些是當前心理師（可編輯）vs 他人（唯讀）。

## Controller
- FormTemplatesController：CRUD + clone
- FormSubmissionsController：submit + get
- SessionNotesController：CRUD + lock/unlock
- PhraseTemplatesController：CRUD
- ClientMemosController：CRUD

## 交付標準
- [ ] 動態表單 Template CRUD（含巢狀 Section → Field）
- [ ] 系統預設初診表可複製
- [ ] FormSubmission 值加密儲存+正確解密
- [ ] SessionNote SOAP + free_text CRUD
- [ ] Lock/Unlock + 稽核快照
- [ ] PhraseTemplate CRUD 依 therapist 隔離
- [ ] ClientMemo CRUD + 可編輯/唯讀標記
