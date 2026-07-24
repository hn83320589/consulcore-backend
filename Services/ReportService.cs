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

        var records = await _db.PaymentRecords
            .Where(r => r.CreatedAt >= startDate && r.CreatedAt < endDate)
            .ToListAsync();

        return new MonthlyRevenueDto
        {
            Year = year, Month = month,
            TotalRevenue = records.Where(r => r.Status == PaymentStatus.Paid).Sum(r => r.FinalAmount),
            TotalTransactions = records.Count,
            PaidAmount = records.Where(r => r.Status == PaymentStatus.Paid).Sum(r => r.FinalAmount),
            PendingAmount = records.Where(r => r.Status == PaymentStatus.Pending).Sum(r => r.FinalAmount),
            RefundedAmount = records.Where(r => r.Status == PaymentStatus.Refunded).Sum(r => r.FinalAmount),
            WaivedAmount = records.Where(r => r.Status == PaymentStatus.Waived).Sum(r => r.FinalAmount),
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
        var packages = await _db.ClientPackages
            .Include(cp => cp.ClientProfile)
            .Include(cp => cp.Package)
            .ToListAsync();

        var paymentRecords = await _db.PaymentRecords
            .Where(r => r.PaymentType == PaymentTypes.PackagePurchase && r.Status == PaymentStatus.Paid)
            .ToListAsync();

        return new PackageStatsDto
        {
            ActivePackages = packages.Count(p => p.Status == ClientPackageStatus.Active),
            ExpiredPackages = packages.Count(p => p.Status == ClientPackageStatus.Expired),
            CompletedPackages = packages.Count(p => p.Status == ClientPackageStatus.Completed),
            TotalPackageRevenue = paymentRecords.Sum(r => r.FinalAmount),
            LowRemaining = packages
                .Where(p => p.Status == ClientPackageStatus.Active && p.SessionsRemaining <= 2)
                .Select(p => new PackageRemainingDto
                {
                    ClientPackageId = p.Id, ClientName = p.ClientProfile.Name,
                    PackageName = p.Package.Name, SessionsRemaining = p.SessionsRemaining,
                    ExpiresAt = p.ExpiresAt,
                }).ToList(),
        };
    }
}
