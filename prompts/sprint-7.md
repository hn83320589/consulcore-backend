# Sprint 7 — 後端：通知 + 考核 + 稽核完整化

## Entity
- **NotificationTemplate**：organization_id, event_type(account_created/appointment_created/appointment_cancelled/appointment_reminder/therapist_leave/series_renewal/package_low/package_expiring), channel(email/sms/in_app), subject, body_template, is_required(bool), is_active
- **NotificationLog**：template_id, recipient_id, recipient_type(client/therapist/admin), channel, status(queued/sent/failed/read), sent_at, read_at, error_message
- **NotificationPreference**：user_id, event_type, email_enabled(true), sms_enabled(true), in_app_enabled(true)
- **PerformanceReview**：organization_id, therapist_id, reviewer_id, period, professional_score(1-5), client_feedback_score(1-5), teamwork_score(1-5), overall_score(1-5), comment(加密), visibility(hidden/visible), visible_from(nullable), visible_until(nullable), opened_by(nullable), opened_at(nullable)
- **ViolationRecord**：organization_id, therapist_id, reported_by, incident_date, severity(low/medium/high/critical), description(加密), action_taken(加密), status(open/resolved/dismissed), resolved_at(nullable)

## Service

### NotificationService
- `Send(eventType, recipientId, templateVars)`：查 Template → 查 Preference → 啟用管道各產生 NotificationLog(queued) → 變數替換({{client_name}}等) → 發送(Email:SMTP / SMS:第三方 / in_app:直接寫入) → 更新 status
- GetUnreadCount / MarkAsRead / GetByUser
- **DatabaseSeeder**：每個 event_type × 3 channel 的預設範本

### 觸發點接線（在各 Service 中加入 NotificationService.Send 呼叫）
1. ClientService.CreateWithAccount → account_created（→個案）
2. AppointmentService.Create → appointment_created（→個案+心理師）
3. AppointmentService.Cancel → appointment_cancelled（→個案+心理師）
4. ScheduleService.BlockSlot → therapist_leave（→受影響已預約個案）
5. AppointmentSeriesService.GetPendingRenewal → series_renewal（→心理師）
6. BillingService.OnAppointmentCompleted → package_low（套餐≤2→心理師+行政）
7. BillingService.CheckExpiredPackages → package_expiring（→心理師+行政）

### PerformanceService
- Review CRUD (Owner) + SetVisibility(reviewId, visibility, from?, until?) + CheckVisibilitySchedule（到期回hidden）
- GetByTherapist（回傳已開放的）/ GetAllReviews (Owner)
- `GetMetrics(therapistId, dateRange)`：aggregate 7 項指標
  - 預約完成率、No Show率、紀錄及時性(avg時差)、鎖定率、留存率(>3個月active)、月諮商數/時數、收費完成率
- `GetMetricsComparison(orgId, dateRange)`：所有心理師指標比較

### ViolationService
- CRUD + Resolve / Dismiss (Owner only)

### 稽核完整化
檢查所有 Service，確認以下操作都有 `_audit.LogAsync()`：
- 必記：SessionNote(CRUD+lock/unlock+檢視)、ClientProfile(CRUD+檢視)、ConsentRecord(sign/void)、PaymentRecord(所有異動)、User(權限變更/密碼重設)
- 選記：Appointment(CRUD)、ScheduleSlot(block/unblock)、系統設定修改

## Controller
- NotificationTemplatesController：CRUD (Owner)
- NotificationsController：list / unread-count / mark-read
- NotificationPreferencesController：get / update
- PerformanceReviewsController：CRUD + visibility + metrics + comparison
- ViolationRecordsController：CRUD + resolve / dismiss
- AuditLogsController：list(filters) + export(CSV/PDF)

## 交付標準
- [ ] 9 個通知觸發點全部接通
- [ ] 三管道發送正常（至少 in_app 實際運作，Email/SMS 可用 mock）
- [ ] 通知偏好可設定，必要通知不可關
- [ ] 量化指標 7 項正確 aggregate
- [ ] 考核 CRUD + 可見性控制（手動+排程）
- [ ] 違規紀錄 CRUD + resolve/dismiss
- [ ] AuditLog 所有必記觸發點接通
- [ ] AuditLog 查詢+匯出 API
