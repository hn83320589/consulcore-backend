namespace consulcore_backend.Authorization;

/// <summary>授權 policy 名稱，以及每個 policy 要求的權限碼。Program.cs 依此註冊。</summary>
public static class Policies
{
    public const string CanViewClients = "CanViewClients";
    public const string CanCreateClients = "CanCreateClients";
    public const string CanUpdateClients = "CanUpdateClients";
    public const string CanDeleteClients = "CanDeleteClients";
    public const string CanManageClients = "CanManageClients";
    public const string CanViewAppointments = "CanViewAppointments";
    public const string CanCreateAppointments = "CanCreateAppointments";
    public const string CanUpdateAppointments = "CanUpdateAppointments";
    public const string CanDeleteAppointments = "CanDeleteAppointments";
    public const string CanViewSchedules = "CanViewSchedules";
    public const string CanCreateSchedules = "CanCreateSchedules";
    public const string CanUpdateSchedules = "CanUpdateSchedules";
    public const string CanDeleteSchedules = "CanDeleteSchedules";
    public const string CanViewSessions = "CanViewSessions";
    public const string CanCreateSessions = "CanCreateSessions";
    public const string CanUpdateSessions = "CanUpdateSessions";
    public const string CanLockSessions = "CanLockSessions";
    public const string CanViewConsents = "CanViewConsents";
    public const string CanCreateConsents = "CanCreateConsents";
    public const string CanUpdateConsents = "CanUpdateConsents";
    public const string CanVoidConsents = "CanVoidConsents";
    public const string CanViewBilling = "CanViewBilling";
    public const string CanCreateBilling = "CanCreateBilling";
    public const string CanUpdateBilling = "CanUpdateBilling";
    public const string CanRefundBilling = "CanRefundBilling";
    public const string CanManageNotifications = "CanManageNotifications";
    public const string CanManageStaff = "CanManageStaff";
    public const string CanManageRooms = "CanManageRooms";
    public const string CanViewAudit = "CanViewAudit";
    public const string CanViewPerformance = "CanViewPerformance";
    public const string CanManagePerformance = "CanManagePerformance";
    public const string CanManageOrgSettings = "CanManageOrgSettings";
    public const string CanListTherapists = "CanListTherapists";

    public static readonly IReadOnlyDictionary<string, string> PermissionByPolicy = new Dictionary<string, string>
    {
        [CanViewClients] = Permissions.ClientView,
        [CanCreateClients] = Permissions.ClientCreate,
        [CanUpdateClients] = Permissions.ClientUpdate,
        [CanDeleteClients] = Permissions.ClientDelete,
        [CanManageClients] = Permissions.ClientManage,
        [CanViewAppointments] = Permissions.AppointmentView,
        [CanCreateAppointments] = Permissions.AppointmentCreate,
        [CanUpdateAppointments] = Permissions.AppointmentUpdate,
        [CanDeleteAppointments] = Permissions.AppointmentDelete,
        [CanViewSchedules] = Permissions.ScheduleView,
        [CanCreateSchedules] = Permissions.ScheduleCreate,
        [CanUpdateSchedules] = Permissions.ScheduleUpdate,
        [CanDeleteSchedules] = Permissions.ScheduleDelete,
        [CanViewSessions] = Permissions.SessionView,
        [CanCreateSessions] = Permissions.SessionCreate,
        [CanUpdateSessions] = Permissions.SessionUpdate,
        [CanLockSessions] = Permissions.SessionLock,
        [CanViewConsents] = Permissions.ConsentView,
        [CanCreateConsents] = Permissions.ConsentCreate,
        [CanUpdateConsents] = Permissions.ConsentUpdate,
        [CanVoidConsents] = Permissions.ConsentVoid,
        [CanViewBilling] = Permissions.BillingView,
        [CanCreateBilling] = Permissions.BillingCreate,
        [CanUpdateBilling] = Permissions.BillingUpdate,
        [CanRefundBilling] = Permissions.BillingRefund,
        [CanManageNotifications] = Permissions.NotificationManage,
        [CanManageStaff] = Permissions.StaffManage,
        [CanManageRooms] = Permissions.RoomManage,
        [CanViewAudit] = Permissions.AuditView,
        [CanViewPerformance] = Permissions.PerformanceView,
        [CanManagePerformance] = Permissions.PerformanceManage,
        [CanManageOrgSettings] = Permissions.OrgSettings,
    };

    /// <summary>需要好幾個頁面共用的資料：角色擁有其中任一權限即可。</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> AnyPermissionByPolicy =
        new Dictionary<string, IReadOnlyList<string>>
        {
            // 排班、預約、考核頁的心理師下拉選單
            [CanListTherapists] = [Permissions.ScheduleView, Permissions.AppointmentView, Permissions.PerformanceView],
        };

    public static IEnumerable<string> All => PermissionByPolicy.Keys.Concat(AnyPermissionByPolicy.Keys);
}
