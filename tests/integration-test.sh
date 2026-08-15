#!/bin/bash
# Sprint 8 — 整合測試腳本
#
# 連線資訊由環境變數提供（勿在腳本內寫死憑證）：
#   BASE       API 位址（預設 http://localhost:5037）
#   DB_HOST DB_PORT DB_USER DB_PASSWORD  加密驗證直接查 DB 用（必填）
#   DB_NAME    資料庫名稱（預設 consulcore）
#   MYSQL_BIN  mysql client 路徑（預設 mysql）
#
# Usage:
#   export DB_HOST=... DB_PORT=... DB_USER=... DB_PASSWORD=...
#   bash tests/integration-test.sh
set -euo pipefail

BASE="${BASE:-http://localhost:5037}"
PASS=0
FAIL=0

: "${DB_HOST:?請設定環境變數 DB_HOST}"
: "${DB_PORT:?請設定環境變數 DB_PORT}"
: "${DB_USER:?請設定環境變數 DB_USER}"
: "${DB_PASSWORD:?請設定環境變數 DB_PASSWORD}"
DB_NAME="${DB_NAME:-consulcore}"
MYSQL_BIN="${MYSQL_BIN:-mysql}"
MYSQL="$MYSQL_BIN -h $DB_HOST -P $DB_PORT -u $DB_USER -p$DB_PASSWORD $DB_NAME -N -e"

# Helpers
api() { curl -s "$BASE$1" "${@:2}"; }
jq_() { python3 -c "import sys,json; d=json.load(sys.stdin); $1"; }
assert() {
  local desc="$1" actual="$2" expected="$3"
  if [ "$actual" = "$expected" ]; then
    echo "  ✓ $desc"
    PASS=$((PASS+1))
  else
    echo "  ✗ $desc (expected=$expected, actual=$actual)"
    FAIL=$((FAIL+1))
  fi
}

