using System.Net;

namespace ConsulCore.Tests.Integration;

// B8：BR-20 只放寬「唯讀」；心理師只能修改自己指派中的個案。
// 指派 / 移轉 / 建立時指定的心理師必須是同機構、具 therapist 角色的使用者。
public class ClientScopeTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    private async Task<(ClinicScenario clinic, string assigned, string other, int otherId)> SetUpAsync(string prefix)
    {
        var clinic = await ClinicScenario.CreateAsync(_api, prefix); // 個案指派給 {prefix}_ther
        var otherUser = await _api.CreateUserAsync(clinic.OwnerToken, $"{prefix}_other", "therapist");
        var assigned = await _api.LoginTokenAsync($"{prefix}_ther", "Pass123!");
        var other = await _api.LoginTokenAsync($"{prefix}_other", "Pass123!");
        return (clinic, assigned, other, otherUser.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task Therapist_cannot_update_a_client_not_assigned_to_them()
    {
        var (clinic, _, other, _) = await SetUpAsync("scope1");

        var response = await _api.SendAsync(HttpMethod.Put, $"/api/client-profiles/{clinic.ClientProfileId}",
            other, "therapist", new { phone = "0900999999" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Assigned_therapist_and_owner_can_update_the_client()
    {
        var (clinic, assigned, _, _) = await SetUpAsync("scope2");

        var byTherapist = await _api.SendAsync(HttpMethod.Put, $"/api/client-profiles/{clinic.ClientProfileId}",
            assigned, "therapist", new { phone = "0900111111" });
        var byOwner = await _api.SendAsync(HttpMethod.Put, $"/api/client-profiles/{clinic.ClientProfileId}",
            clinic.OwnerToken, "owner", new { phone = "0900222222" });

        Assert.Equal(HttpStatusCode.OK, byTherapist.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byOwner.StatusCode);
    }

    [Fact]
    public async Task Only_therapists_of_the_same_organization_can_be_assigned()
    {
        var (clinic, _, _, otherTherapistId) = await SetUpAsync("scope3");
        var admin = await _api.CreateUserAsync(clinic.OwnerToken, "scope3_admin", "admin");
        var foreignOwner = await _api.RegisterOrganizationAsync("別家診所", "scope3_foreign");
        var foreignTherapist = await _api.CreateUserAsync(foreignOwner, "scope3_foreign_ther", "therapist");

        var assignAdmin = await _api.SendAsync(HttpMethod.Post, $"/api/client-profiles/{clinic.ClientProfileId}/assign-therapist",
            clinic.OwnerToken, "owner", new { therapistUserId = admin.GetProperty("id").GetInt32() });
        var assignForeign = await _api.SendAsync(HttpMethod.Post, $"/api/client-profiles/{clinic.ClientProfileId}/assign-therapist",
            clinic.OwnerToken, "owner", new { therapistUserId = foreignTherapist.GetProperty("id").GetInt32() });
        var createWithForeign = await _api.SendAsync(HttpMethod.Post, "/api/client-profiles", clinic.OwnerToken, "owner",
            new { name = "新個案", createAccount = false, therapistUserId = foreignTherapist.GetProperty("id").GetInt32() });
        var transferToForeign = await _api.SendAsync(HttpMethod.Post,
            $"/api/client-profiles/{clinic.ClientProfileId}/transfer-therapist/{clinic.TherapistUserId}",
            clinic.OwnerToken, "owner", new { newTherapistUserId = foreignTherapist.GetProperty("id").GetInt32() });
        var assignValid = await _api.SendAsync(HttpMethod.Post, $"/api/client-profiles/{clinic.ClientProfileId}/assign-therapist",
            clinic.OwnerToken, "owner", new { therapistUserId = otherTherapistId });

        Assert.Equal(HttpStatusCode.BadRequest, assignAdmin.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, assignForeign.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, createWithForeign.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, transferToForeign.StatusCode);
        Assert.Equal(HttpStatusCode.OK, assignValid.StatusCode);
    }
}
