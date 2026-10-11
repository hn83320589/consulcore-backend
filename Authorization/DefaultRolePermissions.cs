namespace consulcore_backend.Authorization;

/// <summary>開設機構時，各系統角色預設取得的權限（測試會檢查每個權限碼都存在且實際寫入）。</summary>
public static class DefaultRolePermissions
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ByRole = new Dictionary<string, IReadOnlyList<string>>
    {
        [Roles.Owner] =
        [
            Permissions.ClientView, Permissions.ClientCreate, Permissions.ClientUpdate, Permissions.ClientDelete, Permissions.ClientManage,
            Permissions.AppointmentView, Permissions.AppointmentCreate, Permissions.AppointmentUpdate, Permissions.AppointmentDelete,
            Permissions.ScheduleView, Permissions.ScheduleCreate, Permissions.ScheduleUpdate, Permissions.ScheduleDelete,
            Permissions.SessionView, Permissions.SessionCreate, Permissions.SessionUpdate, Permissions.SessionLock,
            Permissions.ConsentView, Permissions.ConsentCreate, Permissions.ConsentUpdate, Permissions.ConsentVoid,
            Permissions.BillingView, Permissions.BillingCreate, Permissions.BillingUpdate, Permissions.BillingRefund,
            Permissions.NotificationManage,
            Permissions.StaffManage,
            Permissions.RoomManage,
            Permissions.AuditView,
            Permissions.PerformanceView, Permissions.PerformanceManage,
            Permissions.OrgSettings
        ],
        [Roles.Admin] =
        [
            Permissions.ClientView, Permissions.ClientCreate, Permissions.ClientUpdate, Permissions.ClientManage,
            Permissions.AppointmentView, Permissions.AppointmentCreate, Permissions.AppointmentUpdate, Permissions.AppointmentDelete,
            Permissions.ScheduleView, Permissions.ScheduleCreate, Permissions.ScheduleUpdate, Permissions.ScheduleDelete,
            Permissions.SessionView,
            Permissions.ConsentView, Permissions.ConsentCreate, Permissions.ConsentUpdate,
            Permissions.BillingView, Permissions.BillingCreate, Permissions.BillingUpdate,
            Permissions.NotificationManage,
            Permissions.StaffManage,
            Permissions.RoomManage,
            Permissions.AuditView,
            Permissions.PerformanceView
        ],
        [Roles.Therapist] =
        [
            Permissions.ClientView, Permissions.ClientCreate, Permissions.ClientUpdate,
            Permissions.AppointmentView, Permissions.AppointmentCreate, Permissions.AppointmentUpdate,
            Permissions.ScheduleView,
            Permissions.SessionView, Permissions.SessionCreate, Permissions.SessionUpdate, Permissions.SessionLock,
            // 不含 billing.view：收費 / 報表 / 匯出是全機構資料，心理師預設不可見（決策 D7）
            Permissions.ConsentView, Permissions.ConsentCreate
        ],
        [Roles.Client] =
        [
            Permissions.AppointmentView,
            Permissions.ConsentView
        ],
    };
}
