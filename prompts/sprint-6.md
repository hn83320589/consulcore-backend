# Sprint 6 — 後端：收費模組

## Entity
- **FeeSchedule**：organization_id, name, session_type(individual/couple/family), visit_type(initial/follow_up), therapist_id(nullable=機構通用), amount(decimal), currency(TWD), is_active
- **Package**：organization_id, name, session_type, total_sessions, total_amount, per_session_amount, valid_days, is_active
- **ClientPackage**：organization_id, client_profile_id, package_id, purchased_at, expires_at, sessions_used(0), sessions_remaining, status(active/expired/completed), payment_record_id
- **Discount**：therapist_id, name, discount_type(percentage/fixed_amount), percentage_value(nullable), fixed_amount(nullable), applicable_role(nullable), is_active
- **ClientDiscount**：organization_id, client_profile_id, discount_id, assigned_by, assigned_at, is_active, note
- **PaymentRecord**：organization_id, client_profile_id, appointment_id(nullable), client_package_id(nullable), discount_id(nullable), original_amount, final_amount, payment_method(cash/transfer/credit_card/line_pay/other), payment_type(single_session/package_purchase), status(paid/pending/refunded/waived), note, recorded_by, paid_at

## Service
- **FeeScheduleService**：CRUD + FindApplicableFee（心理師專屬 > 機構通用，BR-09）
- **PackageService**：CRUD
- **BillingService**：
  - OnAppointmentCompleted(appointmentId)：檢查套餐→有則扣次→無則查費率+查折扣→產生PaymentRecord(pending)→套餐≤2觸發提醒→套餐用完改completed
  - PurchasePackage + ConfirmPayment + Refund + Waive
  - CheckExpiredPackages：到期未用完改expired
- **DiscountService**：CRUD + FindApplicableDiscount + AssignDiscount + RemoveDiscount
- **ReportService**：GetMonthlyRevenue + GetRevenueByTherapist + GetPendingPayments + GetPackageStats

## Controller
- FeeSchedulesController, PackagesController, DiscountsController：CRUD
- BillingController：confirm/refund/waive/purchase-package/pending
- ReportsController：monthly-revenue/by-therapist/pending/package-stats

## 交付標準
- [ ] 費率查找邏輯正確（心理師專屬>通用）
- [ ] 預約完成→自動產生PaymentRecord（含折扣計算）
- [ ] 套餐購買+扣次+到期+用完全流程
- [ ] 折扣自動套用+original/final amount正確
- [ ] 報表aggregate API正確
