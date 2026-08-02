namespace consulcore_backend.Services;

// CSV 欄位安全輸出：防止試算表公式注入（Excel/Sheets）並正確跳脫逗號/引號/換行。
public static class CsvWriter
{
    // 以這些字元開頭的值會被 Excel 當成公式執行，前置單引號使其變成純文字
    private const string DangerousLeadingChars = "=+-@\t\r";

    public static string Field(object? value)
    {
        var s = value?.ToString() ?? string.Empty;
        if (s.Length > 0 && DangerousLeadingChars.IndexOf(s[0]) >= 0)
            s = "'" + s;
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }

    public static string Row(params object?[] fields)
        => string.Join(",", fields.Select(Field));
}
