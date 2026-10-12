using System.Net;
using System.Text.Json;
using consulcore_backend.Exceptions;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _env;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        IHostEnvironment env,
        ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _env = env;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "未處理的例外");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        // 依例外型別對應 HTTP 狀態碼；業務例外的訊息可安全回傳給前端
        var (statusCode, exposeMessage) = exception switch
        {
            ForbiddenException => (HttpStatusCode.Forbidden, true),
            DbUpdateConcurrencyException => (HttpStatusCode.Conflict, true),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, true),
            KeyNotFoundException => (HttpStatusCode.NotFound, true),
            BusinessRuleException => (HttpStatusCode.BadRequest, true),
            _ => (HttpStatusCode.InternalServerError, false),
        };
        context.Response.StatusCode = (int)statusCode;

        // 已知業務例外一律回傳其訊息；未知例外在生產環境隱藏細節
        var message = exception switch
        {
            // EF 的併發訊息是英文技術細節，改為使用者看得懂的說明
            DbUpdateConcurrencyException => "資料剛被其他人更新（例如此時段剛被預約），請重新整理後再試",
            _ when exposeMessage || _env.IsDevelopment() => exception.Message,
            _ => $"伺服器發生錯誤，請稍後再試（追蹤碼：{context.TraceIdentifier}）",
        };

        // 僅在未預期的 500 且開發環境時附上堆疊
        var errors = (!exposeMessage && _env.IsDevelopment())
            ? new List<string> { exception.StackTrace ?? string.Empty }
            : null;

        var response = ApiResponse<object>.Fail(message, errors);

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
