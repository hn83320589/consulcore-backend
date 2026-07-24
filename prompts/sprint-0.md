# Sprint 0 — 後端：專案初始化

## 目標
建立 .NET 10 Web API 骨架，完成基礎建設：加密服務、JWT 認證、Global Query Filter、統一回應格式、稽核服務骨架。

---

## 任務

### 1. 建立專案
```bash
dotnet new webapi -n consulcore-backend
// 與 CLAUDE.md 所規劃的專案結構需一致
```

安裝 NuGet：Pomelo.EntityFrameworkCore.MySql, Microsoft.AspNetCore.Authentication.JwtBearer, BCrypt.Net-Next, Swashbuckle.AspNetCore

### 2. ApiResponse<T>（Models/ApiResponse.cs）
```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
    public static ApiResponse<T> Ok(T data, string msg = "操作成功") => new() { Success = true, Message = msg, Data = data };
    public static ApiResponse<T> Fail(string msg, List<string>? errors = null) => new() { Success = false, Message = msg, Errors = errors };
}
```

### 3. 加密服務（Data/Encryption/）
- `IEncryptionService`：Encrypt(string) / Decrypt(string)
- `AesEncryptionService`：AES-256-GCM，密鑰從 `IConfiguration["Encryption:Key"]` 讀取
- `EncryptedStringConverter` : `ValueConverter<string, string>`
- `[Encrypted]` 自訂屬性，標記需加密的 Property

### 4. AppDbContext（Data/AppDbContext.cs）
- 注入 `IHttpContextAccessor`，從 JWT Claims 取 `organizationId` 存為 `_currentOrgId`
- OnModelCreating 預留 Global Query Filter 接入點（先寫註解，Entity 在 Sprint 1 建）
- 自動掃描所有標記 `[Encrypted]` 的 Property，套用 EncryptedStringConverter

### 5. ErrorHandlingMiddleware（Middleware/）
- 全域攔截未處理 Exception
- 回傳 ApiResponse.Fail()
- 開發環境回傳堆疊，生產環境只回傳訊息

### 6. JWT 認證（Program.cs）
- AddAuthentication + AddJwtBearer
- Claims：userId, organizationId, roles (JSON array), username
- Token 有效期 24 小時
- Issuer: "ConsulCore"
- 密鑰從 IConfiguration["Jwt:SecretKey"]

### 7. CORS
- 允許 AllowedOrigins 設定值（開發環境 http://localhost:5173）

### 8. IAuditService 骨架（Services/AuditService.cs）
- 介面：LogAsync(action, resourceType, resourceId, description, changes?)
- 實作：暫時只寫 Console.WriteLine（Sprint 1 建 AuditLog Entity 後接上）

### 9. appsettings.json
```json
{
  "ConnectionStrings": { "DefaultConnection": "" },
  "Jwt": { "SecretKey": "", "Issuer": "ConsulCore", "ExpiryHours": 24 },
  "Encryption": { "Key": "" },
  "AllowedOrigins": ["http://localhost:5173"]
}
```

### 10. 驗證
- `dotnet run` 可啟動
- Swagger 可存取（http://localhost:5037/swagger）
- 無 DB 連線也不 crash

---

## 交付標準
- [ ] dotnet run 成功啟動
- [ ] Swagger 頁面可存取
- [ ] 加密服務可正常加解密字串
- [ ] JWT 可手動產生 token（寫個臨時測試端點）
- [ ] ErrorHandlingMiddleware 正常攔截錯誤
- [ ] ApiResponse 格式正確
