namespace consulcore_backend.DTOs.Health;

public class HealthStatusDto
{
    /// <summary>healthy / unhealthy</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>ok / unreachable</summary>
    public string Database { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }
}
