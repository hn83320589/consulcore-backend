using System.Text.Json;
using consulcore_backend.Authorization;
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
            ?? int.Parse(httpContext?.User?.FindFirst(JwtClaims.OrganizationId)?.Value ?? "0");
        var uid = userId
            ?? int.Parse(httpContext?.User?.FindFirst(JwtClaims.UserId)?.Value ?? "0");

        if (orgId == 0 || uid == 0)
        {
            _logger.LogWarning(
                "[Audit] 無法取得 organizationId 或 userId: {Action} {ResourceType}#{ResourceId}",
                action, resourceType, resourceId);
            return;
        }

        await WriteAsync(orgId, uid, action, resourceType, resourceId, description, changes,
            httpContext?.Connection?.RemoteIpAddress?.ToString());
    }

    public Task LogSystemAsync(int organizationId, string action, string resourceType, int resourceId,
        string description, object? changes = null) =>
        WriteAsync(organizationId, null, action, resourceType, resourceId, description, changes, ipAddress: null);

    private async Task WriteAsync(int organizationId, int? userId, string action, string resourceType,
        int resourceId, string description, object? changes, string? ipAddress)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            OrganizationId = organizationId,
            UserId = userId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Description = description,
            Changes = changes != null ? JsonSerializer.Serialize(changes) : null,
            IpAddress = ipAddress,
        });
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "[Audit] {Action} {ResourceType}#{ResourceId}: {Description}",
            action, resourceType, resourceId, description);
    }
}
