using System.Reflection;

namespace consulcore_backend.OpenApi;

/// <summary>
/// 標註「這個 string 欄位的值只會是某個常數類別裡的值」（例如 <c>AppointmentStatus</c>），
/// OpenAPI 會列出可用值，前端產生的型別就是字面值聯集而不是 string。只用於文件，不做輸入驗證。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ConstantValuesAttribute(Type constantsType) : Attribute
{
    public IReadOnlyList<string> Values { get; } = constantsType
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.IsLiteral && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToList();
}
