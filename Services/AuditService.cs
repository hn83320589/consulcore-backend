using System.Text.Json;
using consulcore_backend.Data;
using consulcore_backend.Models;

namespace consulcore_backend.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        AppDbContext db,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditService> logger)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(string action, string resourceType, int resourceId, string description,
        object? changes = null, int? organizationId = null, int? userId = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        var orgId = organizationId
            ?? int.Parse(httpContext?.User?.FindFirst("organizationId")?.Value ?? "0");
        var uid = userId
            ?? int.Parse(httpContext?.User?.FindFirst("userId")?.Value ?? "0");

        if (orgId == 0 || uid == 0)
        {
            _logger.LogWarning(
                "[Audit] 無法取得 organizationId 或 userId: {Action} {ResourceType}#{ResourceId}",
                action, resourceType, resourceId);
            return;
        }

        var changesJson = changes != null ? JsonSerializer.Serialize(changes) : null;

        var auditLog = new AuditLog
        {
            OrganizationId = orgId,
            UserId = uid,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Description = description,
            Changes = changesJson,
            IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString()
        };

        _db.AuditLogs.Add(auditLog);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "[Audit] {Action} {ResourceType}#{ResourceId}: {Description}",
            action, resourceType, resourceId, description);
    }
}
