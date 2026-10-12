using consulcore_backend.Data;
using consulcore_backend.DTOs.Organization;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Services;

/// <summary>登入者所屬機構的基本資料。機構 id 一律取自 JWT，不接受外部指定。</summary>
public class OrganizationService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public OrganizationService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<OrganizationResponseDto> GetAsync(int orgId)
    {
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == orgId)
            ?? throw new KeyNotFoundException("機構不存在");
        return Map(org);
    }

    public async Task<OrganizationResponseDto> UpdateAsync(int orgId, UpdateOrganizationDto dto)
    {
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == orgId)
            ?? throw new KeyNotFoundException("機構不存在");
        var before = Map(org);

        org.Name = dto.Name.Trim();
        org.Phone = Blank(dto.Phone);
        org.Email = Blank(dto.Email);
        org.Address = Blank(dto.Address);
        org.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var after = Map(org);
        await _audit.LogAsync("update", "organization", org.Id, "更新機構基本資料", new { before, after });
        return after;
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static OrganizationResponseDto Map(Models.Organization o) => new()
    {
        Id = o.Id,
        Name = o.Name,
        Phone = o.Phone,
        Email = o.Email,
        Address = o.Address,
        CreatedAt = o.CreatedAt,
        UpdatedAt = o.UpdatedAt,
    };
}
