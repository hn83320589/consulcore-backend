using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.AppointmentSeries;

public class AppointmentSeriesResponseDto
{
    public int Id { get; set; }
    public int ClientProfileId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int TherapistUserId { get; set; }
    public string TherapistName { get; set; } = string.Empty;
    public int DayOfWeek { get; set; }
    public string StartTime { get; set; } = string.Empty;
    [ConstantValues(typeof(SeriesFrequency))]
    public string Frequency { get; set; } = string.Empty;
    [ConstantValues(typeof(SeriesStatus))]
    public string Status { get; set; } = string.Empty;
    public string ExpiresAt { get; set; } = string.Empty;
    public int TotalAppointments { get; set; }
    public int RemainingAppointments { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RenewSeriesDto
{
    [System.ComponentModel.DataAnnotations.Range(1, 3)]
    public int Months { get; set; } = 1;
}
