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
    /// <summary>質性考核；非 Owner 檢視時為 null（BR-18）</summary>
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
    /// <summary>預約完成率（%）：已完成 ÷（已完成 + 未出席）；已排定、已取消不計</summary>
    public decimal AppointmentCompletionRate { get; set; }
    /// <summary>未到率（%）：未出席 ÷（已完成 + 未出席）；已排定、已取消不計</summary>
    public decimal NoShowRate { get; set; }
    /// <summary>紀錄及時性：晤談後平均多久完成紀錄（小時）</summary>
    public decimal AvgNoteDelayHours { get; set; }
    /// <summary>紀錄鎖定率（%）</summary>
    public decimal NoteLockRate { get; set; }
    /// <summary>個案留存率（%）</summary>
    public decimal ClientRetentionRate { get; set; }
    /// <summary>月諮商次數</summary>
    public int MonthlySessionCount { get; set; }
    /// <summary>月諮商時數</summary>
    public decimal MonthlySessionHours { get; set; }
    /// <summary>收費完成率（%）</summary>
    public decimal PaymentCompletionRate { get; set; }
}

public class MetricsTrendDto
{
    /// <summary>指標鍵，同 TherapistMetricsDto 的欄位名（如 appointmentCompletionRate）</summary>
    public string Metric { get; set; } = string.Empty;
    public List<MetricPointDto> Points { get; set; } = [];
}

public class MetricPointDto
{
    /// <summary>"yyyy-MM"</summary>
    public string Period { get; set; } = string.Empty;
    public decimal Value { get; set; }
}
