# 資料庫

開發環境使用 **SQLite**（EF Core 10）。正式環境未來可能改用 **MySQL / MariaDB**，因此程式碼保持不綁特定資料庫，
由設定 `Database:Provider` 決定 provider（目前只啟用 `Sqlite`，其他值會在啟動時直接報錯）。

## 開發環境（SQLite）

- 設定在 `appsettings.json`：`Database:Provider = Sqlite`、`ConnectionStrings:DefaultConnection = Data Source=App_Data/consulcore.db`。
- 相對路徑以專案根目錄（content root）為準，資料夾不存在會自動建立；`*.db`、`*.db-wal`、`*.db-shm` 已被 gitignore（內含加密個資）。
- 首次 `dotnet run` 會自動套用 migration（`Migrations/Sqlite/`）並執行 `DatabaseSeeder`。
- **重置資料庫**：停止 API，刪除 `App_Data/consulcore.db*`，再 `dotnet run`。
- 直接查看資料：`sqlite3 App_Data/consulcore.db`（加密欄位會是密文）。

新增 migration：

```bash
dotnet ef migrations add <Name> -o Migrations/Sqlite
```

## 寫程式時保持 provider 中立

| 規則 | 原因 |
|------|------|
| 不寫 raw SQL（`FromSqlRaw`、`ExecuteSql*`、`migrationBuilder.Sql`） | SQL 方言不同 |
| 不用 `[Column(TypeName = "...")]` / `HasColumnType` 指定資料庫型別；decimal 用 `[Precision(p, s)]`，字串長度用 `[MaxLength]` | 型別名稱是 provider 專屬；`[Precision]` 在 MySQL 對應 `decimal(p,s)`，在 SQLite 以 TEXT 精確儲存 |
| provider 專屬設定用 `Database.IsSqlite()` 等判斷包起來（例：`AppDbContext` 的 `NOCASE`） | 另一個 provider 不認得該設定 |
| decimal 欄位的排序、大小比較、`Min/Max` 先查回記憶體再做 | SQLite provider 無法在 SQL 端翻譯這些操作（`Sum` 可以） |

帳號 / Email 不分大小寫：MySQL / MariaDB 預設 collation 本來就不分大小寫；SQLite 由 `AppDbContext` 指定 `NOCASE` 達到相同行為。

## 正式環境改用 MySQL / MariaDB（上線時執行）

目前沒有 MySQL 的 migration。正式環境上線時還沒有資料，因此直接產生一份全新的 `InitialCreate` 即可，不需要轉移舊 migration。

1. **選 provider 套件**（版本須與 EF Core 10 相容）：
   - MySQL：`MySql.EntityFrameworkCore`（Oracle 官方，已有 10.x）。
   - MariaDB：Pomelo `Pomelo.EntityFrameworkCore.MySql`。截至 2026-10 只有 9.0.0（綁 EF Core 9），需等 EF Core 10 版本，或先確認 Oracle provider 對所用 MariaDB 版本的相容性。
2. **分開兩套 migration**：同一個 assembly 內，同一個 `DbContext` 不能有兩套 migration。依 EF Core 文件〈Migrations with Multiple Providers〉擇一：
   - 新增 migration 專案（例：`ConsulCore.Migrations.MySql`），連線時指定 `MigrationsAssembly`；或
   - 新增衍生的 `MySqlAppDbContext`，各自有 migration 資料夾。
3. **擴充 `Data/DatabaseProvider.cs`**：接受 `MySql`，`Program.cs` 依 provider 呼叫對應的 `UseXxx`。
4. **產生 MySQL 的 `InitialCreate`**，檢查 decimal 欄位為 `decimal(p,s)`、Username/Email 的 collation 不分大小寫。
5. **用容器跑一次完整驗證**：以 Docker 起 MySQL / MariaDB，API 指向它，執行 `tests/integration-test.sh`（腳本目前直接查 SQLite 驗證加密，需加一個查 MySQL 的分支）以及自動化測試。
6. 正式環境的連線字串、`Encryption:Key`、`Jwt:SecretKey` 一律用環境變數或 secret manager 注入（見 `SECURITY.md`）。
