using System.Text;
using consulcore_backend.Data;
using consulcore_backend.DTOs.AuditLog;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace consulcore_backend.Services;

public class AuditLogService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public AuditLogService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<List<AuditLogResponseDto>> GetLogsAsync(
        string? action, string? resourceType, int? userId,
        DateTime? startDate, DateTime? endDate, int page = 1, int pageSize = 50,
        int? resourceId = null)
    {
        var query = _db.AuditLogs
            .Include(a => a.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(action)) query = query.Where(a => a.Action == action);
        if (!string.IsNullOrEmpty(resourceType)) query = query.Where(a => a.ResourceType == resourceType);
        if (resourceId.HasValue) query = query.Where(a => a.ResourceId == resourceId.Value);
        if (userId.HasValue) query = query.Where(a => a.UserId == userId.Value);
        if (startDate.HasValue) query = query.Where(a => a.CreatedAt >= startDate.Value);
        if (endDate.HasValue) query = query.Where(a => a.CreatedAt <= endDate.Value);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogResponseDto
            {
                Id = a.Id, UserId = a.UserId, UserName = a.User.DisplayName,
                Action = a.Action, ResourceType = a.ResourceType, ResourceId = a.ResourceId,
                Description = a.Description, Changes = a.Changes,
                IpAddress = a.IpAddress, CreatedAt = a.CreatedAt,
            }).ToListAsync();
    }

    public async Task<string> ExportCsvAsync(
        string? action, string? resourceType, DateTime? startDate, DateTime? endDate)
    {
        var query = _db.AuditLogs
            .Include(a => a.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(action)) query = query.Where(a => a.Action == action);
        if (!string.IsNullOrEmpty(resourceType)) query = query.Where(a => a.ResourceType == resourceType);
        if (startDate.HasValue) query = query.Where(a => a.CreatedAt >= startDate.Value);
        if (endDate.HasValue) query = query.Where(a => a.CreatedAt <= endDate.Value);

        var logs = await query.OrderByDescending(a => a.CreatedAt)
            .Select(a => new { a.Id, UserName = a.User.DisplayName, a.Action, a.ResourceType, a.ResourceId, a.Description, a.IpAddress, a.CreatedAt })
            .ToListAsync();

        // 匯出內容含使用者與操作細節，匯出行為本身也要留痕（在查詢之後寫入，不會出現在這次匯出裡）
        await _audit.LogAsync("export", "audit_log", 0, $"匯出稽核紀錄 CSV（{logs.Count} 筆）",
            new { action, resourceType, startDate, endDate });

        var sb = new StringBuilder();
        sb.AppendLine(CsvWriter.Row("ID", "使用者", "操作", "資源類型", "資源ID", "描述", "IP", "時間"));
        foreach (var l in logs)
            sb.AppendLine(CsvWriter.Row(
                l.Id, l.UserName, l.Action, l.ResourceType, l.ResourceId,
                l.Description, l.IpAddress, l.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
        return sb.ToString();
    }

    public async Task<byte[]> ExportPdfAsync(
        string? action, string? resourceType, DateTime? startDate, DateTime? endDate)
    {
        var query = _db.AuditLogs
            .Include(a => a.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(action)) query = query.Where(a => a.Action == action);
        if (!string.IsNullOrEmpty(resourceType)) query = query.Where(a => a.ResourceType == resourceType);
        if (startDate.HasValue) query = query.Where(a => a.CreatedAt >= startDate.Value);
        if (endDate.HasValue) query = query.Where(a => a.CreatedAt <= endDate.Value);

        var logs = await query.OrderByDescending(a => a.CreatedAt)
            .Select(a => new { a.Id, UserName = a.User.DisplayName, a.Action, a.ResourceType, a.ResourceId, a.Description, a.CreatedAt })
            .ToListAsync();

        await _audit.LogAsync("export", "audit_log", 0, $"匯出稽核紀錄 PDF（{logs.Count} 筆）",
            new { action, resourceType, startDate, endDate });

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Text("稽核紀錄匯出").FontSize(16).SemiBold();

                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(35);   // ID
                        columns.RelativeColumn(1.5f);  // 使用者
                        columns.RelativeColumn(1);     // 操作
                        columns.RelativeColumn(1.5f);  // 資源類型
                        columns.ConstantColumn(45);    // 資源ID
                        columns.RelativeColumn(3);     // 描述
                        columns.RelativeColumn(2);     // 時間
                    });

                    table.Header(header =>
                    {
                        foreach (var h in new[] { "ID", "使用者", "操作", "資源類型", "資源ID", "描述", "時間" })
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(h).SemiBold();
                    });

                    foreach (var l in logs)
                    {
                        table.Cell().Padding(4).Text(l.Id.ToString());
                        table.Cell().Padding(4).Text(l.UserName);
                        table.Cell().Padding(4).Text(l.Action);
                        table.Cell().Padding(4).Text(l.ResourceType);
                        table.Cell().Padding(4).Text(l.ResourceId.ToString());
                        table.Cell().Padding(4).Text(l.Description);
                        table.Cell().Padding(4).Text(l.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("頁 ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
