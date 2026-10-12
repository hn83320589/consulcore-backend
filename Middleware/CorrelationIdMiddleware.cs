using System.Text.RegularExpressions;

namespace consulcore_backend.Middleware;

/// <summary>
/// 每個請求一個追蹤碼：沿用呼叫端送來的 X-Correlation-Id（格式不合就重新產生，避免把任意字串寫進 log），
/// 寫入 HttpContext.TraceIdentifier、回應 header 與 log scope。500 錯誤訊息會附上它，使用者回報時可直接對到 log。
/// </summary>
public partial class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        var id = ValidId().IsMatch(incoming) ? incoming : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = id;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = id;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope("CorrelationId:{CorrelationId}", id))
        {
            await _next(context);
        }
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{1,64}$")]
    private static partial Regex ValidId();
}
