using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Swagger;

namespace ConsulCore.Tests.Integration;

/// <summary>
/// Phase 4：openapi.json 是前端產生 TS 型別的來源，必須完整描述每個端點的回應，且與程式碼同步。
/// 契約有異動時執行 `UPDATE_OPENAPI=1 dotnet test --filter OpenApiContractTests` 更新檔案並一起 commit。
/// </summary>
public class OpenApiContractTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private static readonly string[] FileContentTypes = ["text/csv", "application/pdf"];

    private async Task<string> GenerateAsync()
    {
        var document = factory.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
        var json = await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_0);
        return json.ReplaceLineEndings("\n") + "\n";
    }

    private static string CommittedSpecPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "consulcore-backend.csproj")))
            dir = dir.Parent;
        return Path.Combine(dir?.FullName ?? throw new DirectoryNotFoundException("找不到後端專案根目錄"), "openapi.json");
    }

    [Fact]
    public async Task Every_operation_documents_its_success_response_body()
    {
        using var spec = JsonDocument.Parse(await GenerateAsync());
        var undocumented = new List<string>();

        foreach (var path in spec.RootElement.GetProperty("paths").EnumerateObject())
        foreach (var operation in path.Value.EnumerateObject())
        {
            var ok = operation.Value.GetProperty("responses").GetProperty("200");
            var documented = ok.TryGetProperty("content", out var content) && content.EnumerateObject().Any(c =>
                (c.Name == "application/json" && c.Value.TryGetProperty("schema", out var schema)
                    && schema.TryGetProperty("$ref", out _))
                || FileContentTypes.Contains(c.Name));
            if (!documented) undocumented.Add($"{operation.Name.ToUpperInvariant()} {path.Name}");
        }

        Assert.True(undocumented.Count == 0,
            $"{undocumented.Count} 個端點的 200 回應沒有型別：\n" + string.Join("\n", undocumented));
    }

    [Fact]
    public async Task Bearer_token_and_active_role_header_are_declared()
    {
        using var spec = JsonDocument.Parse(await GenerateAsync());
        var schemes = spec.RootElement.GetProperty("components").GetProperty("securitySchemes");

        Assert.Equal("bearer", schemes.GetProperty("Bearer").GetProperty("scheme").GetString());
        Assert.Equal("X-Active-Role", schemes.GetProperty("ActiveRole").GetProperty("name").GetString());
    }

    // 狀態值在後端是常數類別（string），沒標註的話前端產生的型別只會是 string
    [Theory]
    [InlineData("AppointmentResponseDto", "status", new[] { "scheduled", "completed", "cancelled", "no_show" })]
    [InlineData("CreateAppointmentDto", "sessionType", new[] { "individual", "couple", "family" })]
    [InlineData("ScheduleSlotResponseDto", "status", new[] { "available", "reserved", "blocked", "cancelled" })]
    [InlineData("TherapistInfoDto", "status", new[] { "active", "transferred", "closed" })]
    [InlineData("UpdateClientMemoDto", "category", new[] { "personality", "trigger", "preference", "family", "other" })]
    public async Task Status_like_properties_list_their_allowed_values(string schema, string property, string[] expected)
    {
        using var spec = JsonDocument.Parse(await GenerateAsync());
        var values = spec.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty(schema).GetProperty("properties").GetProperty(property).GetProperty("enum")
            .EnumerateArray().Select(v => v.GetString()).ToArray();

        Assert.Equal(expected, values);
    }

    [Fact]
    public async Task Committed_openapi_json_matches_the_code()
    {
        var generated = await GenerateAsync();
        var path = CommittedSpecPath();
        if (Environment.GetEnvironmentVariable("UPDATE_OPENAPI") == "1")
            await File.WriteAllTextAsync(path, generated);

        Assert.True(File.Exists(path), "openapi.json 不存在，請以 UPDATE_OPENAPI=1 執行此測試產生");
        Assert.True(generated == await File.ReadAllTextAsync(path),
            "openapi.json 與程式碼不一致：API 契約有異動時，以 UPDATE_OPENAPI=1 重新產生並 commit");
    }
}
