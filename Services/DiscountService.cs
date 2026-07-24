using consulcore_backend.Data;
using consulcore_backend.DTOs.Discount;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Services;

public class DiscountService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public DiscountService(AppDbContext db, IAuditService audit) { _db = db; _audit = audit; }

    public async Task<List<DiscountResponseDto>> GetAllAsync()
    {
        return await _db.Discounts.OrderBy(d => d.Name)
            .Select(d => new DiscountResponseDto
            {
                Id = d.Id, Name = d.Name, DiscountType = d.DiscountType,
                PercentageValue = d.PercentageValue, FixedAmount = d.FixedAmount,
                ApplicableRole = d.ApplicableRole, IsActive = d.IsActive,
            }).ToListAsync();
    }

    public async Task<DiscountResponseDto> CreateAsync(CreateDiscountDto dto, int orgId)
    {
        var discount = new Models.Discount
        {
            OrganizationId = orgId, Name = dto.Name, DiscountType = dto.DiscountType,
            PercentageValue = dto.PercentageValue, FixedAmount = dto.FixedAmount,
            ApplicableRole = dto.ApplicableRole,
        };
        _db.Discounts.Add(discount);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("create", "discount", discount.Id, $"建立折扣「{discount.Name}」");
        return new DiscountResponseDto
        {
            Id = discount.Id, Name = discount.Name, DiscountType = discount.DiscountType,
            PercentageValue = discount.PercentageValue, FixedAmount = discount.FixedAmount,
            ApplicableRole = discount.ApplicableRole, IsActive = discount.IsActive,
        };
    }

    public async Task<DiscountResponseDto> UpdateAsync(int id, UpdateDiscountDto dto)
    {
        var d = await _db.Discounts.FindAsync(id) ?? throw new KeyNotFoundException("折扣不存在");
        if (dto.Name != null) d.Name = dto.Name;
        if (dto.PercentageValue.HasValue) d.PercentageValue = dto.PercentageValue;
        if (dto.FixedAmount.HasValue) d.FixedAmount = dto.FixedAmount;
        if (dto.IsActive.HasValue) d.IsActive = dto.IsActive.Value;
        d.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new DiscountResponseDto
        {
            Id = d.Id, Name = d.Name, DiscountType = d.DiscountType,
            PercentageValue = d.PercentageValue, FixedAmount = d.FixedAmount,
            ApplicableRole = d.ApplicableRole, IsActive = d.IsActive,
        };
    }

    public async Task DeleteAsync(int id)
    {
        var d = await _db.Discounts.FindAsync(id) ?? throw new KeyNotFoundException("折扣不存在");
        _db.Discounts.Remove(d);
        await _db.SaveChangesAsync();
    }

    public async Task AssignDiscountAsync(AssignDiscountDto dto, int orgId, int assignedBy)
    {
        _db.ClientDiscounts.Add(new ClientDiscount
        {
            OrganizationId = orgId, ClientProfileId = dto.ClientProfileId,
            DiscountId = dto.DiscountId, AssignedBy = assignedBy, Note = dto.Note,
        });
        await _db.SaveChangesAsync();
        await _audit.LogAsync("assign", "client_discount", dto.DiscountId,
            $"指派折扣給個案 {dto.ClientProfileId}");
    }

    public async Task RemoveDiscountAsync(int clientProfileId, int discountId)
    {
        var cd = await _db.ClientDiscounts
            .FirstOrDefaultAsync(c => c.ClientProfileId == clientProfileId && c.DiscountId == discountId && c.IsActive)
            ?? throw new KeyNotFoundException("找不到此折扣指派");
        cd.IsActive = false;
        await _db.SaveChangesAsync();
    }

    // 找到個案適用的折扣（手動指派 + 角色自動套用）
    public async Task<(Models.Discount? discount, decimal discountAmount)> FindApplicableDiscountAsync(
        int clientProfileId, decimal originalAmount)
    {
        // 1. 手動指派的折扣
        var clientDiscount = await _db.ClientDiscounts
            .Include(cd => cd.Discount)
            .Where(cd => cd.ClientProfileId == clientProfileId && cd.IsActive && cd.Discount.IsActive)
            .FirstOrDefaultAsync();

        if (clientDiscount != null)
            return (clientDiscount.Discount, CalculateDiscount(clientDiscount.Discount, originalAmount));

        // 2. 角色自動套用 (BR-11)
        var clientRoles = await _db.ClientProfiles
            .Where(c => c.Id == clientProfileId && c.UserId != null)
            .SelectMany(c => c.User!.UserRoles)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

        if (clientRoles.Count > 0)
        {
            var roleDiscount = await _db.Discounts
                .Where(d => d.IsActive && d.ApplicableRole != null && clientRoles.Contains(d.ApplicableRole))
                .FirstOrDefaultAsync();

            if (roleDiscount != null)
                return (roleDiscount, CalculateDiscount(roleDiscount, originalAmount));
        }

        return (null, 0);
    }

    private static decimal CalculateDiscount(Models.Discount discount, decimal amount)
    {
        if (discount.DiscountType == DiscountTypes.Percentage && discount.PercentageValue.HasValue)
            return Math.Round(amount * discount.PercentageValue.Value / 100, 2);
        if (discount.DiscountType == DiscountTypes.FixedAmount && discount.FixedAmount.HasValue)
            return Math.Min(discount.FixedAmount.Value, amount);
        return 0;
    }
}
