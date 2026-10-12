using System.Security.Cryptography;
using consulcore_backend.Authorization;
using consulcore_backend.Data;
using consulcore_backend.DTOs.ClientProfile;
using consulcore_backend.Exceptions;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Services;

public class ClientService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    private readonly NotificationService _notify;

    public ClientService(AppDbContext db, IAuditService audit, NotificationService notify)
    {
        _db = db;
        _audit = audit;
        _notify = notify;
    }

    /// <summary>未刪除的個案（D6 軟刪除）；預約、紀錄等歷史資料仍可透過導覽屬性顯示已刪除個案的姓名</summary>
    private IQueryable<ClientProfile> ExistingClients => _db.ClientProfiles.Where(c => !c.IsDeleted);

    public async Task<List<ClientProfileResponseDto>> GetAllAsync()
    {
        var clients = await ExistingClients
            .Include(c => c.TherapistClients)
                .ThenInclude(tc => tc.TherapistUser)
            .ToListAsync();

        return clients.Select(MapToDto).ToList();
    }

    public async Task<ClientProfileResponseDto> GetByIdAsync(int id)
    {
        var client = await ExistingClients
            .Include(c => c.TherapistClients)
                .ThenInclude(tc => tc.TherapistUser)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("個案不存在");

        return MapToDto(client);
    }

    // controller 檢視單一個案專用：記錄檢視稽核後回傳（與內部 mapper GetByIdAsync 區隔，避免異動時誤記 view）
    // crossTherapist=true 代表代診/非指派存取，於稽核描述標示以利事後追溯
    public async Task<ClientProfileResponseDto> ViewByIdAsync(int id, bool crossTherapist = false)
    {
        var dto = await GetByIdAsync(id);
        var detail = crossTherapist ? $"檢視個案 (id={id})（代診/非指派存取）" : $"檢視個案 (id={id})";
        await _audit.LogAsync("view", "client_profile", id, detail);
        return dto;
    }

    // 由登入帳號 userId 取得其對應的個案檔 id（供 client 角色自我存取範圍控制；無則 null）
    public async Task<int?> GetClientProfileIdByUserAsync(int userId)
    {
        return await ExistingClients
            .Where(c => c.UserId == userId)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<ClientProfileResponseDto>> GetByTherapistAsync(int therapistUserId)
    {
        var clientIds = await _db.TherapistClients
            .Where(tc => tc.TherapistUserId == therapistUserId && tc.Status == TherapistClientStatus.Active)
            .Select(tc => tc.ClientProfileId)
            .ToListAsync();

        var clients = await _db.ClientProfiles
            .Include(c => c.TherapistClients)
                .ThenInclude(tc => tc.TherapistUser)
            .Where(c => clientIds.Contains(c.Id))
            .ToListAsync();

        return clients.Select(MapToDto).ToList();
    }

    // 指定的心理師必須是本機構（Users 有機構 query filter）、啟用中、具 therapist 角色的使用者
    private async Task EnsureTherapistOfOrganizationAsync(int userId)
    {
        var valid = await _db.Users.AnyAsync(u => u.Id == userId && u.IsActive
            && u.UserRoles.Any(ur => ur.Role.Name == Roles.Therapist));
        if (!valid)
            throw new BusinessRuleException("指定的心理師不存在，或不是本機構的心理師");
    }

    private Task<bool> IsAssignedTherapistAsync(int clientId, int therapistUserId) =>
        _db.TherapistClients.AnyAsync(tc => tc.ClientProfileId == clientId
            && tc.TherapistUserId == therapistUserId
            && tc.Status == TherapistClientStatus.Active);

    public async Task<ClientProfileResponseDto> CreateAsync(CreateClientProfileDto dto, int orgId)
    {
        if (dto.TherapistUserId.HasValue)
            await EnsureTherapistOfOrganizationAsync(dto.TherapistUserId.Value);

        var client = new Models.ClientProfile
        {
            OrganizationId = orgId,
            Name = dto.Name,
            IdNumber = dto.IdNumber,
            Gender = dto.Gender,
            DateOfBirth = dto.DateOfBirth,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
            EmergencyContactName = dto.EmergencyContactName,
            EmergencyContactPhone = dto.EmergencyContactPhone,
            EmergencyContactRelationship = dto.EmergencyContactRelationship,
            Occupation = dto.Occupation,
            ReferralSource = dto.ReferralSource,
            Notes = dto.Notes,
            MedicalHistory = dto.MedicalHistory,
            Status = ClientStatus.Active,
        };

        InitialCredentialsDto? initialCredentials = null;
        if (dto.CreateAccount)
        {
            var initialPassword = GenerateInitialPassword();
            var username = GenerateClientUsername(orgId);

            // 檢查 username 是否已存在
            var usernameExists = await _db.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Username == username);
            if (usernameExists)
                throw new BusinessRuleException($"自動產生的使用者名稱 '{username}' 已被使用，請改用不建立帳號方式");

            var user = new User
            {
                OrganizationId = orgId,
                Username = username,
                Email = dto.Email ?? $"{username}@placeholder.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(initialPassword),
                DisplayName = dto.Name,
                Phone = dto.Phone,
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // 指派 client 角色
            var clientRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == Roles.Client)
                ?? throw new BusinessRuleException("找不到 client 角色");
            _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = clientRole.Id });

            client.UserId = user.Id;
            client.MustChangePassword = true;
            initialCredentials = new InitialCredentialsDto { Username = username, Password = initialPassword };
        }

        _db.ClientProfiles.Add(client);
        await _db.SaveChangesAsync();

        // 指派心理師
        if (dto.TherapistUserId.HasValue)
        {
            _db.TherapistClients.Add(new TherapistClient
            {
                OrganizationId = orgId,
                TherapistUserId = dto.TherapistUserId.Value,
                ClientProfileId = client.Id,
            });
            await _db.SaveChangesAsync();
        }

        await _audit.LogAsync("create", "client_profile", client.Id, $"建立個案 (id={client.Id})");

        // 通知：account_created
        if (client.UserId.HasValue)
        {
            var vars = new Dictionary<string, string> { ["client_name"] = dto.Name };
            await _notify.SendAsync(NotificationEventType.AccountCreated, client.UserId.Value, Roles.Client, orgId, vars);
        }

        var created = await GetByIdAsync(client.Id);
        created.InitialCredentials = initialCredentials;
        return created;
    }

    /// <param name="hasFullScope">owner / admin（已驗證）可修改全機構個案</param>
    public async Task<ClientProfileResponseDto> UpdateAsync(
        int id, UpdateClientProfileDto dto, int actorUserId, bool hasFullScope)
    {
        var client = await ExistingClients.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("個案不存在");

        // BR-20 的跨心理師存取只放寬「唯讀」；修改限指派中的心理師
        if (!hasFullScope && !await IsAssignedTherapistAsync(id, actorUserId))
            throw new ForbiddenException("只有指派中的心理師可以修改此個案資料");

        if (dto.Name != null) client.Name = dto.Name;
        if (dto.IdNumber != null) client.IdNumber = dto.IdNumber;
        if (dto.Gender != null) client.Gender = dto.Gender;
        if (dto.DateOfBirth != null) client.DateOfBirth = dto.DateOfBirth;
        if (dto.Phone != null) client.Phone = dto.Phone;
        if (dto.Email != null) client.Email = dto.Email;
        if (dto.Address != null) client.Address = dto.Address;
        if (dto.EmergencyContactName != null) client.EmergencyContactName = dto.EmergencyContactName;
        if (dto.EmergencyContactPhone != null) client.EmergencyContactPhone = dto.EmergencyContactPhone;
        if (dto.EmergencyContactRelationship != null) client.EmergencyContactRelationship = dto.EmergencyContactRelationship;
        if (dto.Occupation != null) client.Occupation = dto.Occupation;
        if (dto.ReferralSource != null) client.ReferralSource = dto.ReferralSource;
        if (dto.Notes != null) client.Notes = dto.Notes;
        if (dto.MedicalHistory != null) client.MedicalHistory = dto.MedicalHistory;
        client.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync("update", "client_profile", id, $"更新個案 (id={id})");

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var client = await ExistingClients
            .Include(c => c.TherapistClients)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("個案不存在");

        // 軟刪除（D6 / BR-15）：硬刪除會經由外鍵串聯刪掉預約、晤談紀錄、收費與同意書
        var now = DateTime.UtcNow;
        client.IsDeleted = true;
        client.DeletedAt = now;
        client.UpdatedAt = now;
        foreach (var relation in client.TherapistClients.Where(tc => tc.Status == TherapistClientStatus.Active))
            relation.Status = TherapistClientStatus.Closed;

        // 附帳號的個案：帳號一併停用並軟刪除，否則個案仍可登入
        if (client.UserId is { } accountId)
        {
            var account = await _db.Users.FirstAsync(u => u.Id == accountId);
            account.IsDeleted = true;
            account.DeletedAt = now;
            account.IsActive = false;
        }
        await _db.SaveChangesAsync();

        await _audit.LogAsync("delete", "client_profile", id, $"刪除個案 (id={id})");
    }

    public async Task<ClientProfileResponseDto> AssignTherapistAsync(int clientId, AssignTherapistDto dto, int orgId)
    {
        var client = await ExistingClients.FirstOrDefaultAsync(c => c.Id == clientId)
            ?? throw new KeyNotFoundException("個案不存在");
        await EnsureTherapistOfOrganizationAsync(dto.TherapistUserId);

        // 檢查是否已有此心理師的 active 關係
        var existing = await _db.TherapistClients
            .AnyAsync(tc => tc.ClientProfileId == clientId
                && tc.TherapistUserId == dto.TherapistUserId
                && tc.Status == TherapistClientStatus.Active);
        if (existing)
            throw new BusinessRuleException("此心理師已指派給該個案");

        _db.TherapistClients.Add(new TherapistClient
        {
            OrganizationId = orgId,
            TherapistUserId = dto.TherapistUserId,
            ClientProfileId = clientId,
        });
        await _db.SaveChangesAsync();

        await _audit.LogAsync("assign_therapist", "client_profile", clientId,
            $"指派心理師 (userId={dto.TherapistUserId}) 給個案 (id={clientId})");

        return await GetByIdAsync(clientId);
    }

    public async Task<ClientProfileResponseDto> TransferTherapistAsync(
        int clientId, int currentTherapistUserId, TransferTherapistDto dto, int orgId)
    {
        await EnsureTherapistOfOrganizationAsync(dto.NewTherapistUserId);

        // 結束舊關係
        var oldRelation = await _db.TherapistClients
            .FirstOrDefaultAsync(tc => tc.ClientProfileId == clientId
                && tc.TherapistUserId == currentTherapistUserId
                && tc.Status == TherapistClientStatus.Active)
            ?? throw new KeyNotFoundException("找不到此心理師與個案的有效關係");

        oldRelation.Status = TherapistClientStatus.Transferred;
        oldRelation.EndedAt = DateTime.UtcNow;
        oldRelation.EndedReason = dto.Reason ?? "移轉";

        // 建立新關係
        _db.TherapistClients.Add(new TherapistClient
        {
            OrganizationId = orgId,
            TherapistUserId = dto.NewTherapistUserId,
            ClientProfileId = clientId,
        });
        await _db.SaveChangesAsync();

        await _audit.LogAsync("transfer_therapist", "client_profile", clientId,
            $"移轉個案 (id={clientId}) 從心理師 {currentTherapistUserId} 到 {dto.NewTherapistUserId}",
            new { from = currentTherapistUserId, to = dto.NewTherapistUserId, reason = dto.Reason });

        return await GetByIdAsync(clientId);
    }

    public async Task<ClientProfileResponseDto> ChangeStatusAsync(int id, ChangeStatusDto dto)
    {
        var validStatuses = new[] { ClientStatus.Pending, ClientStatus.Active, ClientStatus.Suspended, ClientStatus.Closed };
        if (!validStatuses.Contains(dto.Status))
            throw new BusinessRuleException($"無效的狀態：{dto.Status}");

        var client = await ExistingClients.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("個案不存在");

        var oldStatus = client.Status;
        client.Status = dto.Status;
        client.UpdatedAt = DateTime.UtcNow;

        // 結案時關閉所有 active 的心理師關係
        if (dto.Status == ClientStatus.Closed)
        {
            var activeRelations = await _db.TherapistClients
                .Where(tc => tc.ClientProfileId == id && tc.Status == TherapistClientStatus.Active)
                .ToListAsync();

            foreach (var rel in activeRelations)
            {
                rel.Status = TherapistClientStatus.Closed;
                rel.EndedAt = DateTime.UtcNow;
                rel.EndedReason = "個案結案";
            }
        }

        await _db.SaveChangesAsync();

        await _audit.LogAsync("change_status", "client_profile", id,
            $"個案狀態變更 {oldStatus} → {dto.Status}",
            new { from = oldStatus, to = dto.Status });

        return await GetByIdAsync(id);
    }

    // 判斷個案是否目前指派給指定心理師（active 關係），供 controller 做存取隔離
    public async Task<bool> IsAssignedToTherapistAsync(int clientProfileId, int therapistUserId)
    {
        return await _db.TherapistClients.AnyAsync(tc =>
            tc.ClientProfileId == clientProfileId
            && tc.TherapistUserId == therapistUserId
            && tc.Status == TherapistClientStatus.Active);
    }

    public async Task<List<ClientProfileResponseDto>> SearchAsync(
        string? keyword, string? idNumberLast4, int? scopeTherapistUserId = null)
    {
        // 加密欄位無法在 DB 層搜尋，需在應用層比對
        var query = ExistingClients
            .Include(c => c.TherapistClients)
                .ThenInclude(tc => tc.TherapistUser)
            .AsQueryable();

        // therapist scope：僅比對自己被指派（active）的個案
        if (scopeTherapistUserId.HasValue)
        {
            query = query.Where(c => c.TherapistClients.Any(tc =>
                tc.TherapistUserId == scopeTherapistUserId.Value
                && tc.Status == TherapistClientStatus.Active));
        }

        var clients = await query.ToListAsync();

        if (!string.IsNullOrEmpty(keyword))
        {
            clients = clients.Where(c =>
                c.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        if (!string.IsNullOrEmpty(idNumberLast4))
        {
            clients = clients.Where(c =>
                c.IdNumber != null && c.IdNumber.EndsWith(idNumberLast4)
            ).ToList();
        }

        // 檢視稽核：個案搜尋（不記錄關鍵字明文，避免個資落入稽核描述）
        await _audit.LogAsync("search", "client_profile", 0,
            $"搜尋個案（keyword={(string.IsNullOrEmpty(keyword) ? "-" : "***")}, idLast4={(string.IsNullOrEmpty(idNumberLast4) ? "-" : "***")}，命中 {clients.Count} 筆）");

        return clients.Select(MapToDto).ToList();
    }

    // 初始密碼隨機產生（原本由身分證後 4 碼 + 生日組成，知道個案基本資料就能登入）。
    // 去掉 0/O、1/l/I 等易混淆字元，方便櫃檯口頭或紙本交付；個案首次登入須改密碼。
    private const string InitialPasswordAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

    private static string GenerateInitialPassword() =>
        RandomNumberGenerator.GetString(InitialPasswordAlphabet, 10);

    // 帳號用隨機尾碼：原本的秒級時間戳可被猜出，且同一秒建立兩位個案會撞名
    private const string UsernameSuffixAlphabet = "abcdefghjkmnpqrstuvwxyz23456789";

    private static string GenerateClientUsername(int orgId) =>
        $"client_{orgId}_{RandomNumberGenerator.GetString(UsernameSuffixAlphabet, 8)}";

    private static ClientProfileResponseDto MapToDto(Models.ClientProfile client)
    {
        return new ClientProfileResponseDto
        {
            Id = client.Id,
            Name = client.Name,
            IdNumber = client.IdNumber,
            Gender = client.Gender,
            DateOfBirth = client.DateOfBirth,
            Phone = client.Phone,
            Email = client.Email,
            Address = client.Address,
            EmergencyContactName = client.EmergencyContactName,
            EmergencyContactPhone = client.EmergencyContactPhone,
            EmergencyContactRelationship = client.EmergencyContactRelationship,
            Occupation = client.Occupation,
            ReferralSource = client.ReferralSource,
            Notes = client.Notes,
            MedicalHistory = client.MedicalHistory,
            Status = client.Status,
            UserId = client.UserId,
            MustChangePassword = client.MustChangePassword,
            Therapists = client.TherapistClients.Select(tc => new TherapistInfoDto
            {
                TherapistUserId = tc.TherapistUserId,
                TherapistName = tc.TherapistUser?.DisplayName ?? "",
                Status = tc.Status,
                AssignedAt = tc.CreatedAt
            }).ToList(),
            CreatedAt = client.CreatedAt,
            UpdatedAt = client.UpdatedAt
        };
    }
}
