using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ConsulCore.Tests.Integration;

/// <summary>
/// 以 API 建立測試資料的小工具。所有請求都附上 token 與 X-Active-Role，
/// 與前端實際送出的 header 一致。
/// </summary>
public class ApiTestClient(HttpClient http)
{
    public HttpClient Http { get; } = http;

    public async Task<string> RegisterOrganizationAsync(string organizationName, string ownerUsername)
    {
        var data = await PostOkAsync(null, null, "/api/auth/register", new
        {
            organizationName,
            username = ownerUsername,
            email = $"{ownerUsername}@test.local",
            password = "Owner123!",
            displayName = ownerUsername,
            phone = "0900000000",
        });
        return data.GetProperty("token").GetString()!;
    }

    public async Task<JsonElement> CreateUserAsync(string ownerToken, string username, string role) =>
        await PostOkAsync(ownerToken, "owner", "/api/users", new
        {
            username,
            email = $"{username}@test.local",
            password = "Pass123!",
            displayName = username,
            roles = new[] { role },
        });

    public async Task<JsonElement> LoginAsync(string username, string password) =>
        await PostOkAsync(null, null, "/api/auth/login", new { username, password });

    public async Task<string> LoginTokenAsync(string username, string password) =>
        (await LoginAsync(username, password)).GetProperty("token").GetString()!;

    public record ClientCredentials(int ClientProfileId, string Username, string Password);

    /// <summary>建立附帳號的個案，回傳建立時一次性提供的帳號與初始密碼（尚未改密碼）。</summary>
    public async Task<ClientCredentials> CreateClientAccountAsync(string ownerToken)
    {
        var client = await PostOkAsync(ownerToken, "owner", "/api/client-profiles", new
        {
            name = "測試個案",
            idNumber = "A123456789",
            dateOfBirth = "1995-06-15",
            createAccount = true,
        });
        var credentials = client.GetProperty("initialCredentials");
        return new ClientCredentials(
            client.GetProperty("id").GetInt32(),
            credentials.GetProperty("username").GetString()!,
            credentials.GetProperty("password").GetString()!);
    }

    /// <summary>建立個案帳號並完成首次改密碼，回傳可正常使用的個案 token。</summary>
    public async Task<string> CreateActiveClientTokenAsync(string ownerToken)
    {
        var account = await CreateClientAccountAsync(ownerToken);
        var firstToken = await LoginTokenAsync(account.Username, account.Password);
        await ReadDataAsync(await SendAsync(HttpMethod.Put, "/api/auth/change-password", firstToken, "client",
            new { currentPassword = account.Password, newPassword = "NewPass123!" }));
        return await LoginTokenAsync(account.Username, "NewPass123!");
    }

    public async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string url, string? token, string? activeRole, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (activeRole != null) request.Headers.Add("X-Active-Role", activeRole);
        if (body != null) request.Content = JsonContent.Create(body);
        return await Http.SendAsync(request);
    }

    public async Task<JsonElement> PostOkAsync(string? token, string? activeRole, string url, object body)
    {
        var response = await SendAsync(HttpMethod.Post, url, token, activeRole, body);
        return await ReadDataAsync(response);
    }

    /// <summary>分頁清單（稽核、收費、通知）：取出 ApiResponse.data.items</summary>
    public static async Task<JsonElement> ReadItemsAsync(HttpResponseMessage response) =>
        (await ReadDataAsync(response)).GetProperty("items");

    /// <summary>確認回應成功並取出 ApiResponse.data；失敗時把後端訊息帶進錯誤，方便 debug。</summary>
    public static async Task<JsonElement> ReadDataAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri} → {(int)response.StatusCode}: {text}");
        }
        return JsonDocument.Parse(text).RootElement.GetProperty("data").Clone();
    }
}
