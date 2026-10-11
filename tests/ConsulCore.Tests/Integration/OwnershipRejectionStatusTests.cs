using System.Net;

namespace ConsulCore.Tests.Integration;

// B5：「不是建立者」原本回 401，前端會當成登入過期而把心理師登出。
// 這是已登入但不被允許的操作，應回 403；401 只留給「你是誰」的認證失敗。
public class OwnershipRejectionStatusTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    private async Task<(ClinicScenario clinic, string author, string other)> SetUpAsync(string prefix)
    {
        var clinic = await ClinicScenario.CreateAsync(_api, prefix);
        await _api.CreateUserAsync(clinic.OwnerToken, $"{prefix}_other", "therapist");
        var author = await _api.LoginTokenAsync($"{prefix}_ther", "Pass123!");
        var other = await _api.LoginTokenAsync($"{prefix}_other", "Pass123!");
        return (clinic, author, other);
    }

    [Fact]
    public async Task Another_therapist_changing_a_session_note_gets_403()
    {
        var (clinic, author, other) = await SetUpAsync("own1");
        var note = await _api.PostOkAsync(author, "therapist", "/api/session-notes",
            new { clientProfileId = clinic.ClientProfileId, format = "soap", soapS = "主述" });
        var id = note.GetProperty("id").GetInt32();

        var update = await _api.SendAsync(HttpMethod.Put, $"/api/session-notes/{id}", other, "therapist", new { soapS = "竄改" });
        var lockIt = await _api.SendAsync(HttpMethod.Post, $"/api/session-notes/{id}/lock", other, "therapist");
        var delete = await _api.SendAsync(HttpMethod.Delete, $"/api/session-notes/{id}", other, "therapist");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, lockIt.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Another_therapist_changing_a_client_memo_gets_403()
    {
        var (clinic, author, other) = await SetUpAsync("own2");
        var memo = await _api.PostOkAsync(author, "therapist", "/api/client-memos",
            new { clientProfileId = clinic.ClientProfileId, category = "other", content = "備忘" });
        var id = memo.GetProperty("id").GetInt32();

        var update = await _api.SendAsync(HttpMethod.Put, $"/api/client-memos/{id}", other, "therapist", new { content = "竄改" });
        var delete = await _api.SendAsync(HttpMethod.Delete, $"/api/client-memos/{id}", other, "therapist");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Another_therapist_changing_a_phrase_template_gets_403()
    {
        var (_, author, other) = await SetUpAsync("own3");
        var phrase = await _api.PostOkAsync(author, "therapist", "/api/phrase-templates",
            new { category = "general", title = "開場", content = "今天想談什麼？" });
        var id = phrase.GetProperty("id").GetInt32();

        var update = await _api.SendAsync(HttpMethod.Put, $"/api/phrase-templates/{id}", other, "therapist", new { title = "竄改" });
        var delete = await _api.SendAsync(HttpMethod.Delete, $"/api/phrase-templates/{id}", other, "therapist");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Wrong_current_password_when_changing_password_is_a_400_not_a_401()
    {
        var (_, author, _) = await SetUpAsync("own4");

        var response = await _api.SendAsync(HttpMethod.Put, "/api/auth/change-password", author, "therapist",
            new { currentPassword = "wrong-pass", newPassword = "NewPass123!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
