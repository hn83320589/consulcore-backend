# 建立新 API 端點

## 規範

### 路由命名
- 資源名稱用 kebab-case 複數：`/api/client-profiles`
- 巢狀資源：`/api/client-profiles/{id}/memos`
- 動作型端點用動詞：`/api/appointments/{id}/complete`

### Controller 結構

```csharp
[ApiController]
[Route("api/resource-names")]
public class ResourceNameController : ControllerBase
{
    private readonly ResourceNameService _service;

    // 公開端點（無 [Authorize]）
    [HttpGet("public")]
    public async Task<ActionResult<ApiResponse<List<ResponseDto>>>> GetPublic() { }

    // 需認證端點
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ResponseDto>>>> GetAll() { }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ResponseDto>>> GetById(int id) { }

    // 需特定權限
    [Authorize(Policy = "CanManageResource")]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ResponseDto>>> Create(CreateDto dto) { }

    [Authorize(Policy = "CanManageResource")]
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ResponseDto>>> Update(int id, UpdateDto dto) { }

    [Authorize(Policy = "CanManageResource")]
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(int id) { }
}
```

### 回應格式

永遠使用 `ApiResponse<T>` 包裝：

```csharp
return Ok(ApiResponse<T>.Ok(data, "操作成功"));
return NotFound(ApiResponse<T>.Fail("找不到資源"));
return BadRequest(ApiResponse<T>.Fail("驗證失敗", errors));
```

### 權限對應

| 操作 | Permission Code | 適用角色 |
|------|----------------|---------|
| 檢視 | `{resource}.view` | 依業務邏輯 |
| 新增 | `{resource}.create` | 依業務邏輯 |
| 編輯 | `{resource}.update` | 依業務邏輯 |
| 刪除 | `{resource}.delete` | 依業務邏輯 |
| 管理 | `{resource}.manage` | 通常 Owner only |

### 檢查清單

- [ ] 路由用 kebab-case
- [ ] 公開端點不加 [Authorize]
- [ ] 需認證端點加 [Authorize] 或 [Authorize(Policy = "...")]
- [ ] 回傳統一用 ApiResponse<T>
- [ ] Service 層處理業務邏輯，Controller 不含邏輯
- [ ] 新增/更新/刪除操作，以及敏感資料的檢視，在 Service 中觸發稽核 Log
