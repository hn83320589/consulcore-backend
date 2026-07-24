# Sprint 4 — 後端：預約管理

## Entity
- **Appointment**：organization_id, client_profile_id, therapist_id, room_id, schedule_slot_id, series_id(nullable), session_type(individual/couple/family), visit_type(initial/follow_up), status(scheduled/completed/cancelled/no_show), note, created_by
- **AppointmentSeries**：organization_id, client_profile_id, therapist_id, day_of_week, start_time, frequency(weekly/biweekly), status(active/pending_confirmation/ended), expires_at
- ScheduleSlot 加欄位：`is_reserved_hold` (bool, default false)

## Service
- **AppointmentService**：
  - Create → Slot 改 reserved，驗證 Slot available + 無時段衝突
  - Complete / Cancel / MarkNoShow → 狀態流轉，cancel 釋放 slot
  - Reschedule(id, newSlotId) → 原 slot 釋放 + 新 slot reserved
  - GetByDateRange(start, end, therapistId?) → 日曆視圖
  - GetList(filters) → 列表視圖
  - GetAvailableSlots(therapistId?, dateRange) → 個案預約用，排除 reserved_hold
  - 溫和提示邏輯：個案預約非慣用心理師時回傳提示訊息（不阻擋）
- **AppointmentSeriesService**：
  - Create(clientId, therapistId, dow, time, freq, months) → 建系列 + 自動產生 Appointment
  - Renew(seriesId, months) → 延長 + 產生新 Appointment
  - End(seriesId)
  - GetPendingRenewal() → 剩餘 ≤ 2 次的系列

## Controller
- AppointmentsController：CRUD + complete/cancel/no-show/reschedule + available-slots + calendar-view + list-view
- AppointmentSeriesController：create / renew / end / pending-renewal

## 交付標準
- [ ] 預約 CRUD + Slot 狀態連動
- [ ] 狀態流轉正確（complete/cancel/no_show/reschedule）
- [ ] 常態性預約建立+續約+結束
- [ ] GetAvailableSlots 排除 reserved_hold
- [ ] 時段衝突驗證（同個案+同心理師）
- [ ] 溫和提示 API 回傳
