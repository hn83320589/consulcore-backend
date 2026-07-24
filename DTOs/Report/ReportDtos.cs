namespace consulcore_backend.DTOs.Report;

public class MonthlyRevenueDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalTransactions { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal WaivedAmount { get; set; }
}

public class TherapistRevenueDto
{
    public int TherapistUserId { get; set; }
    public string TherapistName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public int TotalSessions { get; set; }
}

public class PackageStatsDto
{
    public int ActivePackages { get; set; }
    public int ExpiredPackages { get; set; }
    public int CompletedPackages { get; set; }
    public decimal TotalPackageRevenue { get; set; }
    public List<PackageRemainingDto> LowRemaining { get; set; } = [];
}

public class PackageRemainingDto
{
    public int ClientPackageId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public int SessionsRemaining { get; set; }
    public DateTime ExpiresAt { get; set; }
}
