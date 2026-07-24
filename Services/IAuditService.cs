namespace consulcore_backend.Services;

public interface IAuditService
{
    Task LogAsync(string action, string resourceType, int resourceId, string description,
        object? changes = null, int? organizationId = null, int? userId = null);
}
