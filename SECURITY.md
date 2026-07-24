# 安全與憑證管理

## 機密存放原則

- **絕不**將 DB 密碼、AES 加密金鑰、JWT 金鑰寫入版控。
- 本地開發：放在 `appsettings.Development.json`（已被 `.gitignore` 排除）或環境變數。
- 部署：一律用環境變數 / secret manager 注入。
- 追蹤檔中只保留 `*.example` 佔位範本。

## ⚠️ 需輪替的憑證

以下憑證曾以明文出現在版控檔（`appsettings.Development.json`、`prompts/sprint-1.md`、
`tests/integration-test.sh`）與建置輸出，已從追蹤檔清除，但**視為已洩漏，請輪替**：

| 憑證 | 影響 | 輪替方式 |
|------|------|---------|
| MySQL root 密碼 | 資料庫完全存取 | 見下方步驟 1 |
| AES-256 加密金鑰 | 可解密所有加密欄位 | 見下方步驟 2（注意資料相容性） |
| JWT SecretKey | 可偽造任意 token | 見下方步驟 3 |

### 步驟 1：輪替資料庫憑證

建議同時**改用最小權限的專用帳號**，不要用 root 跑應用程式：

```sql
-- 於 MySQL 執行
CREATE USER 'consulcore_app'@'%' IDENTIFIED BY '<新的強密碼>';
GRANT SELECT, INSERT, UPDATE, DELETE ON consulcore.* TO 'consulcore_app'@'%';
FLUSH PRIVILEGES;
-- 之後輪替 root 密碼
ALTER USER 'root'@'%' IDENTIFIED BY '<新的 root 密碼>';
```

更新連線字串（環境變數或 gitignored 的 `appsettings.Development.json`）：

```
ConnectionStrings__DefaultConnection=Server=...;Port=...;Database=consulcore;User=consulcore_app;Password=<新密碼>;
```

### 步驟 2：輪替 AES 加密金鑰

> ⚠️ **破壞性**：加密金鑰用於欄位層級加密。若資料庫已有以舊金鑰加密的資料，
> 直接換金鑰會導致既有密文**無法解密**。

- **開發 / 可拋棄資料**：清空資料庫 → 產生新金鑰 → 重新 migrate + seed。
- **正式資料需保留**：需寫一次性 re-encryption（以舊金鑰解密 → 以新金鑰加密）後再切換。

產生新金鑰：

```bash
python3 -c "import os, base64; print(base64.b64encode(os.urandom(32)).decode())"
```

設定：`Encryption__Key=<新的 Base64 32-byte 金鑰>`

### 步驟 3：輪替 JWT SecretKey

換掉後既有 token 全部失效，使用者需重新登入（可接受）。

```bash
# 產生至少 32 字元的隨機字串
python3 -c "import secrets; print(secrets.token_urlsafe(48))"
```

設定：`Jwt__SecretKey=<新金鑰>`；並移除 `appsettings.json` 內的 dev placeholder。

## 執行整合測試

測試腳本的 DB 連線改由環境變數提供（不再寫死）：

```bash
export DB_HOST=... DB_PORT=... DB_USER=... DB_PASSWORD=...
bash tests/integration-test.sh
```
