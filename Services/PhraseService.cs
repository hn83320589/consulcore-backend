using consulcore_backend.Data;
using consulcore_backend.DTOs.PhraseTemplate;
using consulcore_backend.Exceptions;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Services;

public class PhraseService
{
    private readonly AppDbContext _db;

    public PhraseService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<PhraseTemplateResponseDto>> GetAllAsync(int therapistUserId)
    {
        return await _db.PhraseTemplates
            .Where(p => p.TherapistUserId == therapistUserId)
            .OrderBy(p => p.Category)
            .ThenBy(p => p.SortOrder)
            .Select(p => MapToDto(p))
            .ToListAsync();
    }

    public async Task<PhraseTemplateResponseDto> CreateAsync(
        CreatePhraseTemplateDto dto, int orgId, int therapistUserId)
    {
        var phrase = new Models.PhraseTemplate
        {
            OrganizationId = orgId,
            TherapistUserId = therapistUserId,
            Category = dto.Category,
            Title = dto.Title,
            Content = dto.Content,
            SortOrder = dto.SortOrder,
        };

        _db.PhraseTemplates.Add(phrase);
        await _db.SaveChangesAsync();

        return MapToDto(phrase);
    }

    public async Task<PhraseTemplateResponseDto> UpdateAsync(
        int id, UpdatePhraseTemplateDto dto, int therapistUserId)
    {
        var phrase = await _db.PhraseTemplates.FindAsync(id)
            ?? throw new KeyNotFoundException("常用語範本不存在");

        if (phrase.TherapistUserId != therapistUserId)
            throw new ForbiddenException("僅建立者可編輯");

        if (dto.Category != null) phrase.Category = dto.Category;
        if (dto.Title != null) phrase.Title = dto.Title;
        if (dto.Content != null) phrase.Content = dto.Content;
        if (dto.SortOrder.HasValue) phrase.SortOrder = dto.SortOrder.Value;
        phrase.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToDto(phrase);
    }

    public async Task DeleteAsync(int id, int therapistUserId)
    {
        var phrase = await _db.PhraseTemplates.FindAsync(id)
            ?? throw new KeyNotFoundException("常用語範本不存在");

        if (phrase.TherapistUserId != therapistUserId)
            throw new ForbiddenException("僅建立者可刪除");

        _db.PhraseTemplates.Remove(phrase);
        await _db.SaveChangesAsync();
    }

    private static PhraseTemplateResponseDto MapToDto(Models.PhraseTemplate p)
    {
        return new PhraseTemplateResponseDto
        {
            Id = p.Id,
            Category = p.Category,
            Title = p.Title,
            Content = p.Content,
            SortOrder = p.SortOrder,
            CreatedAt = p.CreatedAt,
        };
    }
}
