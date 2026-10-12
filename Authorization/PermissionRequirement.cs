using Microsoft.AspNetCore.Authorization;

namespace consulcore_backend.Authorization;

/// <summary>目前角色擁有其中任一權限碼即通過。</summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public IReadOnlyList<string> PermissionCodes { get; }

    public PermissionRequirement(params string[] permissionCodes)
    {
        PermissionCodes = permissionCodes;
    }
}
