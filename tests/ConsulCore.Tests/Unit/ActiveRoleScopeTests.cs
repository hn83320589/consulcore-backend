using System.Security.Claims;
using consulcore_backend.Authorization;
using Microsoft.AspNetCore.Http;

namespace ConsulCore.Tests.Unit;

// 鎖住 fail-closed 原則：X-Active-Role 偽造只能讓存取更受限，永遠不會更寬鬆
public class ActiveRoleScopeTests
{
    private static ClaimsPrincipal UserWithRolesClaim(string? rolesJson)
    {
        var claims = rolesJson == null ? [] : new[] { new Claim("roles", rolesJson) };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static HttpRequest RequestWithActiveRole(string? role)
    {
        var context = new DefaultHttpContext();
        if (role != null) context.Request.Headers["X-Active-Role"] = role;
        return context.Request;
    }

    [Fact]
    public void JwtRoles_parses_json_array_claim()
    {
        Assert.Equal(["therapist", "client"], ActiveRoleScope.JwtRoles(UserWithRolesClaim("[\"therapist\",\"client\"]")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-json")]
    [InlineData("{\"role\":\"owner\"}")]
    public void JwtRoles_returns_empty_for_missing_or_malformed_claim(string? rolesJson)
    {
        Assert.Empty(ActiveRoleScope.JwtRoles(UserWithRolesClaim(rolesJson)));
    }

    [Fact]
    public void Verified_staff_role_present_in_jwt_is_accepted()
    {
        var user = UserWithRolesClaim("[\"therapist\"]");

        Assert.True(ActiveRoleScope.ActingAsVerifiedNonClient(RequestWithActiveRole("therapist"), user));
    }

    [Fact]
    public void Forged_header_for_role_not_in_jwt_is_rejected()
    {
        var user = UserWithRolesClaim("[\"client\"]");

        Assert.False(ActiveRoleScope.ActingAsVerifiedNonClient(RequestWithActiveRole("owner"), user));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("client")]
    public void Missing_header_or_client_role_is_never_staff_scope(string? header)
    {
        var user = UserWithRolesClaim("[\"owner\",\"client\"]");

        Assert.False(ActiveRoleScope.ActingAsVerifiedNonClient(RequestWithActiveRole(header), user));
    }

    [Fact]
    public void ActingAsVerified_requires_header_and_jwt_to_name_the_same_role()
    {
        var owner = UserWithRolesClaim("[\"owner\",\"therapist\"]");
        var admin = UserWithRolesClaim("[\"admin\"]");

        Assert.True(ActiveRoleScope.ActingAsVerified(RequestWithActiveRole("owner"), owner, "owner"));
        Assert.False(ActiveRoleScope.ActingAsVerified(RequestWithActiveRole("therapist"), owner, "owner"));
        Assert.False(ActiveRoleScope.ActingAsVerified(RequestWithActiveRole("owner"), admin, "owner"));
        Assert.False(ActiveRoleScope.ActingAsVerified(RequestWithActiveRole(null), owner, "owner"));
    }
}
