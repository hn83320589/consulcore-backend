using System.Net;

namespace ConsulCore.Tests.Integration;

// 鎖住 BR-01 / 多租戶隔離：Global Query Filter 讓別的機構的個案「看不到」
public class TenantIsolationTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    [Fact]
    public async Task Owner_cannot_read_or_list_another_organizations_client()
    {
        var ownerA = await _api.RegisterOrganizationAsync("A 診所", "iso_owner_a");
        var ownerB = await _api.RegisterOrganizationAsync("B 診所", "iso_owner_b");
        var client = await _api.PostOkAsync(ownerA, "owner", "/api/client-profiles", new
        {
            name = "A 的個案",
            idNumber = "A123456789",
            dateOfBirth = "1995-06-15",
            createAccount = false,
        });
        var clientId = client.GetProperty("id").GetInt32();

        var byId = await _api.SendAsync(HttpMethod.Get, $"/api/client-profiles/{clientId}", ownerB, "owner");
        var list = await ApiTestClient.ReadDataAsync(
            await _api.SendAsync(HttpMethod.Get, "/api/client-profiles", ownerB, "owner"));

        Assert.Equal(HttpStatusCode.NotFound, byId.StatusCode);
        Assert.DoesNotContain(list.EnumerateArray(), c => c.GetProperty("id").GetInt32() == clientId);
    }
}
