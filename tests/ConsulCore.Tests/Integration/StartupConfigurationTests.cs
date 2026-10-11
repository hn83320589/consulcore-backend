using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ConsulCore.Tests.Integration;

// B3：確認金鑰檢查有接在啟動流程上（不只是單元測試裡的函式）
public class StartupConfigurationTests
{
    private const string Placeholder = "DEV-ONLY-CHANGE-ME-IN-PRODUCTION-AT-LEAST-32-CHARS!!";

    [Fact]
    public void Production_refuses_to_start_with_the_placeholder_jwt_key()
    {
        using var factory = new ConsulCoreApiFactory().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("Jwt:SecretKey", Placeholder);
        });

        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains("Jwt__SecretKey", ex.ToString());
    }
}
