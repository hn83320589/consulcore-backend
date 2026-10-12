using System.ComponentModel.DataAnnotations;
using consulcore_backend.Models;
using consulcore_backend.OpenApi;

namespace consulcore_backend.DTOs.AppointmentSeries;

public class CreateAppointmentSeriesDto
{
    [Required]
    public int ClientProfileId { get; set; }

    [Required]
    public int TherapistUserId { get; set; }

    [Required]
    [Range(0, 6)]
    public int DayOfWeek { get; set; }

    [Required]
    public string StartTime { get; set; } = string.Empty; // "10:00"

    [MaxLength(20)]
    [ConstantValues(typeof(SeriesFrequency))]
    public string Frequency { get; set; } = "weekly";

    [Range(1, 3)]
    public int Months { get; set; } = 1; // BR-07: 上限 3 個月
}
