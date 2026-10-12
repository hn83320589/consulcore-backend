using System.Reflection;
using System.Text.Json;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace consulcore_backend.OpenApi;

/// <summary>
/// 不可為 null 的屬性（int、string 等，依 nullable 標註判斷）一律列為 required。
/// API 的 JSON 一定會輸出這些欄位，前端型別就能寫成必填而不是 optional。
/// </summary>
public sealed class NonNullablePropertiesRequiredFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema target || target.Properties is not { Count: > 0 }) return;

        // NullabilityInfoContext 不是 thread-safe，每次建立
        var nullability = new NullabilityInfoContext();
        foreach (var property in context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var name = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            if (!target.Properties.ContainsKey(name)) continue;
            if (nullability.Create(property).ReadState != NullabilityState.NotNull) continue;

            target.Required ??= new HashSet<string>();
            target.Required.Add(name);
        }
    }
}
