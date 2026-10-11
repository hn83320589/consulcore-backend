using System.Security.Claims;
using consulcore_backend.Authorization;

namespace ConsulCore.Tests.Unit;

// R1：controller 原本有 67 處 int.Parse(User.FindFirst("...")!.Value)，roles 解析有 3 份
public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal User(params (string type, string value)[] claims) =>
        new(new ClaimsIdentity(claims.Select(c => new Claim(c.type, c.value)), "test"));

    [Fact]
    public void Reads_user_and_organization_ids()
    {
        var user = User((JwtClaims.UserId, "7"), (JwtClaims.OrganizationId, "3"));

        Assert.Equal(7, user.GetUserId());
        Assert.Equal(3, user.GetOrganizationId());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("abc")]
    public void Missing_or_invalid_id_claim_fails_with_a_clear_message(string? value)
    {
        var user = value == null ? User() : User((JwtClaims.UserId, value));

        var ex = Assert.Throws<InvalidOperationException>(() => user.GetUserId());
        Assert.Contains(JwtClaims.UserId, ex.Message);
    }

    [Fact]
    public void Parses_roles_claim_json_array()
    {
        Assert.Equal(["therapist", "client"], User((JwtClaims.Roles, "[\"therapist\",\"client\"]")).GetJwtRoles());
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-json")]
    [InlineData("{\"role\":\"owner\"}")]
    public void Malformed_roles_claim_yields_no_roles(string value)
    {
        Assert.Empty(User((JwtClaims.Roles, value)).GetJwtRoles());
    }
}
