using System.Text;
using System.Threading.RateLimiting;
using consulcore_backend.Authorization;
using consulcore_backend.Data;
using consulcore_backend.Data.Encryption;
using consulcore_backend.Middleware;
using consulcore_backend.Models;
using consulcore_backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

// QuestPDF 社群授權（免費用於年營收 < 100 萬美元的組織 / 個人）
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// === Services ===

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // 模型驗證失敗也回統一的 ApiResponse（預設是 ValidationProblemDetails，與契約不符）
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors.Select(err =>
                    string.IsNullOrEmpty(err.ErrorMessage) ? $"{e.Key} 格式不正確" : err.ErrorMessage))
                .ToList();
            return new BadRequestObjectResult(ApiResponse<object>.Fail($"輸入資料有誤：{errors.FirstOrDefault()}", errors));
        };
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Encryption
builder.Services.AddSingleton<IEncryptionService, AesEncryptionService>();

// DbContext — provider 由 Database:Provider 決定，見 docs/database.md
var connectionString = DatabaseProvider.ResolveConnectionString(
    builder.Configuration["Database:Provider"],
    builder.Configuration.GetConnectionString("DefaultConnection"),
    builder.Environment.ContentRootPath);
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString))
    {
        options.UseSqlite(connectionString);
    }
});

// Services
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<RolePermissionService>();
builder.Services.AddScoped<ClientService>();
builder.Services.AddScoped<ConsentService>();
builder.Services.AddScoped<RoomService>();
builder.Services.AddScoped<ScheduleService>();
builder.Services.AddScoped<AppointmentService>();
builder.Services.AddScoped<AppointmentSeriesService>();
builder.Services.AddScoped<FormService>();
builder.Services.AddScoped<SessionNoteService>();
builder.Services.AddScoped<PhraseService>();
builder.Services.AddScoped<ClientMemoService>();
builder.Services.AddScoped<FeeScheduleService>();
builder.Services.AddScoped<PackageService>();
builder.Services.AddScoped<DiscountService>();
builder.Services.AddScoped<BillingService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<PerformanceService>();
builder.Services.AddScoped<ViolationService>();
builder.Services.AddScoped<AuditLogService>();

// 背景排程：套餐到期、考核可見性
builder.Services.AddHostedService<ScheduledMaintenanceService>();

// 登入限流：每個 IP 每分鐘最多 N 次（預設 10），超過回 429，降低暴力猜密碼的速度
var loginPermitLimit = builder.Configuration.GetValue("RateLimiting:Login:PermitLimit", 10);
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(RateLimitPolicies.Login, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = loginPermitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            ApiResponse<object>.Fail("登入嘗試過於頻繁，請稍後再試"), cancellationToken);
    };
});

// JWT Authentication
var jwtSecretKey = JwtSecretKey.Validate(
    builder.Configuration["Jwt:SecretKey"], builder.Environment.EnvironmentName);
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ConsulCore";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        // token 有效期內帳號或機構被停用時立即失效（否則停用後 24 小時內仍可使用）。
        // 驗證失敗回空內容的 401，前端據此視為登入過期。
        OnTokenValidated = async context =>
        {
            var userIdClaim = context.Principal?.FindFirst(JwtClaims.UserId)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                context.Fail("token 缺少 userId");
                return;
            }

            // 用獨立 scope：AppDbContext 在建構時就從 HttpContext.User 讀機構 id，此時 User 尚未設定；
            // 若取用請求的 scoped 實例，整個請求的多租戶 filter 都會變成「機構 = null」而查不到任何資料
            using var scope = context.HttpContext.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var active = await db.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Id == userId && u.IsActive && u.Organization.IsActive);
            if (!active)
                context.Fail("帳號或機構已停用");
        },
    };
});

// Authorization Policies
builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

builder.Services.AddAuthorization(options =>
{
    foreach (var (policy, permission) in Policies.PermissionByPolicy)
    {
        options.AddPolicy(policy, p => p.Requirements.Add(new PermissionRequirement(permission)));
    }
});

// CORS
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// === Database Migration + Seed ===
if (!string.IsNullOrEmpty(connectionString))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(db);
}

// === Middleware Pipeline ===

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.UseRateLimiter();

app.UseAuthentication();

// 首次登入強制改密碼：需在 Authentication 之後（User 已填入）、Authorization 之前
app.UseMiddleware<MustChangePasswordMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
