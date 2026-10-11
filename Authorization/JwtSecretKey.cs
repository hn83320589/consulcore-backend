using System.Text;

namespace consulcore_backend.Authorization;

/// <summary>
/// 啟動時檢查 JWT 簽章金鑰。appsettings.json 內的 placeholder 只供本機開發；
/// 其他環境若沿用它（等於公開金鑰），任何人都能偽造 token，因此直接拒絕啟動。
/// </summary>
public static class JwtSecretKey
{
    private const string PlaceholderPrefix = "DEV-ONLY-";
    private const int MinimumBytes = 32; // HS256 需要至少 256 bits

    public static string Validate(string? key, string environmentName)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new InvalidOperationException("未設定 JWT 金鑰，請以環境變數 Jwt__SecretKey 提供。");
        }

        if (Encoding.UTF8.GetByteCount(key) < MinimumBytes)
        {
            throw new InvalidOperationException(
                $"JWT 金鑰長度不足 {MinimumBytes} bytes，請以環境變數 Jwt__SecretKey 提供更長的隨機字串。");
        }

        var isDevelopment = string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);
        if (!isDevelopment && key.StartsWith(PlaceholderPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"環境 '{environmentName}' 仍在使用開發用 JWT 金鑰，請以環境變數 Jwt__SecretKey 提供正式金鑰。");
        }

        return key;
    }
}
