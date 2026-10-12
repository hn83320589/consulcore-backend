using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace consulcore_backend.OpenApi;

/// <summary>
/// openapi.json 是前端產生 TS 型別的來源（tests 的 OpenApiContractTests 會檢查它與程式碼同步）。
/// </summary>
public static class SwaggerSetup
{
    public const string BearerScheme = "Bearer";
    public const string ActiveRoleScheme = "ActiveRole";

    public static void Configure(SwaggerGenOptions options)
    {
        // 讓 string 與 string? 在 schema 上可區分（nullable），再把回應型別中不可為 null 的屬性標成 required，
        // 前端產生的型別才不會每個欄位都是 optional
        options.SupportNonNullableReferenceTypes();
        options.SchemaFilter<NonNullablePropertiesRequiredFilter>();
        options.SchemaFilter<ConstantValuesSchemaFilter>();

        options.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
        });
        options.AddSecurityDefinition(ActiveRoleScheme, new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = "X-Active-Role",
            Description = "目前使用的角色，必須是 JWT roles 內擁有的角色",
        });
        options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(BearerScheme, document)] = [],
            [new OpenApiSecuritySchemeReference(ActiveRoleScheme, document)] = [],
        });
    }
}
