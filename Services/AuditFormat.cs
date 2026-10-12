using System.Globalization;

namespace consulcore_backend.Services;

/// <summary>稽核描述的共用格式：描述會顯示在稽核頁並匯出給機構負責人，要用一般人讀得懂的寫法。</summary>
public static class AuditFormat
{
    /// <summary>新台幣金額，例：2,500 元（不帶小數，與伺服器語系無關）</summary>
    public static string Money(decimal amount) => $"{amount.ToString("N0", CultureInfo.InvariantCulture)} 元";
}
