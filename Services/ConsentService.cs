using consulcore_backend.Data;
using consulcore_backend.DTOs.ConsentRecord;
using consulcore_backend.DTOs.ConsentTemplate;
using consulcore_backend.Exceptions;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Services;

public class ConsentService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public ConsentService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    // === Template CRUD ===

    public async Task<List<ConsentTemplateResponseDto>> GetAllTemplatesAsync()
    {
        return await _db.ConsentTemplates
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => MapTemplateToDto(t))
            .ToListAsync();
    }

    public async Task<ConsentTemplateResponseDto> GetTemplateByIdAsync(int id)
    {
        var template = await _db.ConsentTemplates.FindAsync(id)
            ?? throw new KeyNotFoundException("同意書範本不存在");
        return MapTemplateToDto(template);
    }

    public async Task<ConsentTemplateResponseDto> CreateTemplateAsync(CreateConsentTemplateDto dto, int orgId)
    {
        var template = new ConsentTemplate
        {
            OrganizationId = orgId,
            Title = dto.Title,
            Content = dto.Content,
            TriggerEvent = dto.TriggerEvent,
        };

        _db.ConsentTemplates.Add(template);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("create", "consent_template", template.Id, $"建立同意書範本「{template.Title}」");

        return MapTemplateToDto(template);
    }

    public async Task<ConsentTemplateResponseDto> UpdateTemplateAsync(int id, UpdateConsentTemplateDto dto)
    {
        var template = await _db.ConsentTemplates.FindAsync(id)
            ?? throw new KeyNotFoundException("同意書範本不存在");

        if (dto.Title != null) template.Title = dto.Title;
        if (dto.TriggerEvent != null) template.TriggerEvent = dto.TriggerEvent;
        if (dto.IsActive.HasValue) template.IsActive = dto.IsActive.Value;

        // 更新 content 時 version++
        if (dto.Content != null)
        {
            template.Content = dto.Content;
            template.Version++;
        }

        template.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync("update", "consent_template", id,
            $"更新同意書範本「{template.Title}」(v{template.Version})");

        return MapTemplateToDto(template);
    }

    public async Task DeleteTemplateAsync(int id)
    {
        var template = await _db.ConsentTemplates.FindAsync(id)
            ?? throw new KeyNotFoundException("同意書範本不存在");

        // 檢查是否有已簽署的記錄
        var hasRecords = await _db.ConsentRecords
            .AnyAsync(r => r.ConsentTemplateId == id && r.Status == ConsentRecordStatus.Signed);
        if (hasRecords)
            throw new BusinessRuleException("此範本有已簽署的記錄，無法刪除，請改為停用");

        _db.ConsentTemplates.Remove(template);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("delete", "consent_template", id, $"刪除同意書範本「{template.Title}」");
    }

    // === Consent Records ===

    public async Task<ConsentRecordResponseDto> SignConsentAsync(SignConsentDto dto, int orgId)
    {
        var template = await _db.ConsentTemplates.FindAsync(dto.ConsentTemplateId)
            ?? throw new KeyNotFoundException("同意書範本不存在");

        if (!template.IsActive)
            throw new BusinessRuleException("此同意書範本已停用");

        var client = await _db.ClientProfiles.FirstOrDefaultAsync(c => c.Id == dto.ClientProfileId && !c.IsDeleted)
            ?? throw new KeyNotFoundException("個案不存在");

        // BR-14: 保存內容快照與版本號
        var record = new ConsentRecord
        {
            OrganizationId = orgId,
            ConsentTemplateId = template.Id,
            ClientProfileId = client.Id,
            ContentSnapshot = template.Content,
            TemplateVersion = template.Version,
            SignatureImage = dto.SignatureImage,
            SignedRemotely = dto.SignedRemotely,
            Status = ConsentRecordStatus.Signed,
            SignedAt = DateTime.UtcNow,
        };

        _db.ConsentRecords.Add(record);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("sign", "consent_record", record.Id,
            $"個案 (id={client.Id}) 簽署同意書「{template.Title}」v{template.Version}");

        return await GetRecordByIdAsync(record.Id);
    }

    public async Task<ConsentRecordResponseDto> VoidConsentAsync(int recordId, VoidConsentDto dto)
    {
        var record = await _db.ConsentRecords
            .Include(r => r.ConsentTemplate)
            .FirstOrDefaultAsync(r => r.Id == recordId)
            ?? throw new KeyNotFoundException("同意書記錄不存在");

        if (record.Status != ConsentRecordStatus.Signed)
            throw new BusinessRuleException("只有已簽署的同意書可以作廢");

        record.Status = ConsentRecordStatus.Voided;
        record.VoidedAt = DateTime.UtcNow;
        record.VoidReason = dto.Reason;

        await _db.SaveChangesAsync();

        await _audit.LogAsync("void", "consent_record", recordId,
            $"作廢同意書記錄 (id={recordId}), 原因: {dto.Reason}");

        return await GetRecordByIdAsync(recordId);
    }

    public async Task<List<ConsentRecordResponseDto>> GetRecordsByClientAsync(int clientProfileId)
    {
        return await _db.ConsentRecords
            .Include(r => r.ConsentTemplate)
            .Include(r => r.ClientProfile)
            .Where(r => r.ClientProfileId == clientProfileId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => MapRecordToDto(r))
            .ToListAsync();
    }

    public async Task<List<ConsentRecordResponseDto>> GetRecordsByTriggerEventAsync(string triggerEvent)
    {
        return await _db.ConsentRecords
            .Include(r => r.ConsentTemplate)
            .Include(r => r.ClientProfile)
            .Where(r => r.ConsentTemplate.TriggerEvent == triggerEvent)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => MapRecordToDto(r))
            .ToListAsync();
    }

    // 簽署紀錄總表（含篩選）
    public async Task<List<ConsentRecordResponseDto>> GetRecordsAsync(
        int? clientProfileId, string? status, string? triggerEvent)
    {
        var query = _db.ConsentRecords
            .Include(r => r.ConsentTemplate)
            .Include(r => r.ClientProfile)
            .AsQueryable();

        if (clientProfileId.HasValue) query = query.Where(r => r.ClientProfileId == clientProfileId.Value);
        if (!string.IsNullOrEmpty(status)) query = query.Where(r => r.Status == status);
        if (!string.IsNullOrEmpty(triggerEvent)) query = query.Where(r => r.ConsentTemplate.TriggerEvent == triggerEvent);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => MapRecordToDto(r))
            .ToListAsync();
    }

    private async Task<ConsentRecordResponseDto> GetRecordByIdAsync(int id)
    {
        var record = await _db.ConsentRecords
            .Include(r => r.ConsentTemplate)
            .Include(r => r.ClientProfile)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException("同意書記錄不存在");

        return MapRecordToDto(record);
    }

    private static ConsentTemplateResponseDto MapTemplateToDto(ConsentTemplate t)
    {
        return new ConsentTemplateResponseDto
        {
            Id = t.Id,
            Title = t.Title,
            Content = t.Content,
            Version = t.Version,
            TriggerEvent = t.TriggerEvent,
            IsActive = t.IsActive,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
        };
    }

    private static ConsentRecordResponseDto MapRecordToDto(ConsentRecord r)
    {
        return new ConsentRecordResponseDto
        {
            Id = r.Id,
            ConsentTemplateId = r.ConsentTemplateId,
            TemplateTitle = r.ConsentTemplate?.Title ?? "",
            ClientProfileId = r.ClientProfileId,
            ClientName = r.ClientProfile?.Name ?? "",
            ContentSnapshot = r.ContentSnapshot,
            TemplateVersion = r.TemplateVersion,
            SignatureImage = r.SignatureImage,
            SignedRemotely = r.SignedRemotely,
            Status = r.Status,
            SignedAt = r.SignedAt,
            VoidedAt = r.VoidedAt,
            VoidReason = r.VoidReason,
            CreatedAt = r.CreatedAt,
        };
    }
}
