using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using consulcore_backend.Data.Encryption;

namespace consulcore_backend.Models;

[Table("audit_logs")]
public class AuditLog
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("organization_id")]
    public int OrganizationId { get; set; }

    /// <summary>null = 系統動作（背景排程），沒有登入的操作者</summary>
    [Column("user_id")]
    public int? UserId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("action")]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("resource_type")]
    public string ResourceType { get; set; } = string.Empty;

    [Column("resource_id")]
    public int ResourceId { get; set; }

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    [Encrypted]
    [Column("changes")]
    public string? Changes { get; set; }

    [MaxLength(50)]
    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("OrganizationId")]
    public Organization Organization { get; set; } = null!;

    [ForeignKey("UserId")]
    public User? User { get; set; }

    /// <summary>系統動作在查詢與匯出時顯示的操作者名稱</summary>
    public const string SystemActorName = "系統";
}
