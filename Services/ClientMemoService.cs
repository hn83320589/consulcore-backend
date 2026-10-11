using consulcore_backend.Data;
using consulcore_backend.DTOs.ClientMemo;
using consulcore_backend.Exceptions;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Services;

public class ClientMemoService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public ClientMemoService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<List<ClientMemoResponseDto>> GetByClientAsync(
        int clientProfileId, int currentTherapistUserId)
    {
        var memos = await _db.ClientMemos
            .Include(m => m.TherapistUser)
            .Where(m => m.ClientProfileId == clientProfileId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        // 個案備忘屬臨床紀錄，代診可讀他人備忘 → 留檢視稽核（BR-16 唯讀由 IsEditable 標記）
        await _audit.LogAsync("view", "client_memo", clientProfileId,
            $"檢視個案 {clientProfileId} 的備忘（{memos.Count} 筆）");

        // BR-16: 標記可編輯/唯讀
        return memos.Select(m => MapToDto(m, currentTherapistUserId)).ToList();
    }

    public async Task<ClientMemoResponseDto> CreateAsync(
        CreateClientMemoDto dto, int orgId, int therapistUserId)
    {
        var memo = new ClientMemo
        {
            OrganizationId = orgId,
            ClientProfileId = dto.ClientProfileId,
            TherapistUserId = therapistUserId,
            Category = dto.Category,
            Content = dto.Content,
        };

        _db.ClientMemos.Add(memo);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("create", "client_memo", memo.Id,
            $"建立個案備忘 (client={dto.ClientProfileId})");

        return await GetByIdAsync(memo.Id, therapistUserId);
    }

    public async Task<ClientMemoResponseDto> UpdateAsync(
        int id, UpdateClientMemoDto dto, int therapistUserId)
    {
        var memo = await _db.ClientMemos.FindAsync(id)
            ?? throw new KeyNotFoundException("個案備忘不存在");

        // BR-16: 僅建立者可編輯
        if (memo.TherapistUserId != therapistUserId)
            throw new ForbiddenException("僅建立此備忘的心理師可編輯");

        if (dto.Category != null) memo.Category = dto.Category;
        if (dto.Content != null) memo.Content = dto.Content;
        memo.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.LogAsync("update", "client_memo", id, "更新個案備忘");

        return await GetByIdAsync(id, therapistUserId);
    }

    public async Task DeleteAsync(int id, int therapistUserId)
    {
        var memo = await _db.ClientMemos.FindAsync(id)
            ?? throw new KeyNotFoundException("個案備忘不存在");

        if (memo.TherapistUserId != therapistUserId)
            throw new ForbiddenException("僅建立此備忘的心理師可刪除");

        _db.ClientMemos.Remove(memo);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("delete", "client_memo", id, "刪除個案備忘");
    }

    private async Task<ClientMemoResponseDto> GetByIdAsync(int id, int currentTherapistUserId)
    {
        var memo = await _db.ClientMemos
            .Include(m => m.TherapistUser)
            .FirstOrDefaultAsync(m => m.Id == id)
            ?? throw new KeyNotFoundException("個案備忘不存在");

        return MapToDto(memo, currentTherapistUserId);
    }

    private static ClientMemoResponseDto MapToDto(ClientMemo m, int currentTherapistUserId)
    {
        return new ClientMemoResponseDto
        {
            Id = m.Id,
            ClientProfileId = m.ClientProfileId,
            TherapistUserId = m.TherapistUserId,
            TherapistName = m.TherapistUser?.DisplayName ?? "",
            Category = m.Category,
            Content = m.Content,
            IsEditable = m.TherapistUserId == currentTherapistUserId,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt,
        };
    }
}
