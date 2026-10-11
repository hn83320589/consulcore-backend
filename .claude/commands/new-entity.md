# 建立新 Entity

當需要建立新的 EF Core Entity 時，按以下步驟執行：

## 步驟

### 1. 建立 Model（`Models/`）

```csharp
[Table("entity_names")]  // 表名/欄位名 snake_case 一律明確標註
public class EntityName
{
    [Column("id")]
    public int Id { get; set; }

    [Column("organization_id")]
    public int OrganizationId { get; set; }  // 多租戶必加

    // 加密欄位標記（AppDbContext 會自動套用 converter）
    [Encrypted]
    [Column("sensitive_field")]
    public string SensitiveField { get; set; }

    // FK 關聯
    [Column("related_entity_id")]
    public int RelatedEntityId { get; set; }

    // Enum 欄位用 string 儲存
    [Column("status")]
    public string Status { get; set; } = "default_value";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

### 2. 建立 3 層 DTO（`DTOs/`）

```
DTOs/
├── EntityName/
│   ├── CreateEntityNameDto.cs    # 新增時的輸入
│   ├── UpdateEntityNameDto.cs    # 更新時的輸入
│   └── EntityNameResponseDto.cs  # API 回傳
```

- CreateDto：不含 Id、CreatedAt、UpdatedAt
- UpdateDto：與 CreateDto 類似，所有欄位 nullable（部分更新）
- ResponseDto：包含 Id 與所有可見欄位，敏感欄位已解密

### 3. 註冊到 AppDbContext（`Data/AppDbContext.cs`）

```csharp
public DbSet<EntityName> EntityNames { get; set; }

// OnModelCreating 中：
// 1. 加 Global Query Filter
builder.Entity<EntityName>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);

// （[Encrypted] 欄位由 OnModelCreating 的迴圈自動套用 EncryptedStringConverter，不需手動註冊）

// 2. Enum 欄位轉 string
builder.Entity<EntityName>()
    .Property(e => e.Status)
    .HasConversion<string>();

// 3. 索引
builder.Entity<EntityName>()
    .HasIndex(e => e.OrganizationId);
```

### 4. 建立 Service（`Services/`）

```csharp
public class EntityNameService
{
    private readonly AppDbContext _context;
    private readonly IAuditService _audit;
    
    // CRUD 方法 + 業務邏輯
    // 每個 CUD 操作結尾呼叫 _audit.LogAsync()；敏感資料的檢視（單筆 / 依個案查詢）也要記 "view"
}
```

### 5. 建立 Controller（`Controllers/`）

```csharp
[ApiController]
[Route("api/entity-names")]  // kebab-case
[Authorize]
public class EntityNameController : ControllerBase
{
    // 注入 Service，不直接操作 DbContext
    // 回傳統一用 ApiResponse<T>
}
```

### 6. 建立 Migration

```bash
dotnet ef migrations add AddEntityName
dotnet ef database update
```

## 檢查清單

- [ ] Model 有 OrganizationId
- [ ] 加密欄位標記 [Encrypted]
- [ ] Enum 欄位有預設值
- [ ] 3 層 DTO 都建立
- [ ] Global Query Filter 已註冊
- [ ] 表名/欄位已標註 [Table]/[Column]（snake_case）
- [ ] Service 含稽核呼叫
- [ ] Controller 用 kebab-case 路由
- [ ] Migration 已建立並執行
