using consulcore_backend.Data;
using consulcore_backend.DTOs.Billing;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Services;

public class BillingService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    private readonly FeeScheduleService _feeService;
    private readonly DiscountService _discountService;
    private readonly NotificationService _notify;

    public BillingService(AppDbContext db, IAuditService audit,
        FeeScheduleService feeService, DiscountService discountService, NotificationService notify)
    {
        _db = db; _audit = audit; _feeService = feeService; _discountService = discountService; _notify = notify;
    }

    public async Task<PaymentRecordResponseDto?> OnAppointmentCompletedAsync(
        int appointmentId, int recordedBy)
    {
        var apt = await _db.Appointments
            .Include(a => a.ClientProfile)
            .FirstOrDefaultAsync(a => a.Id == appointmentId)
            ?? throw new KeyNotFoundException("預約不存在");

        // 1. 檢查是否有 active 套餐
        var clientPackage = await _db.ClientPackages
            .Include(cp => cp.Package)
            .Where(cp => cp.ClientProfileId == apt.ClientProfileId
                && cp.Status == ClientPackageStatus.Active
                && cp.SessionsRemaining > 0
                && cp.ExpiresAt > DateTime.UtcNow)
            .OrderBy(cp => cp.ExpiresAt)
            .FirstOrDefaultAsync();

        if (clientPackage != null)
        {
            // 用套餐扣次
            clientPackage.SessionsUsed++;
            clientPackage.SessionsRemaining--;

            if (clientPackage.SessionsRemaining == 0)
                clientPackage.Status = ClientPackageStatus.Completed;

            var record = new PaymentRecord
            {
                OrganizationId = apt.OrganizationId,
                ClientProfileId = apt.ClientProfileId,
                AppointmentId = appointmentId,
                ClientPackageId = clientPackage.Id,
                OriginalAmount = clientPackage.Package.PerSessionAmount,
                FinalAmount = 0, // 套餐已預付
                PaymentMethod = PaymentMethods.Other,
                PaymentType = PaymentTypes.SingleSession,
                Status = PaymentStatus.Paid,
                RecordedBy = recordedBy,
                PaidAt = DateTime.UtcNow,
                Note = $"套餐扣次 ({clientPackage.SessionsUsed}/{clientPackage.Package.TotalSessions})",
            };
            _db.PaymentRecords.Add(record);
            await _db.SaveChangesAsync();

            await _audit.LogAsync("package_deduct", "payment_record", record.Id,
                $"套餐扣次 (remaining={clientPackage.SessionsRemaining})");

            // BR-10: 套餐 ≤2 觸發提醒（心理師 + 行政）
            if (clientPackage.SessionsRemaining <= 2 && clientPackage.SessionsRemaining > 0)
            {
                var pkgVars = new Dictionary<string, string>
                {
                    ["client_name"] = apt.ClientProfile?.Name ?? "",
                    ["package_name"] = clientPackage.Package.Name,
                    ["remaining"] = clientPackage.SessionsRemaining.ToString(),
                };
                await _notify.SendAsync(NotificationEventType.PackageLow, apt.TherapistUserId, "therapist", apt.OrganizationId, pkgVars);
                foreach (var adminId in await GetAdminUserIdsAsync(apt.OrganizationId))
                    await _notify.SendAsync(NotificationEventType.PackageLow, adminId, "admin", apt.OrganizationId, pkgVars);
            }

            return await GetPaymentByIdAsync(record.Id);
        }

        // 2. 無套餐：查費率
        var fee = await _feeService.FindApplicableFeeAsync(
            apt.TherapistUserId, apt.SessionType, apt.VisitType);

        if (fee == null) return null; // 無費率設定

        var originalAmount = fee.Value;

        // 3. 查折扣
        var (discount, discountAmount) = await _discountService.FindApplicableDiscountAsync(
            apt.ClientProfileId, originalAmount);

        var finalAmount = originalAmount - discountAmount;

        var paymentRecord = new PaymentRecord
        {
            OrganizationId = apt.OrganizationId,
            ClientProfileId = apt.ClientProfileId,
            AppointmentId = appointmentId,
            DiscountId = discount?.Id,
            OriginalAmount = originalAmount,
            FinalAmount = finalAmount,
            PaymentMethod = PaymentMethods.Cash,
            PaymentType = PaymentTypes.SingleSession,
            Status = PaymentStatus.Pending,
            RecordedBy = recordedBy,
        };
        _db.PaymentRecords.Add(paymentRecord);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("create", "payment_record", paymentRecord.Id,
            $"自動產生收費 original={originalAmount} final={finalAmount}");

        return await GetPaymentByIdAsync(paymentRecord.Id);
    }

    public async Task<ClientPackageResponseDto> PurchasePackageAsync(
        PurchasePackageDto dto, int orgId, int recordedBy)
    {
        var pkg = await _db.Packages.FindAsync(dto.PackageId)
            ?? throw new KeyNotFoundException("套餐不存在");

        var payment = new PaymentRecord
        {
            OrganizationId = orgId,
            ClientProfileId = dto.ClientProfileId,
            OriginalAmount = pkg.TotalAmount,
            FinalAmount = pkg.TotalAmount,
            PaymentMethod = dto.PaymentMethod,
            PaymentType = PaymentTypes.PackagePurchase,
            Status = PaymentStatus.Paid,
            RecordedBy = recordedBy,
            PaidAt = DateTime.UtcNow,
        };
        _db.PaymentRecords.Add(payment);
        await _db.SaveChangesAsync();

        var clientPkg = new ClientPackage
        {
            OrganizationId = orgId,
            ClientProfileId = dto.ClientProfileId,
            PackageId = dto.PackageId,
            ExpiresAt = DateTime.UtcNow.AddDays(pkg.ValidDays),
            SessionsRemaining = pkg.TotalSessions,
            PaymentRecordId = payment.Id,
        };
        _db.ClientPackages.Add(clientPkg);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("purchase", "client_package", clientPkg.Id,
            $"購買套餐「{pkg.Name}」({pkg.TotalSessions}次/${pkg.TotalAmount})");

        return await GetClientPackageByIdAsync(clientPkg.Id);
    }

    public async Task<PaymentRecordResponseDto> ConfirmPaymentAsync(int id, ConfirmPaymentDto dto)
    {
        var record = await _db.PaymentRecords.FindAsync(id)
            ?? throw new KeyNotFoundException("收費記錄不存在");
        if (record.Status != PaymentStatus.Pending)
            throw new InvalidOperationException("此記錄非待付款狀態");
        record.Status = PaymentStatus.Paid;
        record.PaymentMethod = dto.PaymentMethod;
        record.PaidAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("confirm", "payment_record", id, "確認收款");
        return await GetPaymentByIdAsync(id);
    }

    public async Task<PaymentRecordResponseDto> RefundAsync(int id, RefundDto dto)
    {
        var record = await _db.PaymentRecords.FindAsync(id)
            ?? throw new KeyNotFoundException("收費記錄不存在");
        if (record.Status != PaymentStatus.Paid)
            throw new InvalidOperationException("僅已付款記錄可退費");
        record.Status = PaymentStatus.Refunded;
        record.Note = dto.Note;
        record.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("refund", "payment_record", id, $"退費 ${record.FinalAmount}");
        return await GetPaymentByIdAsync(id);
    }

    public async Task<PaymentRecordResponseDto> WaiveAsync(int id)
    {
        var record = await _db.PaymentRecords.FindAsync(id)
            ?? throw new KeyNotFoundException("收費記錄不存在");
        if (record.Status != PaymentStatus.Pending)
            throw new InvalidOperationException("僅待付款記錄可免除");
        record.Status = PaymentStatus.Waived;
        record.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("waive", "payment_record", id, "免除收費");
        return await GetPaymentByIdAsync(id);
    }

    public async Task<List<PaymentRecordResponseDto>> GetPendingAsync()
    {
        return await _db.PaymentRecords
            .Include(r => r.ClientProfile)
            .Include(r => r.Discount)
            .Where(r => r.Status == PaymentStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .Select(r => MapPaymentToDto(r))
            .ToListAsync();
    }

    // 系統維護任務：跨機構掃描到期套餐，故 IgnoreQueryFilters（背景執行無 org context）
    public async Task<int> CheckExpiredPackagesAsync()
    {
        var expired = await _db.ClientPackages
            .IgnoreQueryFilters()
            .Include(cp => cp.Package)
            .Include(cp => cp.ClientProfile)
            .Where(cp => cp.Status == ClientPackageStatus.Active && cp.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();

        foreach (var cp in expired)
        {
            cp.Status = ClientPackageStatus.Expired;
        }
        await _db.SaveChangesAsync();

        // 觸發點 #7：package_expiring 通知（心理師 + 行政）
        foreach (var cp in expired)
        {
            var therapistUserId = await _db.TherapistClients
                .IgnoreQueryFilters()
                .Where(tc => tc.ClientProfileId == cp.ClientProfileId
                    && tc.Status == TherapistClientStatus.Active)
                .Select(tc => (int?)tc.TherapistUserId)
                .FirstOrDefaultAsync();

            var vars = new Dictionary<string, string>
            {
                ["client_name"] = cp.ClientProfile?.Name ?? "",
                ["package_name"] = cp.Package?.Name ?? "",
                ["expires_at"] = cp.ExpiresAt.ToString("yyyy-MM-dd"),
            };

            if (therapistUserId.HasValue)
                await _notify.SendAsync(NotificationEventType.PackageExpiring, therapistUserId.Value, "therapist", cp.OrganizationId, vars);
            foreach (var adminId in await GetAdminUserIdsAsync(cp.OrganizationId))
                await _notify.SendAsync(NotificationEventType.PackageExpiring, adminId, "admin", cp.OrganizationId, vars);
        }

        return expired.Count;
    }

    // 取得機構內具行政職能的收件者（admin / owner），供套餐相關通知使用
    private async Task<List<int>> GetAdminUserIdsAsync(int orgId)
    {
        return await _db.UserRoles
            .IgnoreQueryFilters()
            .Where(ur => ur.User.OrganizationId == orgId
                && (ur.Role.Name == "admin" || ur.Role.Name == "owner"))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync();
    }

    private async Task<PaymentRecordResponseDto> GetPaymentByIdAsync(int id)
    {
        var r = await _db.PaymentRecords
            .Include(r => r.ClientProfile)
            .Include(r => r.Discount)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException("收費記錄不存在");
        return MapPaymentToDto(r);
    }

    private async Task<ClientPackageResponseDto> GetClientPackageByIdAsync(int id)
    {
        var cp = await _db.ClientPackages
            .Include(c => c.ClientProfile)
            .Include(c => c.Package)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("個案套餐不存在");
        return new ClientPackageResponseDto
        {
            Id = cp.Id, ClientProfileId = cp.ClientProfileId, ClientName = cp.ClientProfile.Name,
            PackageId = cp.PackageId, PackageName = cp.Package.Name,
            SessionsUsed = cp.SessionsUsed, SessionsRemaining = cp.SessionsRemaining,
            Status = cp.Status, PurchasedAt = cp.PurchasedAt, ExpiresAt = cp.ExpiresAt,
        };
    }

    private static PaymentRecordResponseDto MapPaymentToDto(PaymentRecord r) => new()
    {
        Id = r.Id, ClientProfileId = r.ClientProfileId, ClientName = r.ClientProfile?.Name ?? "",
        AppointmentId = r.AppointmentId, ClientPackageId = r.ClientPackageId,
        DiscountId = r.DiscountId, DiscountName = r.Discount?.Name,
        OriginalAmount = r.OriginalAmount, FinalAmount = r.FinalAmount,
        PaymentMethod = r.PaymentMethod, PaymentType = r.PaymentType,
        Status = r.Status, Note = r.Note, PaidAt = r.PaidAt, CreatedAt = r.CreatedAt,
    };
}
