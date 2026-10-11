using System.Net;

namespace ConsulCore.Tests.Integration;

// 鎖住 PermissionHandler 的提權防護：X-Active-Role 必須是 JWT 內確實擁有的角色
public class PermissionPolicyTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    private async Task<(string ownerToken, string therapistToken)> SetUpAsync(string prefix)
    {
        var ownerToken = await _api.RegisterOrganizationAsync($"{prefix} 診所", $"{prefix}_owner");
        await _api.CreateUserAsync(ownerToken, $"{prefix}_ther", "therapist");
        var login = await _api.LoginAsync($"{prefix}_ther", "Pass123!");
        return (ownerToken, login.GetProperty("token").GetString()!);
    }

    [Fact]
    public async Task Owner_with_matching_role_header_can_manage_staff()
    {
        var (ownerToken, _) = await SetUpAsync("perm1");

        var response = await _api.SendAsync(HttpMethod.Get, "/api/users", ownerToken, "owner");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Therapist_forging_owner_header_is_forbidden()
    {
        var (_, therapistToken) = await SetUpAsync("perm2");

        var response = await _api.SendAsync(HttpMethod.Get, "/api/users", therapistToken, "owner");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Request_without_role_header_is_forbidden()
    {
        var (ownerToken, _) = await SetUpAsync("perm3");

        var response = await _api.SendAsync(HttpMethod.Get, "/api/users", ownerToken, activeRole: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_request_is_unauthorized()
    {
        var response = await _api.SendAsync(HttpMethod.Get, "/api/users", token: null, activeRole: "owner");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