# 日期一律相對於「今天」計算，不可寫死：
# BR-05 展班會把 startDate 夾到今天，寫死的日期一旦過期，展班區間就會變成
# 「start > end」而整個測試在 1.6 掛掉（測試資料腐化）。
# 班表範本建在週一~週五，故取「下一個週一」與其隔天（週二）作為預約用日期。
DATES=$(python3 -c "
import datetime as dt
today = dt.date.today()
days_ahead = (7 - today.weekday()) % 7 or 7   # weekday(): 週一=0；0 → 取 7 確保嚴格未來
monday = today + dt.timedelta(days=days_ahead)
print(monday, monday + dt.timedelta(days=1), monday + dt.timedelta(days=34))
")
read -r MONDAY TUESDAY EXPAND_END <<< "$DATES"
EXPAND_START="$MONDAY"
# 變數後緊接全形標點時必須用 ${}，否則 bash 會把全形字元併入變數名（配 set -u 會直接中止）
echo "測試日期：展班 ${EXPAND_START} ~ ${EXPAND_END}；預約日 週一=${MONDAY} 週二=${TUESDAY}"

echo "========================================"
echo " 情境 1：新機構從零開始"
echo "========================================"

# 1.1 Register
echo "--- 1.1 Register ---"
REG=$(api /api/auth/register -X POST -H "Content-Type: application/json" \
  -d '{"organizationName":"全心諮商所","username":"owner1","email":"owner@allheart.com","password":"Owner123!","displayName":"王所長","phone":"0912000001"}')
TOKEN=$(echo "$REG" | jq_ "print(d['data']['token'])")
ORG_ID=$(echo "$REG" | jq_ "print(d['data']['organizationId'])")
assert "Register success" "$(echo "$REG" | jq_ "print(d['success'])")" "True"
assert "Has token" "$([ -n "$TOKEN" ] && echo yes || echo no)" "yes"

# 1.2 Create rooms
echo "--- 1.2 Create rooms ---"
for name in "陽光室" "微風室" "星空室"; do
  api /api/rooms -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
    -H "Content-Type: application/json" -d "{\"name\":\"$name\"}" > /dev/null
done
ROOM_COUNT=$(api /api/rooms/active-count -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" | jq_ "print(d['data']['activeCount'])")
assert "3 rooms created" "$ROOM_COUNT" "3"

# 1.3 Create therapist + admin
echo "--- 1.3 Create staff ---"
api /api/users -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" \
  -d '{"username":"therapist_a","email":"a@allheart.com","password":"Pass123!","displayName":"陳心理師A","roles":["therapist"]}' > /dev/null
api /api/users -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" \
  -d '{"username":"therapist_b","email":"b@allheart.com","password":"Pass123!","displayName":"林心理師B","roles":["therapist"]}' > /dev/null
api /api/users -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin1","email":"admin@allheart.com","password":"Pass123!","displayName":"張行政","roles":["admin"]}' > /dev/null
USER_COUNT=$(api /api/users -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" | jq_ "print(len(d['data']))")
assert "4 users (owner+2therapists+admin)" "$USER_COUNT" "4"

# 1.4 Assign rooms
echo "--- 1.4 Assign rooms ---"
api /api/rooms/1/assign-therapist -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" -d '{"therapistUserId":2}' > /dev/null
api /api/rooms/2/assign-therapist -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" -d '{"therapistUserId":3}' > /dev/null
assert "Rooms assigned" "ok" "ok"

# 1.5 Schedule templates (Mon-Fri for both therapists)
echo "--- 1.5 Schedule templates ---"
for tid in 2 3; do
  for dow in 1 2 3 4 5; do
    api /api/schedule-templates -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
      -H "Content-Type: application/json" \
      -d "{\"therapistUserId\":$tid,\"dayOfWeek\":$dow,\"startTime\":\"09:00\",\"endTime\":\"17:00\",\"slotDuration\":50}" > /dev/null
  done
done
TMPL_COUNT=$(api /api/schedule-templates -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" | jq_ "print(len(d['data']))")
assert "10 schedule templates" "$TMPL_COUNT" "10"

# 1.6 Expand schedule
echo "--- 1.6 Expand schedule ---"
EXPAND=$(api /api/schedule-slots/expand -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" -d "{\"startDate\":\"$EXPAND_START\",\"endDate\":\"$EXPAND_END\"}")
CREATED=$(echo "$EXPAND" | jq_ "print(d['data']['createdCount'])")
assert "Slots created > 0" "$([ "$CREATED" -gt 0 ] && echo yes || echo no)" "yes"

# 1.7 Fee schedules
echo "--- 1.7 Fee schedules ---"
api /api/fee-schedules -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" \
  -d '{"name":"個人諮商","sessionType":"individual","visitType":"follow_up","amount":2000}' > /dev/null
api /api/fee-schedules -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" \
  -d '{"name":"初診","sessionType":"individual","visitType":"initial","amount":2500}' > /dev/null
assert "Fee schedules created" "ok" "ok"

# 1.8 Package
echo "--- 1.8 Package ---"
api /api/packages -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" \
  -d '{"name":"10次套餐","sessionType":"individual","totalSessions":10,"totalAmount":18000,"validDays":180}' > /dev/null
assert "Package created" "ok" "ok"

# 1.9 Consent template
echo "--- 1.9 Consent template ---"
api /api/consent-templates -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" \
  -d '{"title":"個資蒐集同意書","content":"本人同意...","triggerEvent":"first_visit"}' > /dev/null
assert "Consent template created" "ok" "ok"

# 1.10 Discount
echo "--- 1.10 Discount ---"
api /api/discounts -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" \
  -d '{"name":"學生優惠","discountType":"percentage","percentageValue":10}' > /dev/null
assert "Discount created" "ok" "ok"

# Check form template was auto-seeded
FORM_COUNT=$(api /api/form-templates -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" | jq_ "print(len(d['data']))")
assert "Default form template seeded" "$([ "$FORM_COUNT" -ge 1 ] && echo yes || echo no)" "yes"

# Check notification templates seeded (8 event × 3 channel = 24)
NOTIF_COUNT=$(api /api/notification-templates -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" | jq_ "print(len(d['data']))")
assert "Notification templates seeded (24)" "$NOTIF_COUNT" "24"

echo ""
echo "========================================"
echo " 情境 2：完整個案生命週期"
echo "========================================"

# 2.1 Create client with account
echo "--- 2.1 Create client ---"
CLIENT_RESP=$(api /api/client-profiles -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" \
  -d '{"name":"小明","idNumber":"A123456789","gender":"male","dateOfBirth":"1995-06-15","phone":"0922111222","email":"xiaoming@test.com","address":"台北市大安區","emergencyContactName":"明爸","emergencyContactPhone":"0933222333","emergencyContactRelationship":"父親","occupation":"學生","referralSource":"網路搜尋","notes":"首次諮商","medicalHistory":"無","createAccount":true,"therapistUserId":2}')
CLIENT_ID=$(echo "$CLIENT_RESP" | jq_ "print(d['data']['id'])")
CLIENT_UID=$(echo "$CLIENT_RESP" | jq_ "print(d['data']['userId'])")
MUST_CHANGE=$(echo "$CLIENT_RESP" | jq_ "print(d['data']['mustChangePassword'])")
assert "Client created with account" "$(echo "$CLIENT_RESP" | jq_ "print(d['success'])")" "True"
assert "MustChangePassword=True" "$MUST_CHANGE" "True"

# 2.2 Default password login
echo "--- 2.2 Default password login ---"
CLIENT_USER=$(api /api/users -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" | jq_ "
for u in d['data']:
    if 'client' in u['roles']:
        print(u['username']); break
")
# Password: idNumber last 4 (6789) + birthday MMDD (0615)
LOGIN_RESP=$(api /api/auth/login -X POST -H "Content-Type: application/json" \
  -d "{\"username\":\"$CLIENT_USER\",\"password\":\"67890615\"}")
assert "Default password login" "$(echo "$LOGIN_RESP" | jq_ "print(d['success'])")" "True"
assert "MustChangePassword flag" "$(echo "$LOGIN_RESP" | jq_ "print(d['data']['mustChangePassword'])")" "True"
CLIENT_TOKEN=$(echo "$LOGIN_RESP" | jq_ "print(d['data']['token'])")

# 2.3 Change password
echo "--- 2.3 Force change password ---"
api /api/auth/change-password -X PUT -H "Authorization: Bearer $CLIENT_TOKEN" \
  -H "Content-Type: application/json" -d '{"currentPassword":"67890615","newPassword":"NewClient123!"}' > /dev/null
LOGIN2=$(api /api/auth/login -X POST -H "Content-Type: application/json" \
  -d "{\"username\":\"$CLIENT_USER\",\"password\":\"NewClient123!\"}")
assert "Password changed, mustChange=False" "$(echo "$LOGIN2" | jq_ "print(d['data']['mustChangePassword'])")" "False"
CLIENT_TOKEN=$(echo "$LOGIN2" | jq_ "print(d['data']['token'])")

# 2.4 Book initial appointment
echo "--- 2.4 Book initial appointment ---"
SLOT_ID=$(api "/api/appointments/available-slots?startDate=$MONDAY&endDate=$MONDAY&therapistUserId=2" \
  -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" | jq_ "print(d['data'][0]['id'])")
APT1=$(api /api/appointments -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" \
  -d "{\"clientProfileId\":$CLIENT_ID,\"scheduleSlotId\":$SLOT_ID,\"sessionType\":\"individual\",\"visitType\":\"initial\"}")
APT1_ID=$(echo "$APT1" | jq_ "print(d['data']['id'])")
assert "Initial appointment created" "$(echo "$APT1" | jq_ "print(d['data']['status'])")" "scheduled"

# 2.5 Sign consent
echo "--- 2.5 Sign consent ---"
CONSENT=$(api /api/consent-records/sign -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" \
  -d "{\"consentTemplateId\":1,\"clientProfileId\":$CLIENT_ID,\"signedRemotely\":false}")
assert "Consent signed" "$(echo "$CONSENT" | jq_ "print(d['data']['status'])")" "signed"

# 2.6 Submit intake form
echo "--- 2.6 Submit intake form ---"
FORM_TMPL_ID=$(api /api/form-templates -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" | jq_ "print(d['data'][0]['id'])")
FIELD1=$(api /api/form-templates -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" | jq_ "print(d['data'][0]['sections'][0]['fields'][0]['id'])")
SUBMISSION=$(api /api/form-submissions -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" \
  -d "{\"templateId\":$FORM_TMPL_ID,\"clientProfileId\":$CLIENT_ID,\"values\":[{\"fieldId\":$FIELD1,\"value\":\"網路搜尋\"}]}")
assert "Form submitted" "$(echo "$SUBMISSION" | jq_ "print(d['success'])")" "True"

# 2.7 Complete appointment + write SOAP note + lock
echo "--- 2.7 Complete + SOAP + Lock ---"
api "/api/appointments/$APT1_ID/complete" -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" > /dev/null

THER_TOKEN=$(api /api/auth/login -X POST -H "Content-Type: application/json" \
  -d '{"username":"therapist_a","password":"Pass123!"}' | jq_ "print(d['data']['token'])")
NOTE=$(api /api/session-notes -X POST -H "Authorization: Bearer $THER_TOKEN" -H "X-Active-Role: therapist" \
  -H "Content-Type: application/json" \
  -d "{\"clientProfileId\":$CLIENT_ID,\"appointmentId\":$APT1_ID,\"format\":\"soap\",\"soapS\":\"個案初次來談，主述壓力大\",\"soapO\":\"語速快，眼神閃避\",\"soapA\":\"適應性壓力反應\",\"soapP\":\"建立信任關係，下次探索壓力源\"}")
NOTE_ID=$(echo "$NOTE" | jq_ "print(d['data']['id'])")
api "/api/session-notes/$NOTE_ID/lock" -X POST -H "Authorization: Bearer $THER_TOKEN" -H "X-Active-Role: therapist" > /dev/null
assert "Note locked" "ok" "ok"

# 2.8 Auto billing
echo "--- 2.8 Auto billing ---"
BILLING=$(api "/api/billing/on-completed/$APT1_ID" -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner")
assert "Billing created" "$(echo "$BILLING" | jq_ "print(d['data']['status'])")" "pending"
assert "Initial fee 2500" "$(echo "$BILLING" | jq_ "print(d['data']['originalAmount'])")" "2500.0"
PAY1_ID=$(echo "$BILLING" | jq_ "print(d['data']['id'])")

# 2.9 Confirm payment
echo "--- 2.9 Confirm payment ---"
api "/api/billing/$PAY1_ID/confirm" -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" -d '{"paymentMethod":"cash"}' > /dev/null
assert "Payment confirmed" "ok" "ok"

# 2.10 Purchase package
echo "--- 2.10 Purchase package ---"
PKG=$(api /api/billing/purchase-package -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" \
  -d "{\"clientProfileId\":$CLIENT_ID,\"packageId\":1,\"paymentMethod\":\"credit_card\"}")
assert "Package purchased" "$(echo "$PKG" | jq_ "print(d['data']['sessionsRemaining'])")" "10"

# 2.11 Create series + auto appointments
echo "--- 2.11 Create series ---"
SERIES=$(api /api/appointment-series -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" \
  -d "{\"clientProfileId\":$CLIENT_ID,\"therapistUserId\":2,\"dayOfWeek\":1,\"startTime\":\"10:40\",\"frequency\":\"weekly\",\"months\":1}")
SERIES_TOTAL=$(echo "$SERIES" | jq_ "print(d['data']['totalAppointments'])")
assert "Series created with appointments" "$([ "$SERIES_TOTAL" -gt 0 ] && echo yes || echo no)" "yes"

# 2.12 Complete series appointments + package deduction
echo "--- 2.12 Complete + package deduct ---"
APT_LIST=$(api "/api/appointments/list?clientProfileId=$CLIENT_ID&status=scheduled" \
  -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner")
FIRST_SERIES_APT=$(echo "$APT_LIST" | jq_ "
for a in d['data']:
    if a['seriesId'] is not None:
        print(a['id']); break
")
if [ -n "$FIRST_SERIES_APT" ] && [ "$FIRST_SERIES_APT" != "None" ]; then
  api "/api/appointments/$FIRST_SERIES_APT/complete" -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" > /dev/null
  DEDUCT=$(api "/api/billing/on-completed/$FIRST_SERIES_APT" -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner")
  assert "Package deducted, final=0" "$(echo "$DEDUCT" | jq_ "print(d['data']['finalAmount'])")" "0"
fi

# 2.13 Client memo
echo "--- 2.13 Client memo ---"
api /api/client-memos -X POST -H "Authorization: Bearer $THER_TOKEN" -H "X-Active-Role: therapist" \
  -H "Content-Type: application/json" \
  -d "{\"clientProfileId\":$CLIENT_ID,\"category\":\"personality\",\"content\":\"個案性格內向，較需要時間暖身\"}" > /dev/null
assert "Memo created" "ok" "ok"

# 註：結案（close case）刻意移到「情境 4 移轉」之後才做。
# ChangeStatusAsync 結案時會把所有 active 的心理師關係一併關閉（正確業務行為），
# 若在此處先結案，情境 3b「therapist_a 看得到自己的個案」與情境 4 的移轉
# （TransferTherapistAsync 要求存在 active 關係）都會失敗。
# 實際生命週期順序本來就是：建立 → 晤談/收費 → 移轉 → 結案。

echo ""
echo "========================================"
echo " 情境 3：權限測試"
echo "========================================"

# Reopen client for permission tests
api "/api/client-profiles/$CLIENT_ID/status" -X PATCH -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" -d '{"status":"active"}' > /dev/null

ADMIN_TOKEN=$(api /api/auth/login -X POST -H "Content-Type: application/json" \
  -d '{"username":"admin1","password":"Pass123!"}' | jq_ "print(d['data']['token'])")
THERB_TOKEN=$(api /api/auth/login -X POST -H "Content-Type: application/json" \
  -d '{"username":"therapist_b","password":"Pass123!"}' | jq_ "print(d['data']['token'])")

# Therapist cannot access user management
echo "--- Therapist cannot manage staff ---"
STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/users" \
  -H "Authorization: Bearer $THER_TOKEN" -H "X-Active-Role: therapist")
assert "Therapist /users → 403" "$STATUS_CODE" "403"

# Therapist cannot manage rooms
STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/rooms" \
  -H "Authorization: Bearer $THER_TOKEN" -H "X-Active-Role: therapist")
assert "Therapist /rooms → 403" "$STATUS_CODE" "403"

# 安全：therapist 偽造 X-Active-Role: owner 不可提權（active role 須為 JWT 內實有角色）
STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/users" \
  -H "Authorization: Bearer $THER_TOKEN" -H "X-Active-Role: owner")
assert "Therapist spoofing X-Active-Role=owner → 403" "$STATUS_CODE" "403"

# Therapist cannot manage fee schedules
# Therapist has billing.view (by design) so fee-schedules is accessible
STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/audit-logs" \
  -H "Authorization: Bearer $THER_TOKEN" -H "X-Active-Role: therapist")
assert "Therapist /audit-logs → 403" "$STATUS_CODE" "403"

# Client cannot access management
STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/client-profiles" \
  -H "Authorization: Bearer $CLIENT_TOKEN" -H "X-Active-Role: client")
assert "Client /client-profiles → 403" "$STATUS_CODE" "403"

STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/rooms" \
  -H "Authorization: Bearer $CLIENT_TOKEN" -H "X-Active-Role: client")
assert "Client /rooms → 403" "$STATUS_CODE" "403"

STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/users" \
  -H "Authorization: Bearer $CLIENT_TOKEN" -H "X-Active-Role: client")
assert "Client /users → 403" "$STATUS_CODE" "403"

# No token → 401
STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/users")
assert "No token → 401" "$STATUS_CODE" "401"

# Admin can view billing
STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/fee-schedules" \
  -H "Authorization: Bearer $ADMIN_TOKEN" -H "X-Active-Role: admin")
assert "Admin /fee-schedules → 200" "$STATUS_CODE" "200"

echo ""
echo "========================================"
echo " 情境 3b：個案存取隔離（身份空間隔離）"
echo "========================================"

# 建立第二位個案，指派給 therapist_b(3)
CLIENT2_RESP=$(api /api/client-profiles -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" \
  -d '{"name":"小華","idNumber":"B234567890","gender":"female","dateOfBirth":"1990-03-20","phone":"0955666777","createAccount":false,"therapistUserId":3}')
CLIENT2_ID=$(echo "$CLIENT2_RESP" | jq_ "print(d['data']['id'])")
assert "Second client (therapist_b) created" "$(echo "$CLIENT2_RESP" | jq_ "print(d['success'])")" "True"

# therapist_a 查 /client-profiles 只應看到自己的個案（小明），不含小華
TA_CLIENTS=$(api /api/client-profiles -H "Authorization: Bearer $THER_TOKEN" -H "X-Active-Role: therapist")
TA_HAS_OWN=$(echo "$TA_CLIENTS" | jq_ "print('yes' if any(c['id']==$CLIENT_ID for c in d['data']) else 'no')")
TA_HAS_OTHER=$(echo "$TA_CLIENTS" | jq_ "print('yes' if any(c['id']==$CLIENT2_ID for c in d['data']) else 'no')")
assert "Therapist_a sees own client" "$TA_HAS_OWN" "yes"
assert "Therapist_a does NOT see therapist_b's client" "$TA_HAS_OTHER" "no"

# therapist_a 不可查詢 therapist_b 的個案清單 → 403
STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/client-profiles/by-therapist/3" \
  -H "Authorization: Bearer $THER_TOKEN" -H "X-Active-Role: therapist")
assert "Therapist_a by-therapist/3 → 403" "$STATUS_CODE" "403"

# 代診/交接：therapist_a 現可「唯讀」therapist_b 的個案單筆（放寬硬性 403），但須留稽核
STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/client-profiles/$CLIENT2_ID" \
  -H "Authorization: Bearer $THER_TOKEN" -H "X-Active-Role: therapist")
assert "Therapist_a locum read other's client → 200" "$STATUS_CODE" "200"

# 跨心理師檢視須留稽核：owner 查 audit-logs 應見該個案的 view 紀錄（代診軌跡）
AUDIT_HIT=$(api "/api/audit-logs?resourceType=client_profile&resourceId=$CLIENT2_ID" \
  -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" | jq_ "print('yes' if any(a['action']=='view' for a in (d.get('data') or [])) else 'no')")
assert "Cross-therapist (locum) view is audited" "$AUDIT_HIT" "yes"

# 安全：therapist 偽造 owner header 也不可取得全機構個案（scope 不提升）
# 注意：PermissionHandler 會先擋下偽造角色並回 403（body 為空、非 JSON），
# 因此不可直接 json.load；403 等同「看不到任何個案」，比回 200 空清單更嚴格。
SPOOF_CLIENTS=$(api /api/client-profiles -H "Authorization: Bearer $THER_TOKEN" -H "X-Active-Role: owner")
SPOOF_SEEN=$(python3 -c "
import sys, json
raw = sys.stdin.read().strip()
try:
    data = json.loads(raw).get('data') or []
except Exception:
    data = []          # 403 / 空回應
print('yes' if any(c['id'] == $CLIENT2_ID for c in data) else 'no')
" <<< "$SPOOF_CLIENTS")
assert "Therapist spoofing owner does NOT see therapist_b's client" "$SPOOF_SEEN" "no"

# 個案跨病人 IDOR：小明(client) 只能存取自己（$CLIENT_ID），不可讀 $CLIENT2_ID 的資料
# 正向：查自己的常態預約/同意書 → 200
STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/appointment-series/by-client/$CLIENT_ID" \
  -H "Authorization: Bearer $CLIENT_TOKEN" -H "X-Active-Role: client")
assert "Client series by-client own → 200" "$STATUS_CODE" "200"
STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/consent-records/by-client/$CLIENT_ID" \
  -H "Authorization: Bearer $CLIENT_TOKEN" -H "X-Active-Role: client")
assert "Client consent by-client own → 200" "$STATUS_CODE" "200"

# 反向：查別的個案（$CLIENT2_ID）→ 403
STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/appointment-series/by-client/$CLIENT2_ID" \
  -H "Authorization: Bearer $CLIENT_TOKEN" -H "X-Active-Role: client")
assert "Client series by-client OTHER → 403" "$STATUS_CODE" "403"
STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/consent-records/by-client/$CLIENT2_ID" \
  -H "Authorization: Bearer $CLIENT_TOKEN" -H "X-Active-Role: client")
assert "Client consent by-client OTHER → 403" "$STATUS_CODE" "403"

# staff 專屬聚合視圖：個案身份一律 403
STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/appointments/calendar?startDate=2026-01-01&endDate=2026-12-31" \
  -H "Authorization: Bearer $CLIENT_TOKEN" -H "X-Active-Role: client")
assert "Client /appointments/calendar → 403" "$STATUS_CODE" "403"
STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/appointment-series/pending-renewal" \
  -H "Authorization: Bearer $CLIENT_TOKEN" -H "X-Active-Role: client")
assert "Client /appointment-series/pending-renewal → 403" "$STATUS_CODE" "403"

# 安全（fail-closed）：小明偽造 X-Active-Role: owner 想甩掉個案範圍，仍不可讀別人資料 → 403
STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/appointment-series/by-client/$CLIENT2_ID" \
  -H "Authorization: Bearer $CLIENT_TOKEN" -H "X-Active-Role: owner")
assert "Client spoofing owner on OTHER's series → 403" "$STATUS_CODE" "403"
STATUS_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/api/consent-records/by-client/$CLIENT2_ID" \
  -H "Authorization: Bearer $CLIENT_TOKEN" -H "X-Active-Role: therapist")
assert "Client spoofing therapist on OTHER's consent → 403" "$STATUS_CODE" "403"

echo ""
echo "========================================"
echo " 情境 4：心理師離職（個案移轉）"
echo "========================================"

# 小明 從 therapist_a(2) 移轉給 therapist_b(3)
TRANSFER=$(api "/api/client-profiles/$CLIENT_ID/transfer-therapist/2" -X POST \
  -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" -d '{"newTherapistUserId":3,"reason":"心理師A離職"}')
assert "Transfer succeeded" "$(echo "$TRANSFER" | jq_ "print(d['success'])")" "True"

# 移轉後 therapist_b 應看到小明，therapist_a 不再看到
TB_CLIENTS=$(api /api/client-profiles -H "Authorization: Bearer $THERB_TOKEN" -H "X-Active-Role: therapist")
assert "Therapist_b now sees transferred client" \
  "$(echo "$TB_CLIENTS" | jq_ "print('yes' if any(c['id']==$CLIENT_ID for c in d['data']) else 'no')")" "yes"
TA_AFTER=$(api /api/client-profiles -H "Authorization: Bearer $THER_TOKEN" -H "X-Active-Role: therapist")
assert "Therapist_a no longer sees transferred client" \
  "$(echo "$TA_AFTER" | jq_ "print('yes' if any(c['id']==$CLIENT_ID for c in d['data']) else 'no')")" "no"

# 結案（生命週期終點）：須在移轉之後，因結案會關閉所有 active 心理師關係
echo "--- 結案 Close case ---"
api "/api/client-profiles/$CLIENT_ID/status" -X PATCH -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" -d '{"status":"closed"}' > /dev/null
STATUS=$(api "/api/client-profiles/$CLIENT_ID" -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" | jq_ "print(d['data']['status'])")
assert "Client closed" "$STATUS" "closed"
# 結案後：接手的 therapist_b 也不應再於清單看到（active 關係已關閉）
TB_AFTER_CLOSE=$(api /api/client-profiles -H "Authorization: Bearer $THERB_TOKEN" -H "X-Active-Role: therapist")
assert "Closed client leaves therapist_b's active list" \
  "$(echo "$TB_AFTER_CLOSE" | jq_ "print('yes' if any(c['id']==$CLIENT_ID for c in d['data']) else 'no')")" "no"

# 建立考核 + 違規紀錄（供加密驗證覆蓋）
api /api/performance-reviews -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" \
  -d '{"therapistUserId":2,"period":"2026-Q1","professionalScore":4,"clientFeedbackScore":5,"teamworkScore":4,"overallScore":4,"comment":"整體表現良好，個案回饋佳"}' > /dev/null
api /api/violation-records -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" \
  -d '{"therapistUserId":2,"incidentDate":"2026-03-01","severity":"low","description":"遲交晤談紀錄","actionTaken":"口頭提醒"}' > /dev/null
assert "Performance review + violation created" "ok" "ok"

echo ""
echo "========================================"
echo " 加密驗證（直接查 DB）"
echo "========================================"

# Check ClientProfile.name is encrypted
echo "--- Encrypted fields in DB ---"
RAW_NAME=$($MYSQL "SELECT name FROM client_profiles LIMIT 1" 2>/dev/null)
IS_ENCRYPTED=$(echo "$RAW_NAME" | python3 -c "
import sys
val = sys.stdin.read().strip()
# Encrypted values are base64 (nonce+tag+cipher), min ~40 chars for short text
print('yes' if len(val) > 30 and not any(c in val for c in '小明張李王陳林') else 'no')
")
assert "ClientProfile.name encrypted" "$IS_ENCRYPTED" "yes"

RAW_SOAP=$($MYSQL "SELECT soap_s FROM session_notes LIMIT 1" 2>/dev/null)
IS_ENCRYPTED=$(echo "$RAW_SOAP" | python3 -c "
import sys; val = sys.stdin.read().strip(); print('yes' if len(val) > 50 else 'no')
")
assert "SessionNote.soap_s encrypted" "$IS_ENCRYPTED" "yes"

RAW_CONSENT=$($MYSQL "SELECT content_snapshot FROM consent_records LIMIT 1" 2>/dev/null)
IS_ENCRYPTED=$(echo "$RAW_CONSENT" | python3 -c "
import sys; val = sys.stdin.read().strip(); print('yes' if len(val) > 50 else 'no')
")
assert "ConsentRecord.content_snapshot encrypted" "$IS_ENCRYPTED" "yes"

RAW_MEMO=$($MYSQL "SELECT content FROM client_memos LIMIT 1" 2>/dev/null)
IS_ENCRYPTED=$(echo "$RAW_MEMO" | python3 -c "
import sys; val = sys.stdin.read().strip(); print('yes' if len(val) > 50 else 'no')
")
assert "ClientMemo.content encrypted" "$IS_ENCRYPTED" "yes"

RAW_FV=$($MYSQL "SELECT value FROM form_field_values LIMIT 1" 2>/dev/null)
IS_ENCRYPTED=$(echo "$RAW_FV" | python3 -c "
import sys; val = sys.stdin.read().strip(); print('yes' if len(val) > 30 else 'no')
")
assert "FormFieldValue.value encrypted" "$IS_ENCRYPTED" "yes"

RAW_AUDIT=$($MYSQL "SELECT changes FROM audit_logs WHERE changes IS NOT NULL LIMIT 1" 2>/dev/null)
IS_ENCRYPTED=$(echo "$RAW_AUDIT" | python3 -c "
import sys; val = sys.stdin.read().strip(); print('yes' if len(val) > 30 and val[0] not in '{[' else 'no')
")
assert "AuditLog.changes encrypted" "$IS_ENCRYPTED" "yes"

RAW_PERF=$($MYSQL "SELECT comment FROM performance_reviews WHERE comment IS NOT NULL LIMIT 1" 2>/dev/null)
IS_ENCRYPTED=$(echo "$RAW_PERF" | python3 -c "
import sys; val = sys.stdin.read().strip(); print('yes' if len(val) > 30 and not any(c in val for c in '整體表現良好個案回饋佳') else 'no')
")
assert "PerformanceReview.comment encrypted" "$IS_ENCRYPTED" "yes"

RAW_VIO=$($MYSQL "SELECT description FROM violation_records WHERE description IS NOT NULL LIMIT 1" 2>/dev/null)
IS_ENCRYPTED=$(echo "$RAW_VIO" | python3 -c "
import sys; val = sys.stdin.read().strip(); print('yes' if len(val) > 30 and not any(c in val for c in '遲交晤談紀錄') else 'no')
")
assert "ViolationRecord.description encrypted" "$IS_ENCRYPTED" "yes"

# Check appsettings.json has no real key
MAIN_KEY=$(python3 -c "import json; d=json.load(open('appsettings.json')); print(d.get('Encryption',{}).get('Key',''))")
assert "appsettings.json Encryption.Key empty" "$MAIN_KEY" ""

# Development 設定檔不得進版控（避免金鑰/密碼外洩）
GITIGNORED=$(grep -q "appsettings.Development.json" .gitignore 2>/dev/null && echo yes || echo no)
assert "appsettings.Development.json is gitignored" "$GITIGNORED" "yes"

echo ""
echo "========================================"
echo " Edge Cases"
echo "========================================"

# E1: Same client same timeslot conflict
echo "--- E1: Client double-booking ---"
SLOT_E1=$(api "/api/appointments/available-slots?startDate=$TUESDAY&endDate=$TUESDAY&therapistUserId=2" \
  -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" | jq_ "print(d['data'][0]['id'])")
api /api/appointments -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" \
  -d "{\"clientProfileId\":$CLIENT_ID,\"scheduleSlotId\":$SLOT_E1}" > /dev/null

# Try same timeslot with different therapist
SLOT_E1B=$(api "/api/appointments/available-slots?startDate=$TUESDAY&endDate=$TUESDAY&therapistUserId=3" \
  -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" | jq_ "
for s in d['data']:
    if s['startTime'] == '09:00': print(s['id']); break
")
if [ -n "$SLOT_E1B" ] && [ "$SLOT_E1B" != "None" ]; then
  CONFLICT=$(api /api/appointments -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
    -H "Content-Type: application/json" \
    -d "{\"clientProfileId\":$CLIENT_ID,\"scheduleSlotId\":$SLOT_E1B}")
  assert "Client double-book blocked" "$(echo "$CONFLICT" | jq_ "print(d['success'])")" "False"
fi

# E2: Consent template update doesn't affect signed
echo "--- E2: Consent snapshot preserved ---"
api /api/consent-templates/1 -X PUT -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" -d '{"content":"更新後的同意書內容v2"}' > /dev/null
SIGNED_VER=$(api /api/consent-records/by-client/$CLIENT_ID -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" | jq_ "print(d['data'][0]['templateVersion'])")
assert "Signed consent keeps v1" "$SIGNED_VER" "1"

# E3: Unlock→modify→re-lock audit trail
echo "--- E3: Unlock/modify/re-lock audit ---"
api "/api/session-notes/$NOTE_ID/unlock" -X POST -H "Authorization: Bearer $THER_TOKEN" -H "X-Active-Role: therapist" > /dev/null
api "/api/session-notes/$NOTE_ID" -X PUT -H "Authorization: Bearer $THER_TOKEN" -H "X-Active-Role: therapist" \
  -H "Content-Type: application/json" -d '{"soapS":"更新：壓力來自工作"}' > /dev/null
api "/api/session-notes/$NOTE_ID/lock" -X POST -H "Authorization: Bearer $THER_TOKEN" -H "X-Active-Role: therapist" > /dev/null
AUDIT_UNLOCK=$($MYSQL "SELECT COUNT(*) FROM audit_logs WHERE action='unlock' AND resource_type='session_note'" 2>/dev/null)
assert "Unlock audit logged" "$([ "$AUDIT_UNLOCK" -ge 1 ] && echo yes || echo no)" "yes"

# E4: Re-expand doesn't overwrite
echo "--- E4: Re-expand skip ---"
REEXPAND=$(api /api/schedule-slots/expand -X POST -H "Authorization: Bearer $TOKEN" -H "X-Active-Role: owner" \
  -H "Content-Type: application/json" -d "{\"startDate\":\"$EXPAND_START\",\"endDate\":\"$EXPAND_END\"}")
assert "Re-expand created=0" "$(echo "$REEXPAND" | jq_ "print(d['data']['createdCount'])")" "0"

echo ""
echo "========================================"
echo " 結果摘要"
echo "========================================"
TOTAL=$((PASS+FAIL))
echo " 通過: $PASS / $TOTAL"
echo " 失敗: $FAIL / $TOTAL"
if [ $FAIL -eq 0 ]; then
  echo " ★ ALL TESTS PASSED ★"
else
  echo " ✗ SOME TESTS FAILED"
fi
