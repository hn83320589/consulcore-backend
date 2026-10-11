namespace consulcore_backend.Authorization;

/// <summary>權限碼。與 DatabaseSeeder 寫入的 Permission.Code 一一對應（測試會檢查）。</summary>
public static class Permissions
{
    public const string ClientView = "client.view";
    public const string ClientCreate = "client.create";
    public const string ClientUpdate = "client.update";
    public const string ClientDelete = "client.delete";
    public const string ClientManage = "client.manage";
    public const string AppointmentView = "appointment.view";
    public const string AppointmentCreate = "appointment.create";
    public const string AppointmentUpdate = "appointment.update";
    public const string AppointmentDelete = "appointment.delete";
    public const string ScheduleView = "schedule.view";
    public const string ScheduleCreate = "schedule.create";
    public const string ScheduleUpdate = "schedule.update";
    public const string ScheduleDelete = "schedule.delete";
    public const string SessionView = "session.view";
    public const string SessionCreate = "session.create";
    public const string SessionUpdate = "session.update";
    public const string SessionLock = "session.lock";
    public const string ConsentView = "consent.view";
    public const string ConsentCreate = "consent.create";
    public const string ConsentUpdate = "consent.update";
    public const string ConsentVoid = "consent.void";
    public const string BillingView = "billing.view";
    public const string BillingCreate = "billing.create";
    public const string BillingUpdate = "billing.update";
    public const string BillingRefund = "billing.refund";
    public const string NotificationManage = "notification.manage";
    public const string StaffManage = "staff.manage";
    public const string RoomManage = "room.manage";
    public const string AuditView = "audit.view";
    public const string PerformanceView = "performance.view";
    public const string PerformanceManage = "performance.manage";
    public const string OrgSettings = "org.settings";

    public static readonly IReadOnlyList<string> All =
    [
        ClientView,
        ClientCreate,
        ClientUpdate,
        ClientDelete,
        ClientManage,
        AppointmentView,
        AppointmentCreate,
        AppointmentUpdate,
        AppointmentDelete,
        ScheduleView,
        ScheduleCreate,
        ScheduleUpdate,
        ScheduleDelete,
        SessionView,
        SessionCreate,
        SessionUpdate,
        SessionLock,
        ConsentView,
        ConsentCreate,
        ConsentUpdate,
        ConsentVoid,
        BillingView,
        BillingCreate,
        BillingUpdate,
        BillingRefund,
        NotificationManage,
        StaffManage,
        RoomManage,
        AuditView,
        PerformanceView,
        PerformanceManage,
        OrgSettings,
    ];
}
