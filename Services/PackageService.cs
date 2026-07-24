using consulcore_backend.Data;
using consulcore_backend.DTOs.Package;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Services;

public class PackageService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public PackageService(AppDbContext db, IAuditService audit) { _db = db; _audit = audit; }

    public async Task<List<PackageResponseDto>> GetAllAsync()
    {
        return await _db.Packages.OrderBy(p => p.Name)
            .Select(p => MapToDto(p)).ToListAsync();
    }

    public async Task<PackageResponseDto> CreateAsync(CreatePackageDto dto, int orgId)
    {
        var pkg = new Models.Package
        {
            OrganizationId = orgId, Name = dto.Name, SessionType = dto.SessionType,
            TotalSessions = dto.TotalSessions, TotalAmount = dto.TotalAmount,
            PerSessionAmount = dto.TotalAmount / dto.TotalSessions,
            ValidDays = dto.ValidDays,
        };
        _db.Packages.Add(pkg);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("create", "package", pkg.Id, $"建立套餐「{pkg.Name}」");
        return MapToDto(pkg);
    }

    public async Task<PackageResponseDto> UpdateAsync(int id, UpdatePackageDto dto)
    {
        var pkg = await _db.Packages.FindAsync(id)
            ?? throw new KeyNotFoundException("套餐不存在");
        if (dto.Name != null) pkg.Name = dto.Name;
        if (dto.TotalAmount.HasValue) { pkg.TotalAmount = dto.TotalAmount.Value; pkg.PerSessionAmount = dto.TotalAmount.Value / pkg.TotalSessions; }
        if (dto.ValidDays.HasValue) pkg.ValidDays = dto.ValidDays.Value;
        if (dto.IsActive.HasValue) pkg.IsActive = dto.IsActive.Value;
        pkg.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapToDto(pkg);
    }

    public async Task DeleteAsync(int id)
    {
        var pkg = await _db.Packages.FindAsync(id) ?? throw new KeyNotFoundException("套餐不存在");
        _db.Packages.Remove(pkg);
        await _db.SaveChangesAsync();
    }

    private static PackageResponseDto MapToDto(Models.Package p) => new()
    {
        Id = p.Id, Name = p.Name, SessionType = p.SessionType,
        TotalSessions = p.TotalSessions, TotalAmount = p.TotalAmount,
        PerSessionAmount = p.PerSessionAmount, ValidDays = p.ValidDays, IsActive = p.IsActive,
    };
}
