using System.Security.Cryptography;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConsulCore.Tests.Integration;

/// <summary>
/// 以暫存 SQLite 檔啟動整個 API：走真實的 provider 解析 → migrate → seed 流程，
/// 不替換 DbContext 註冊。每個測試類別一份資料庫（IClassFixture），彼此隔離。
/// </summary>
public class ConsulCoreApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"consulcore-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 不用 Development：避免讀到開發者本機的 appsettings.Development.json
        builder.UseEnvironment("Testing");

        // UseSetting 在 Program.cs 讀設定之前就生效（ConfigureAppConfiguration 太晚）
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}");
        builder.UseSetting("Encryption:Key", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        builder.UseSetting("Jwt:SecretKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
        // 一般測試會大量登入，放寬限流；限流本身由 LoginSecurityTests 以較低上限驗證
        builder.UseSetting("RateLimiting:Login:PermitLimit", "10000");

        builder.ConfigureTestServices(services =>
        {
            // 背景維護排程啟動即執行一次，會在測試期間寫入資料，結果不可預期
            var hosted = services.Single(d => d.ImplementationType == typeof(ScheduledMaintenanceService));
            services.Remove(hosted);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            File.Delete(_dbPath + suffix);
        }
    }
}
