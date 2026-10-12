using consulcore_backend.Data;
using consulcore_backend.DTOs.Report;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Services;

public class ReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db) { _db = db; }

    public async Task<MonthlyRevenueDto> GetMonthlyRevenueAsync(int year, int month)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        // 在資料庫端依狀態加總，不把整個月的收費紀錄載入記憶體
        var byStatus = await _db.PaymentRecords
            .Where(r => r.CreatedAt >= startDate && r.CreatedAt < endDate)
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Amount = g.Sum(r => r.FinalAmount) })
            .ToListAsync();
        decimal AmountOf(string status) => byStatus.Where(s => s.Status == status).Sum(s => s.Amount);

        return new MonthlyRevenueDto
        {
            Year = year, Month = month,
            TotalRevenue = AmountOf(PaymentStatus.Paid),
            TotalTransactions = byStatus.Sum(s => s.Count),
            PaidAmount = AmountOf(PaymentStatus.Paid),
            PendingAmount = AmountOf(PaymentStatus.Pending),
            RefundedAmount = AmountOf(PaymentStatus.Refunded),
            WaivedAmount = AmountOf(PaymentStatus.Waived),
        };
    }

    public async Task<List<TherapistRevenueDto>> GetRevenueByTherapistAsync(int year, int month)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        return await _db.PaymentRecords
            .Where(r => r.CreatedAt >= startDate && r.CreatedAt < endDate
                && r.Status == PaymentStatus.Paid && r.AppointmentId != null)
            .Include(r => r.Appointment)
                .ThenInclude(a => a!.TherapistUser)
            .GroupBy(r => new { r.Appointment!.TherapistUserId, r.Appointment.TherapistUser.DisplayName })
            .Select(g => new TherapistRevenueDto
            {
                TherapistUserId = g.Key.TherapistUserId,
                TherapistName = g.Key.DisplayName,
                TotalRevenue = g.Sum(r => r.FinalAmount),
                TotalSessions = g.Count(),
            })
            .ToListAsync();
    }

    public async Task<List<DTOs.Billing.PaymentRecordResponseDto>> GetPendingPaymentsAsync()
    {
        return await _db.PaymentRecords
            .Include(r => r.ClientProfile)
            .Include(r => r.Discount)
            .Where(r => r.Status == PaymentStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new DTOs.Billing.PaymentRecordResponseDto
            {
                Id = r.Id, ClientProfileId = r.ClientProfileId, ClientName = r.ClientProfile.Name,
                AppointmentId = r.AppointmentId, OriginalAmount = r.OriginalAmount,
                FinalAmount = r.FinalAmount, PaymentMethod = r.PaymentMethod,
                PaymentType = r.PaymentType, Status = r.Status, CreatedAt = r.CreatedAt,
            })
            .ToListAsync();
    }

    public async Task<PackageStatsDto> GetPackageStatsAsync()
    {
        // 原本載入全部套餐並 Include 個案，會解密每位個案的 14 個個資欄位；改為資料庫端計數，
        // 只有「低剩餘」清單需要個案姓名，只解密這幾筆的姓名
        var countByStatus = await _db.ClientPackages
            .GroupBy(cp => cp.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        var packageRevenue = await _db.PaymentRecords
            .Where(r => r.PaymentType == PaymentTypes.PackagePurchase && r.Status == PaymentStatus.Paid)
            .SumAsync(r => r.FinalAmount);

        var lowRemaining = await _db.ClientPackages
            .Where(p => p.Status == ClientPackageStatus.Active && p.SessionsRemaining <= 2)
            .OrderBy(p => p.Id)
            .Select(p => new PackageRemainingDto
            {
                ClientPackageId = p.Id, ClientName = p.ClientProfile.Name,
                PackageName = p.Package.Name, SessionsRemaining = p.SessionsRemaining,
                ExpiresAt = p.ExpiresAt,
            }).ToListAsync();

        return new PackageStatsDto
        {
            ActivePackages = countByStatus.GetValueOrDefault(ClientPackageStatus.Active),
            ExpiredPackages = countByStatus.GetValueOrDefault(ClientPackageStatus.Expired),
            CompletedPackages = countByStatus.GetValueOrDefault(ClientPackageStatus.Completed),
            TotalPackageRevenue = packageRevenue,
            LowRemaining = lowRemaining,
        };
    }
}
