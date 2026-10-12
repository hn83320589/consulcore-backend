using System.ComponentModel.DataAnnotations;
using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.PerformanceReview;

public class CreatePerformanceReviewDto
{
    [Required] public int TherapistUserId { get; set; }
    [Required] [MaxLength(50)] public string Period { get; set; } = string.Empty;
    [Range(1, 5)] public int ProfessionalScore { get; set; }
    [Range(1, 5)] public int ClientFeedbackScore { get; set; }
    [Range(1, 5)] public int TeamworkScore { get; set; }
    [Range(1, 5)] public int OverallScore { get; set; }
    public string? Comment { get; set; }
}

public class UpdatePerformanceReviewDto
{
    [Range(1, 5)] public int? ProfessionalScore { get; set; }
    [Range(1, 5)] public int? ClientFeedbackScore { get; set; }
    [Range(1, 5)] public int? TeamworkScore { get; set; }
    [Range(1, 5)] public int? OverallScore { get; set; }
    public string? Comment { get; set; }
}

public class SetVisibilityDto
{
    [ConstantValues(typeof(ReviewVisibility))]
    [Required] public string Visibility { get; set; } = "hidden";
    public DateTime? VisibleFrom { get; set; }
    public DateTime? VisibleUntil { get; set; }
}

public class PerformanceReviewResponseDto
{
    public int Id { get; set; }
    public int TherapistUserId { get; set; }
    public string TherapistName { get; set; } = string.Empty;
    public int ReviewerId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public int ProfessionalScore { get; set; }
    public int ClientFeedbackScore { get; set; }
    public int TeamworkScore { get; set; }
    public int OverallScore { get; set; }
    public string? Comment { get; set; }
    [ConstantValues(typeof(ReviewVisibility))]
    public string Visibility { get; set; } = string.Empty;
    public DateTime? VisibleFrom { get; set; }
    public DateTime? VisibleUntil { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TherapistMetricsDto
{
    public int TherapistUserId { get; set; }
    public string TherapistName { get; set; } = string.Empty;
    public decimal AppointmentCompletionRate { get; set; }
    public decimal NoShowRate { get; set; }
    public decimal AvgNoteDelayHours { get; set; }
    public decimal NoteLockRate { get; set; }
    public decimal ClientRetentionRate { get; set; }
    public int MonthlySessionCount { get; set; }
    public decimal MonthlySessionHours { get; set; }
    public decimal PaymentCompletionRate { get; set; }
}

public class MetricsTrendDto
{
    public string Metric { get; set; } = string.Empty;
    public List<MetricPointDto> Points { get; set; } = [];
}

public class MetricPointDto
{
    public string Period { get; set; } = string.Empty; // "yyyy-MM"
    public decimal Value { get; set; }
}
