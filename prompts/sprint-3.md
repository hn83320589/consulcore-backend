# Sprint 3 — 後端：診間與排班

## Entity
- **Room**：organization_id, name, is_active
- **TherapistRoom**：therapist_id, room_id, assigned_at
- **ScheduleTemplate**：organization_id, therapist_id, day_of_week(0-6), start_time, end_time, slot_duration(min), is_active
- **ScheduleSlot**：organization_id, therapist_id, room_id, date, start_time, end_time, status(available/reserved/blocked/cancelled), note

## Service
- **RoomService**：CRUD + GetActiveCount（最大同時段諮商數）
- **ScheduleService**：
  - Template CRUD
  - `ExpandSchedule(startDate, endDate)`：讀 active templates → 逐日產生 Slot → 已存在的不覆蓋 → 自動分配 room（從 TherapistRoom） → 回傳新產生數量
  - `ManualExpand(year, month)`
  - `BlockSlot(slotId, note)` / `UnblockSlot(slotId)`
  - 驗證：date < today 不可操作（BR-05）
  - 衝突檢查：同時段心理師數 > 診間數回傳警告

## Controller
- RoomsController：CRUD + active-count
- ScheduleTemplatesController：CRUD
- ScheduleSlotsController：expand / manual-expand / block / unblock / list (by date range + therapist)

## 交付標準
- [ ] 診間 CRUD + 心理師指派
- [ ] 班表範本 CRUD
- [ ] 展班產生 ScheduleSlot（不覆蓋既有）
- [ ] BlockSlot/UnblockSlot + 過去日期不可操作
- [ ] 衝突檢查回傳警告
