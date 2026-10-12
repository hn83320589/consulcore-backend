using System.Net;
using System.Text.Json;
using consulcore_backend.Exceptions;
using consulcore_backend.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace ConsulCore.Tests.Unit;

// B5：只有刻意丟出的業務例外才把訊息回給前端；框架 / EF 的例外不可外洩內部細節
public class ErrorHandlingMiddlewareTests
{
    private sealed class Env(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "test";
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static async Task<(int status, JsonElement body)> InvokeAsync(
        Exception ex, string environment = "Production", string? traceId = null)
    {
        var middleware = new ErrorHandlingMiddleware(_ => throw ex, new Env(environment),
            NullLogger<ErrorHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext();
        if (traceId != null) context.TraceIdentifier = traceId;
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await JsonDocument.ParseAsync(context.Response.Body);
        return (context.Response.StatusCode, body.RootElement.Clone());
    }

    [Fact]
    public async Task Business_rule_violation_is_a_400_with_its_message()
    {
        var (status, body) = await InvokeAsync(new BusinessRuleException("此時段已被預約或不可用"));

        Assert.Equal((int)HttpStatusCode.BadRequest, status);
        Assert.Equal("此時段已被預約或不可用", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Forbidden_is_a_403_with_its_message()
    {
        var (status, body) = await InvokeAsync(new ForbiddenException("僅建立者可編輯此晤談紀錄"));

        Assert.Equal((int)HttpStatusCode.Forbidden, status);
        Assert.Equal("僅建立者可編輯此晤談紀錄", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Framework_invalid_operation_is_a_500_without_internal_details()
    {
        var (status, body) = await InvokeAsync(new InvalidOperationException(
            "The instance of entity type 'User' cannot be tracked because another instance with the key value '{Id: 3}' is already being tracked."));

        Assert.Equal((int)HttpStatusCode.InternalServerError, status);
        Assert.DoesNotContain("tracked", body.GetProperty("message").GetString());
        Assert.False(body.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task Framework_argument_exception_is_not_exposed()
    {
        var (status, body) = await InvokeAsync(new ArgumentNullException("connectionString"));

        Assert.Equal((int)HttpStatusCode.InternalServerError, status);
        Assert.DoesNotContain("connectionString", body.GetProperty("message").GetString());
    }

    // 使用者回報「伺服器發生錯誤」時，追蹤碼可以直接對到那一筆 log
    [Fact]
    public async Task Unexpected_error_message_includes_the_correlation_id()
    {
        var (_, body) = await InvokeAsync(new InvalidOperationException("boom"), traceId: "req-7f3a");

        Assert.Contains("req-7f3a", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Business_error_message_is_unchanged()
    {
        var (_, body) = await InvokeAsync(new BusinessRuleException("此時段已被預約"), traceId: "req-7f3a");

        Assert.Equal("此時段已被預約", body.GetProperty("message").GetString());
    }
}
