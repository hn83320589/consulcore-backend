namespace consulcore_backend.Services;

public interface IAuditService
{
    Task LogAsync(string action, string resourceType, int resourceId, string description,
        object? changes = null, int? organizationId = null, int? userId = null);

    /// <summary>背景排程等沒有登入者的動作；UserId 存 null，查詢時顯示為「系統」</summary>
    Task LogSystemAsync(int organizationId, string action, string resourceType, int resourceId, string description,
        object? changes = null);
}
