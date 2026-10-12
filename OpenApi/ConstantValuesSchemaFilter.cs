using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace consulcore_backend.OpenApi;

/// <summary>把 <see cref="ConstantValuesAttribute"/> 轉成 schema 的 enum。</summary>
public sealed class ConstantValuesSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        var attribute = context.MemberInfo?.GetCustomAttribute<ConstantValuesAttribute>();
        if (attribute == null || schema is not OpenApiSchema target) return;

        target.Enum = attribute.Values.Select(v => (JsonNode)JsonValue.Create(v)).ToList();
    }
}
