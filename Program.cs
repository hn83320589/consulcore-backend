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
});

// Authorization Policies
builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

builder.Services.AddAuthorization(options =>
{
    // 個案
    options.AddPolicy("CanViewClients", p => p.Requirements.Add(new PermissionRequirement("client.view")));
    options.AddPolicy("CanCreateClients", p => p.Requirements.Add(new PermissionRequirement("client.create")));
    options.AddPolicy("CanUpdateClients", p => p.Requirements.Add(new PermissionRequirement("client.update")));
    options.AddPolicy("CanDeleteClients", p => p.Requirements.Add(new PermissionRequirement("client.delete")));
    options.AddPolicy("CanManageClients", p => p.Requirements.Add(new PermissionRequirement("client.manage")));

    // 預約
    options.AddPolicy("CanViewAppointments", p => p.Requirements.Add(new PermissionRequirement("appointment.view")));
    options.AddPolicy("CanCreateAppointments", p => p.Requirements.Add(new PermissionRequirement("appointment.create")));
    options.AddPolicy("CanUpdateAppointments", p => p.Requirements.Add(new PermissionRequirement("appointment.update")));
    options.AddPolicy("CanDeleteAppointments", p => p.Requirements.Add(new PermissionRequirement("appointment.delete")));

    // 排班
    options.AddPolicy("CanViewSchedules", p => p.Requirements.Add(new PermissionRequirement("schedule.view")));
    options.AddPolicy("CanCreateSchedules", p => p.Requirements.Add(new PermissionRequirement("schedule.create")));
    options.AddPolicy("CanUpdateSchedules", p => p.Requirements.Add(new PermissionRequirement("schedule.update")));
    options.AddPolicy("CanDeleteSchedules", p => p.Requirements.Add(new PermissionRequirement("schedule.delete")));

    // 晤談
    options.AddPolicy("CanViewSessions", p => p.Requirements.Add(new PermissionRequirement("session.view")));
    options.AddPolicy("CanCreateSessions", p => p.Requirements.Add(new PermissionRequirement("session.create")));
    options.AddPolicy("CanUpdateSessions", p => p.Requirements.Add(new PermissionRequirement("session.update")));
    options.AddPolicy("CanLockSessions", p => p.Requirements.Add(new PermissionRequirement("session.lock")));

    // 同意書
    options.AddPolicy("CanViewConsents", p => p.Requirements.Add(new PermissionRequirement("consent.view")));
    options.AddPolicy("CanCreateConsents", p => p.Requirements.Add(new PermissionRequirement("consent.create")));
    options.AddPolicy("CanUpdateConsents", p => p.Requirements.Add(new PermissionRequirement("consent.update")));
    options.AddPolicy("CanVoidConsents", p => p.Requirements.Add(new PermissionRequirement("consent.void")));

    // 收費
    options.AddPolicy("CanViewBilling", p => p.Requirements.Add(new PermissionRequirement("billing.view")));
    options.AddPolicy("CanCreateBilling", p => p.Requirements.Add(new PermissionRequirement("billing.create")));
    options.AddPolicy("CanUpdateBilling", p => p.Requirements.Add(new PermissionRequirement("billing.update")));
    options.AddPolicy("CanRefundBilling", p => p.Requirements.Add(new PermissionRequirement("billing.refund")));

    // 管理
    options.AddPolicy("CanManageNotifications", p => p.Requirements.Add(new PermissionRequirement("notification.manage")));
    options.AddPolicy("CanManageStaff", p => p.Requirements.Add(new PermissionRequirement("staff.manage")));
    options.AddPolicy("CanManageRooms", p => p.Requirements.Add(new PermissionRequirement("room.manage")));
    options.AddPolicy("CanViewAudit", p => p.Requirements.Add(new PermissionRequirement("audit.view")));
    options.AddPolicy("CanViewPerformance", p => p.Requirements.Add(new PermissionRequirement("performance.view")));
    options.AddPolicy("CanManagePerformance", p => p.Requirements.Add(new PermissionRequirement("performance.manage")));
    options.AddPolicy("CanManageOrgSettings", p => p.Requirements.Add(new PermissionRequirement("org.settings")));
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
