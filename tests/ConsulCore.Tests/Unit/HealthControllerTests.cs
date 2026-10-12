using System.Security.Cryptography;
using consulcore_backend.Controllers;
using consulcore_backend.Data;
using consulcore_backend.Data.Encryption;
using consulcore_backend.DTOs.Health;
using consulcore_backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ConsulCore.Tests.Unit;

public class HealthControllerTests
{
    [Fact]
    public async Task Unreachable_database_returns_503()
    {
        // 不存在的資料夾：SQLite 無法建立檔案，連線必定失敗
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing", "x.db")}")
            .Options;
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["Encryption:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) }).Build();
        await using var db = new AppDbContext(options, new HttpContextAccessor(), new AesEncryptionService(config));

        var result = await new HealthController(db).Get();

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
        var body = Assert.IsType<ApiResponse<HealthStatusDto>>(objectResult.Value);
        Assert.Equal("unhealthy", body.Data!.Status);
        Assert.Equal("unreachable", body.Data.Database);
    }
}
