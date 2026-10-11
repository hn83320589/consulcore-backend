namespace consulcore_backend.Services;

/// <summary>
/// 業務規則用的時間來源。包住 TimeProvider（測試可替換），並依診所時區計算「今天」
/// （設定 Clinic:TimeZone，預設 Asia/Taipei）。只用來寫入 CreatedAt / UpdatedAt 的時間戳不需要經過這裡。
/// </summary>
public class ClinicClock(TimeProvider timeProvider, IConfiguration configuration)
{
    private readonly TimeZoneInfo _timeZone =
        TimeZoneInfo.FindSystemTimeZoneById(configuration["Clinic:TimeZone"] ?? "Asia/Taipei");

    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(UtcNow, _timeZone));
}
