namespace consulcore_backend.Services;

/// <summary>
/// 每日背景維護：套餐到期轉 expired、考核可見性排程（開放/收回）、常態預約續約提醒。
/// 每天診所時間 03:00 執行（各任務皆可重複執行，續約提醒已去重）。
/// 這些是跨機構的系統任務，內部方法以 IgnoreQueryFilters 掃全機構。
/// </summary>
public class ScheduledMaintenanceService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledMaintenanceService> _logger;
    private readonly ClinicClock _clock;
    private static readonly TimeOnly RunAt = new(3, 0);

    public ScheduledMaintenanceService(
        IServiceScopeFactory scopeFactory,
        ILogger<ScheduledMaintenanceService> logger,
        ClinicClock clock)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _clock = clock;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_clock.UntilNext(RunAt), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // 背景任務失敗不可讓服務崩潰，記錄後於下一輪重試
                _logger.LogError(ex, "排程維護任務執行失敗");
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var billing = scope.ServiceProvider.GetRequiredService<BillingService>();
        var expiredPackages = await billing.CheckExpiredPackagesAsync();

        var performance = scope.ServiceProvider.GetRequiredService<PerformanceService>();
        var visibilityChanged = await performance.CheckVisibilityScheduleAsync();

        var series = scope.ServiceProvider.GetRequiredService<AppointmentSeriesService>();
        var renewalNotices = await series.NotifyPendingRenewalsAsync();

        _logger.LogInformation(
            "排程維護完成：套餐到期 {Expired} 筆、考核可見性異動 {Visibility} 筆、續約提醒 {Renewal} 筆",
            expiredPackages, visibilityChanged, renewalNotices);
    }
}
