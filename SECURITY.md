# 安全與憑證管理

## 機密存放原則

- **絕不**將 DB 密碼、AES 加密金鑰、JWT 金鑰寫入版控。
- SQLite 資料庫檔（`App_Data/*.db*`）內含加密個資，已被 `.gitignore` 排除，也不要另外備份到共用位置。
- 本地開發：放在 `appsettings.Development.json`（已被 `.gitignore` 排除）或環境變數。
- 部署：一律用環境變數 / secret manager 注入。
- 追蹤檔中只保留 `*.example` 佔位範本。

## ⚠️ 需輪替的憑證

以下憑證曾以明文出現在版控檔（`appsettings.Development.json`、`prompts/sprint-1.md`、
`tests/integration-test.sh`）與建置輸出，已從追蹤檔清除，但**視為已洩漏，請輪替**。
（原本的遠端 MySQL 已於 2026-10 關閉、改用本機 SQLite，資料庫憑證不再適用。）

| 憑證 | 影響 | 輪替方式 |
|------|------|---------|
| AES-256 加密金鑰 | 可解密所有加密欄位 | 見下方步驟 1（注意資料相容性） |
| JWT SecretKey | 可偽造任意 token | 見下方步驟 2 |

### 正式環境資料庫憑證（改用 MySQL / MariaDB 時）

應用程式使用**最小權限的專用帳號**（只給 `SELECT, INSERT, UPDATE, DELETE`），不要用 root；連線字串只用環境變數注入。

### 步驟 1：輪替 AES 加密金鑰

> ⚠️ **破壞性**：加密金鑰用於欄位層級加密。若資料庫已有以舊金鑰加密的資料，
> 直接換金鑰會導致既有密文**無法解密**。

- **開發 / 可拋棄資料**：刪除 `App_Data/consulcore.db*` → 產生新金鑰 → 重新 `dotnet run`（自動 migrate + seed）。
- **正式資料需保留**：需寫一次性 re-encryption（以舊金鑰解密 → 以新金鑰加密）後再切換。

產生新金鑰：

```bash
python3 -c "import os, base64; print(base64.b64encode(os.urandom(32)).decode())"
```

設定：`Encryption__Key=<新的 Base64 32-byte 金鑰>`

### 步驟 2：輪替 JWT SecretKey

換掉後既有 token 全部失效，使用者需重新登入（可接受）。

```bash
# 產生至少 32 字元的隨機字串
python3 -c "import secrets; print(secrets.token_urlsafe(48))"
```

設定：`Jwt__SecretKey=<新金鑰>`。`appsettings.json` 內的 `DEV-ONLY-` placeholder 只供本機開發；非 Development 環境沿用它或金鑰短於 32 bytes 時，API 會拒絕啟動（`Authorization/JwtSecretKey.cs`）。

## 執行整合測試

測試腳本直接讀本機 SQLite 檔驗證加密，不需要任何資料庫憑證：

```bash
bash tests/integration-test.sh            # 預設 DB_PATH=App_Data/consulcore.db
```
