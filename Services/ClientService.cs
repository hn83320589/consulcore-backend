using consulcore_backend.Data;
using consulcore_backend.DTOs.ClientProfile;
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

    public async Task<List<ClientProfileResponseDto>> GetAllAsync()
    {
        var clients = await _db.ClientProfiles
            .Include(c => c.TherapistClients)
                .ThenInclude(tc => tc.TherapistUser)
            .ToListAsync();

        return clients.Select(MapToDto).ToList();
    }

    public async Task<ClientProfileResponseDto> GetByIdAsync(int id)
    {
        var client = await _db.ClientProfiles
            .Include(c => c.TherapistClients)
                .ThenInclude(tc => tc.TherapistUser)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("個案不存在");

        return MapToDto(client);
    }

    // controller 檢視單一個案專用：記錄檢視稽核後回傳（與內部 mapper GetByIdAsync 區隔，避免異動時誤記 view）
    public async Task<ClientProfileResponseDto> ViewByIdAsync(int id)
    {
        var dto = await GetByIdAsync(id);
        await _audit.LogAsync("view", "client_profile", id, $"檢視個案 (id={id})");
        return dto;
    }

    // 由登入帳號 userId 取得其對應的個案檔 id（供 client 角色自我存取範圍控制；無則 null）
    public async Task<int?> GetClientProfileIdByUserAsync(int userId)
    {
        return await _db.ClientProfiles
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

    public async Task<ClientProfileResponseDto> CreateAsync(CreateClientProfileDto dto, int orgId)
    {
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

        if (dto.CreateAccount)
        {
            var defaultPassword = GenerateDefaultPassword(dto.IdNumber, dto.DateOfBirth);
            var username = GenerateClientUsername(dto.Name, orgId);

            // 檢查 username 是否已存在
            var usernameExists = await _db.Users
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Username == username);
            if (usernameExists)
                throw new InvalidOperationException($"自動產生的使用者名稱 '{username}' 已被使用，請改用不建立帳號方式");

            var user = new User
            {
                OrganizationId = orgId,
                Username = username,
                Email = dto.Email ?? $"{username}@placeholder.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword),
                DisplayName = dto.Name,
                Phone = dto.Phone,
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // 指派 client 角色
            var clientRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "client")
                ?? throw new InvalidOperationException("找不到 client 角色");
            _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = clientRole.Id });

            client.UserId = user.Id;
            client.MustChangePassword = true;
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
            await _notify.SendAsync(NotificationEventType.AccountCreated, client.UserId.Value, "client", orgId, vars);
        }

        return await GetByIdAsync(client.Id);
    }

    public async Task<ClientProfileResponseDto> UpdateAsync(int id, UpdateClientProfileDto dto)
    {
        var client = await _db.ClientProfiles.FindAsync(id)
            ?? throw new KeyNotFoundException("個案不存在");

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
        var client = await _db.ClientProfiles
            .Include(c => c.TherapistClients)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("個案不存在");

        _db.TherapistClients.RemoveRange(client.TherapistClients);
        _db.ClientProfiles.Remove(client);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("delete", "client_profile", id, $"刪除個案 (id={id})");
    }

    public async Task<ClientProfileResponseDto> AssignTherapistAsync(int clientId, AssignTherapistDto dto, int orgId)
    {
        var client = await _db.ClientProfiles.FindAsync(clientId)
            ?? throw new KeyNotFoundException("個案不存在");

        // 檢查是否已有此心理師的 active 關係
        var existing = await _db.TherapistClients
            .AnyAsync(tc => tc.ClientProfileId == clientId
                && tc.TherapistUserId == dto.TherapistUserId
                && tc.Status == TherapistClientStatus.Active);
        if (existing)
            throw new InvalidOperationException("此心理師已指派給該個案");

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
            throw new InvalidOperationException($"無效的狀態：{dto.Status}");

        var client = await _db.ClientProfiles.FindAsync(id)
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
        var query = _db.ClientProfiles
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

    private static string GenerateDefaultPassword(string? idNumber, string? dateOfBirth)
    {
        // 預設密碼：身分證後4碼 + 生日MMDD
        var idLast4 = !string.IsNullOrEmpty(idNumber) && idNumber.Length >= 4
            ? idNumber[^4..] : "0000";

        var mmdd = "0101";
        if (!string.IsNullOrEmpty(dateOfBirth) && DateTime.TryParse(dateOfBirth, out var dob))
        {
            mmdd = dob.ToString("MMdd");
        }

        return $"{idLast4}{mmdd}";
    }

    private string GenerateClientUsername(string name, int orgId)
    {
        // 產生 client_orgId_timestamp 格式的 username
        return $"client_{orgId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    }

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
