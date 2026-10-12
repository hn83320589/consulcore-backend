using System.Text.Json;

namespace ConsulCore.Tests.Integration;

// P5：搜尋改成「先只解密姓名與身分證比對，命中的才載入完整資料」之前，鎖住目前的搜尋行為
public class ClientSearchTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    private async Task<int> CreateClientAsync(string owner, string name, string idNumber, int? therapistUserId = null) =>
        (await _api.PostOkAsync(owner, "owner", "/api/client-profiles", new
        {
            name, idNumber, createAccount = false, therapistUserId,
        })).GetProperty("id").GetInt32();

    private async Task<List<string>> SearchAsync(string token, string role, string query)
    {
        var data = await ApiTestClient.ReadDataAsync(
            await _api.SendAsync(HttpMethod.Get, $"/api/client-profiles/search?{query}", token, role));
        return data.EnumerateArray().Select(c => c.GetProperty("name").GetString()!).OrderBy(n => n, StringComparer.Ordinal).ToList();
    }

    [Fact]
    public async Task Search_matches_partial_name_case_insensitively_and_id_suffix()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "search1", expand: false); // 已有「王小明」（無身分證）
        await CreateClientAsync(clinic.OwnerToken, "陳小明", "B223456781");
        await CreateClientAsync(clinic.OwnerToken, "Amy Lin", "C123459999");
        var deleted = await CreateClientAsync(clinic.OwnerToken, "林小明", "D123456781");
        await _api.SendAsync(HttpMethod.Delete, $"/api/client-profiles/{deleted}", clinic.OwnerToken, "owner");

        Assert.Equal(["王小明", "陳小明"], await SearchAsync(clinic.OwnerToken, "owner", "keyword=小明"));
        Assert.Equal(["Amy Lin"], await SearchAsync(clinic.OwnerToken, "owner", "keyword=amy"));
        Assert.Equal(["陳小明"], await SearchAsync(clinic.OwnerToken, "owner", "idNumberLast4=6781"));
        Assert.Equal(["陳小明"], await SearchAsync(clinic.OwnerToken, "owner", "keyword=小明&idNumberLast4=6781"));
        Assert.Empty(await SearchAsync(clinic.OwnerToken, "owner", "keyword=不存在"));
        Assert.Equal(["Amy Lin", "王小明", "陳小明"], await SearchAsync(clinic.OwnerToken, "owner", ""));
    }

    [Fact]
    public async Task Therapist_only_finds_assigned_clients()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "search2", expand: false); // 王小明 指派給 search2_ther
        await CreateClientAsync(clinic.OwnerToken, "陳小明", "B223456781");      // 未指派
        var therapist = await _api.LoginTokenAsync("search2_ther", "Pass123!");

        Assert.Equal(["王小明"], await SearchAsync(therapist, "therapist", "keyword=小明"));
    }

    [Fact]
    public async Task Search_returns_full_client_details()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "search3", expand: false);
        var data = await ApiTestClient.ReadDataAsync(await _api.SendAsync(HttpMethod.Get,
            "/api/client-profiles/search?keyword=王", clinic.OwnerToken, "owner"));

        var client = Assert.Single(data.EnumerateArray());
        Assert.Equal(clinic.ClientProfileId, client.GetProperty("id").GetInt32());
        Assert.Equal(clinic.TherapistUserId, Assert.Single(client.GetProperty("therapists").EnumerateArray())
            .GetProperty("therapistUserId").GetInt32());
    }
}
