using Microsoft.Data.Sqlite;

namespace consulcore_backend.Data;

/// <summary>
/// 依設定 Database:Provider 決定資料庫。目前只啟用 Sqlite（開發用）；
/// 正式環境改 MySQL / MariaDB 的步驟見 docs/database.md。
/// </summary>
public static class DatabaseProvider
{
    public const string Sqlite = "Sqlite";

    /// <summary>
    /// 驗證 provider，並把 SQLite 的相對路徑轉成以 content root 為基準的絕對路徑
    /// （SQLite 預設以行程工作目錄解析相對路徑，從別的目錄啟動會建出另一個空 DB）。
    /// 連線字串為空時回傳空字串，維持「未設定就不連 DB」的既有行為。
    /// </summary>
    public static string ResolveConnectionString(
        string? provider, string? connectionString, string contentRootPath)
    {
        var name = string.IsNullOrWhiteSpace(provider) ? Sqlite : provider;
        if (!string.Equals(name, Sqlite, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Database:Provider '{name}' 尚未啟用。目前只支援 '{Sqlite}'；" +
                "改用 MySQL / MariaDB 需先加入 provider 套件並產生對應 migration，見 docs/database.md。");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return string.Empty;
        }

        var csb = new SqliteConnectionStringBuilder(connectionString);
        if (csb.DataSource != ":memory:" && !Path.IsPathRooted(csb.DataSource))
        {
            csb.DataSource = Path.Combine(contentRootPath, csb.DataSource);
        }

        var directory = Path.GetDirectoryName(csb.DataSource);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return csb.ToString();
    }
}
