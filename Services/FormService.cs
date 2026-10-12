using consulcore_backend.Data;
using consulcore_backend.DTOs.FormSubmission;
using consulcore_backend.DTOs.FormTemplate;
using consulcore_backend.Exceptions;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Services;

public class FormService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public FormService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    // === Template CRUD ===

    public async Task<List<FormTemplateResponseDto>> GetAllTemplatesAsync()
    {
        var templates = await _db.FormTemplates.AsNoTracking()
            .Include(t => t.Sections.OrderBy(s => s.SortOrder))
                .ThenInclude(s => s.Fields.OrderBy(f => f.SortOrder))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return templates.Select(MapTemplateToDto).ToList();
    }

    public async Task<FormTemplateResponseDto> GetTemplateByIdAsync(int id)
    {
        var template = await _db.FormTemplates.AsNoTracking()
            .Include(t => t.Sections.OrderBy(s => s.SortOrder))
                .ThenInclude(s => s.Fields.OrderBy(f => f.SortOrder))
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException("表單範本不存在");

        return MapTemplateToDto(template);
    }

    public async Task<FormTemplateResponseDto> CreateTemplateAsync(
        CreateFormTemplateDto dto, int orgId, int createdBy)
    {
        var template = new Models.FormTemplate
        {
            OrganizationId = orgId,
            Name = dto.Name,
            Type = dto.Type,
            CreatedBy = createdBy,
        };

        _db.FormTemplates.Add(template);
        await _db.SaveChangesAsync();

        await CreateSectionsAndFieldsAsync(template.Id, dto.Sections);

        await _audit.LogAsync("create", "form_template", template.Id,
            $"建立表單範本「{template.Name}」");

        return await GetTemplateByIdAsync(template.Id);
    }

    public async Task<FormTemplateResponseDto> UpdateTemplateAsync(int id, UpdateFormTemplateDto dto)
    {
        var template = await _db.FormTemplates
            .Include(t => t.Sections)
                .ThenInclude(s => s.Fields)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException("表單範本不存在");

        // 系統預設表單不可直接修改，須複製後再編輯（與刪除限制一致）
        if (template.IsSystemDefault)
            throw new BusinessRuleException("系統預設表單不可修改，請複製後再編輯");

        if (dto.Name != null) template.Name = dto.Name;
        if (dto.IsActive.HasValue) template.IsActive = dto.IsActive.Value;

        if (dto.Sections != null)
        {
            // 已有提交紀錄的範本不可硬刪 section/field，否則歷史 FormFieldValue 的 field 參照會斷裂
            var hasSubmissions = await _db.FormSubmissions.AnyAsync(fs => fs.TemplateId == id);
            if (hasSubmissions)
                throw new BusinessRuleException(
                    "此範本已有填答紀錄，不可變更結構；請複製為新範本後再編輯");

            // 移除舊的 sections/fields
            foreach (var section in template.Sections)
            {
                _db.FormFields.RemoveRange(section.Fields);
            }
            _db.FormSections.RemoveRange(template.Sections);
            await _db.SaveChangesAsync();

            await CreateSectionsAndFieldsAsync(template.Id, dto.Sections);
            template.Version++;
        }

        template.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _audit.LogAsync("update", "form_template", id,
            $"更新表單範本「{template.Name}」v{template.Version}");

        return await GetTemplateByIdAsync(id);
    }

    public async Task DeleteTemplateAsync(int id)
    {
        var template = await _db.FormTemplates
            .Include(t => t.Sections)
                .ThenInclude(s => s.Fields)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException("表單範本不存在");

        if (template.IsSystemDefault)
            throw new BusinessRuleException("系統預設表單不可刪除");

        foreach (var section in template.Sections)
            _db.FormFields.RemoveRange(section.Fields);
        _db.FormSections.RemoveRange(template.Sections);
        _db.FormTemplates.Remove(template);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("delete", "form_template", id,
            $"刪除表單範本「{template.Name}」");
    }

    public async Task<FormTemplateResponseDto> CloneTemplateAsync(int id, int orgId, int createdBy)
    {
        var source = await _db.FormTemplates
            .Include(t => t.Sections.OrderBy(s => s.SortOrder))
                .ThenInclude(s => s.Fields.OrderBy(f => f.SortOrder))
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException("表單範本不存在");

        var clone = new Models.FormTemplate
        {
            OrganizationId = orgId,
            Name = $"{source.Name} (複製)",
            Type = source.Type,
            CreatedBy = createdBy,
        };

        _db.FormTemplates.Add(clone);
        await _db.SaveChangesAsync();

        var sections = source.Sections.Select(s => new CreateFormSectionDto
        {
            Title = s.Title,
            SortOrder = s.SortOrder,
            IsRequired = s.IsRequired,
            Fields = s.Fields.Select(f => new CreateFormFieldDto
            {
                Label = f.Label,
                FieldType = f.FieldType,
                Options = f.Options,
                IsRequired = f.IsRequired,
                SortOrder = f.SortOrder,
                Placeholder = f.Placeholder,
                ValidationRules = f.ValidationRules,
            }).ToList(),
        }).ToList();

        await CreateSectionsAndFieldsAsync(clone.Id, sections);

        await _audit.LogAsync("clone", "form_template", clone.Id,
            $"複製表單範本「{source.Name}」→「{clone.Name}」");

        return await GetTemplateByIdAsync(clone.Id);
    }

    // === Submission ===

    public async Task<FormSubmissionResponseDto> SubmitFormAsync(
        SubmitFormDto dto, int orgId, int therapistUserId)
    {
        var template = await _db.FormTemplates.FindAsync(dto.TemplateId)
            ?? throw new KeyNotFoundException("表單範本不存在");

        var submission = new FormSubmission
        {
            OrganizationId = orgId,
            TemplateId = dto.TemplateId,
            TemplateVersion = template.Version,
            ClientProfileId = dto.ClientProfileId,
            TherapistUserId = therapistUserId,
        };

        _db.FormSubmissions.Add(submission);
        await _db.SaveChangesAsync();

        foreach (var val in dto.Values)
        {
            _db.FormFieldValues.Add(new FormFieldValue
            {
                SubmissionId = submission.Id,
                FieldId = val.FieldId,
                Value = val.Value,
            });
        }
        await _db.SaveChangesAsync();

        await _audit.LogAsync("submit", "form_submission", submission.Id,
            $"提交表單「{template.Name}」（個案 #{dto.ClientProfileId}）");

        return await GetSubmissionByIdAsync(submission.Id);
    }

    public async Task<FormSubmissionResponseDto> GetSubmissionByIdAsync(int id)
    {
        var submission = await _db.FormSubmissions.AsNoTracking()
            .Include(s => s.Template)
            .Include(s => s.ClientProfile)
            .Include(s => s.TherapistUser)
            .Include(s => s.FieldValues)
                .ThenInclude(fv => fv.Field)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException("表單提交不存在");

        // 表單填答屬臨床紀錄，代診可讀他人填答 → 留檢視稽核
        await _audit.LogAsync("view", "form_submission", submission.Id,
            $"檢視表單填答 #{id}（個案 #{submission.ClientProfileId}）");

        return new FormSubmissionResponseDto
        {
            Id = submission.Id,
            TemplateId = submission.TemplateId,
            TemplateName = submission.Template.Name,
            TemplateVersion = submission.TemplateVersion,
            ClientProfileId = submission.ClientProfileId,
            ClientName = submission.ClientProfile.Name,
            TherapistUserId = submission.TherapistUserId,
            TherapistName = submission.TherapistUser.DisplayName,
            SubmittedAt = submission.SubmittedAt,
            Values = submission.FieldValues.Select(fv => new FieldValueResponseDto
            {
                FieldId = fv.FieldId,
                FieldLabel = fv.Field.Label,
                FieldType = fv.Field.FieldType,
                Value = fv.Value,
            }).ToList(),
        };
    }

    public async Task<List<FormSubmissionResponseDto>> GetSubmissionsByClientAsync(int clientProfileId)
    {
        var submissions = await _db.FormSubmissions.AsNoTracking()
            .Include(s => s.Template)
            .Include(s => s.ClientProfile)
            .Include(s => s.TherapistUser)
            .Include(s => s.FieldValues)
                .ThenInclude(fv => fv.Field)
            .Where(s => s.ClientProfileId == clientProfileId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();

        // 表單填答屬臨床紀錄，代診可讀他人填答 → 留檢視稽核
        await _audit.LogAsync("view", "form_submission", clientProfileId,
            $"檢視個案 {clientProfileId} 的表單填答（{submissions.Count} 筆）");

        return submissions.Select(s => new FormSubmissionResponseDto
        {
            Id = s.Id,
            TemplateId = s.TemplateId,
            TemplateName = s.Template.Name,
            TemplateVersion = s.TemplateVersion,
            ClientProfileId = s.ClientProfileId,
            ClientName = s.ClientProfile.Name,
            TherapistUserId = s.TherapistUserId,
            TherapistName = s.TherapistUser.DisplayName,
            SubmittedAt = s.SubmittedAt,
            Values = s.FieldValues.Select(fv => new FieldValueResponseDto
            {
                FieldId = fv.FieldId,
                FieldLabel = fv.Field.Label,
                FieldType = fv.Field.FieldType,
                Value = fv.Value,
            }).ToList(),
        }).ToList();
    }

    // === Private helpers ===

    private async Task CreateSectionsAndFieldsAsync(
        int templateId, List<CreateFormSectionDto> sectionDtos)
    {
        foreach (var sDto in sectionDtos)
        {
            var section = new FormSection
            {
                TemplateId = templateId,
                Title = sDto.Title,
                SortOrder = sDto.SortOrder,
                IsRequired = sDto.IsRequired,
            };
            _db.FormSections.Add(section);
            await _db.SaveChangesAsync();

            foreach (var fDto in sDto.Fields)
            {
                _db.FormFields.Add(new FormField
                {
                    SectionId = section.Id,
                    Label = fDto.Label,
                    FieldType = fDto.FieldType,
                    Options = fDto.Options,
                    IsRequired = fDto.IsRequired,
                    SortOrder = fDto.SortOrder,
                    Placeholder = fDto.Placeholder,
                    ValidationRules = fDto.ValidationRules,
                });
            }
            await _db.SaveChangesAsync();
        }
    }

    private static FormTemplateResponseDto MapTemplateToDto(Models.FormTemplate t)
    {
        return new FormTemplateResponseDto
        {
            Id = t.Id,
            Name = t.Name,
            Type = t.Type,
            IsSystemDefault = t.IsSystemDefault,
            Version = t.Version,
            IsActive = t.IsActive,
            Sections = t.Sections.Select(s => new FormSectionResponseDto
            {
                Id = s.Id,
                Title = s.Title,
                SortOrder = s.SortOrder,
                IsRequired = s.IsRequired,
                Fields = s.Fields.Select(f => new FormFieldResponseDto
                {
                    Id = f.Id,
                    Label = f.Label,
                    FieldType = f.FieldType,
                    Options = f.Options,
                    IsRequired = f.IsRequired,
                    SortOrder = f.SortOrder,
                    Placeholder = f.Placeholder,
                    ValidationRules = f.ValidationRules,
                }).ToList(),
            }).ToList(),
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
        };
    }
}
