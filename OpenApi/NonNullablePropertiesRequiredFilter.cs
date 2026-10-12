using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace consulcore_backend.OpenApi;

/// <summary>
/// 回應用到的型別：不可為 null 的屬性（int、string 等，依 nullable 標註判斷）一律列為 required。
/// API 的 JSON 一定會輸出這些欄位，前端型別就能寫成必填而不是 optional。
/// 請求 DTO 不套用：有預設值的欄位前端可以不送，必填與否由 [Required] 決定。
/// </summary>
public sealed class NonNullablePropertiesRequiredFilter : ISchemaFilter
{
    private static readonly Lazy<HashSet<Type>> ResponseTypes = new(CollectResponseTypes);

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema target || target.Properties is not { Count: > 0 }) return;
        if (!ResponseTypes.Value.Contains(context.Type)) return;

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

    /// <summary>從所有 controller action 的回傳型別出發，收集回應 JSON 會用到的型別（含巢狀屬性）。</summary>
    private static HashSet<Type> CollectResponseTypes()
    {
        var found = new HashSet<Type>();
        var actions = typeof(NonNullablePropertiesRequiredFilter).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        foreach (var action in actions) Visit(action.ReturnType, found);
        return found;
    }

    private static void Visit(Type type, HashSet<Type> found)
    {
        // Task<T>、ActionResult<T>、ApiResponse<T>、List<T> 等泛型都往型別參數走
        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments()) Visit(argument, found);
        }

        var ownType = type.Assembly == typeof(NonNullablePropertiesRequiredFilter).Assembly;
        if (!ownType || !type.IsClass || !found.Add(type)) return;

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            Visit(property.PropertyType, found);
    }
}
