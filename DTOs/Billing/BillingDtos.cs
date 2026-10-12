using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.Billing;

public class PurchasePackageDto
{
    public int ClientProfileId { get; set; }
    public int PackageId { get; set; }
    [ConstantValues(typeof(PaymentMethods))]
    public string PaymentMethod { get; set; } = "cash";
}

public class ConfirmPaymentDto
{
    [ConstantValues(typeof(PaymentMethods))]
    public string PaymentMethod { get; set; } = "cash";
}

public class RefundDto
{
    public string? Note { get; set; }
}

public class PaymentRecordResponseDto
{
    public int Id { get; set; }
    public int ClientProfileId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int? AppointmentId { get; set; }
    public int? ClientPackageId { get; set; }
    public int? DiscountId { get; set; }
    public string? DiscountName { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal FinalAmount { get; set; }
    [ConstantValues(typeof(PaymentMethods))]
    public string PaymentMethod { get; set; } = string.Empty;
    [ConstantValues(typeof(PaymentTypes))]
    public string PaymentType { get; set; } = string.Empty;
    [ConstantValues(typeof(PaymentStatus))]
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ClientPackageResponseDto
{
    public int Id { get; set; }
    public int ClientProfileId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public int SessionsUsed { get; set; }
    public int SessionsRemaining { get; set; }
    [ConstantValues(typeof(ClientPackageStatus))]
    public string Status { get; set; } = string.Empty;
    public DateTime PurchasedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
