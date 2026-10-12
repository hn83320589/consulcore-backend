#!/usr/bin/env python3
"""灌入示範資料：一間診所、人員、個案、排班、預約、晤談紀錄、收費、考核。

全部透過 API 建立（與正式操作走同一套業務規則與稽核），只用 Python 標準函式庫。
需要 API 已啟動且資料庫為空（示範帳號已存在時會直接失敗）：

    dotnet run &
    python3 scripts/seed-demo.py            # BASE 預設 http://localhost:5037

示範帳號的密碼一律為 DEMO_PASSWORD，只供本機展示使用。
"""
import datetime as dt
import json
import os
import sys
import urllib.error
import urllib.request

BASE = os.environ.get("BASE", "http://localhost:5037").rstrip("/") + "/api"
DEMO_PASSWORD = "Demo1234!"


class ApiError(Exception):
    pass


def call(method, path, token=None, role=None, body=None):
    """送出請求並回傳 ApiResponse.data；失敗時帶上後端訊息直接中止。"""
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(BASE + path, data=data, method=method)
    req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", f"Bearer {token}")
    if role:
        req.add_header("X-Active-Role", role)
    try:
        with urllib.request.urlopen(req) as resp:
            return json.loads(resp.read() or b"null")["data"]
    except urllib.error.HTTPError as e:
        text = e.read().decode(errors="replace")
        try:
            detail = json.loads(text).get("message") or text
        except ValueError:
            detail = text
        raise ApiError(f"{method} {path} → {e.code}：{detail}") from None


def login(username, password=DEMO_PASSWORD):
    return call("POST", "/auth/login", body={"username": username, "password": password})["token"]


def main():
    today = dt.date.today()

    # ── 機構與擁有者 ────────────────────────────
    owner = call("POST", "/auth/register", body={
        "organizationName": "晴光心理諮商所", "username": "demo_owner", "email": "owner@demo.local",
        "password": DEMO_PASSWORD, "displayName": "王曉晴", "phone": "0912000001",
    })["token"]

    def o(method, path, body=None):
        return call(method, path, owner, "owner", body)

    # ── 人員與診間 ──────────────────────────────
    therapists = {}
    for username, name in [("demo_chen", "陳怡君"), ("demo_lin", "林志豪")]:
        therapists[username] = o("POST", "/users", {
            "username": username, "email": f"{username}@demo.local", "password": DEMO_PASSWORD,
            "displayName": name, "roles": ["therapist"],
        })["id"]
    o("POST", "/users", {
        "username": "demo_admin", "email": "demo_admin@demo.local", "password": DEMO_PASSWORD,
        "displayName": "張雅婷", "roles": ["admin"],
    })
    chen, lin = therapists["demo_chen"], therapists["demo_lin"]

    rooms = [o("POST", "/rooms", {"name": n})["id"] for n in ["晨曦室", "森林室", "海洋室"]]
    o("POST", f"/rooms/{rooms[0]}/assign-therapist", {"therapistUserId": chen})
    o("POST", f"/rooms/{rooms[1]}/assign-therapist", {"therapistUserId": lin})

    # ── 排班：週一到週五 09:00–17:00，展開今天起五週 ─────
    for tid in (chen, lin):
        for dow in range(1, 6):
            o("POST", "/schedule-templates", {
                "therapistUserId": tid, "dayOfWeek": dow,
                "startTime": "09:00", "endTime": "17:00", "slotDuration": 50,
            })
    o("POST", "/schedule-slots/expand", {
        "startDate": today.isoformat(), "endDate": (today + dt.timedelta(days=35)).isoformat(),
    })

    # ── 收費設定與同意書 ────────────────────────
    o("POST", "/fee-schedules", {"name": "個別諮商", "sessionType": "individual", "visitType": "follow_up", "amount": 2000})
    o("POST", "/fee-schedules", {"name": "初談", "sessionType": "individual", "visitType": "initial", "amount": 2500})
    package_id = o("POST", "/packages", {
        "name": "10 次諮商套餐", "sessionType": "individual", "totalSessions": 10, "totalAmount": 18000, "validDays": 180,
    })["id"]
    o("POST", "/discounts", {"name": "學生優惠", "discountType": "percentage", "percentageValue": 10})
    consent_id = o("POST", "/consent-templates", {
        "title": "諮商服務暨個人資料蒐集同意書",
        "content": "本人了解諮商服務的性質、保密原則與例外情形，並同意本所依個人資料保護法蒐集、處理及利用本人資料。",
        "triggerEvent": "first_visit",
    })["id"]

    # ── 個案（前兩位有個案端帳號）─────────────────
    clients = [
        ("李品妤", "female", "1996-03-12", chen, True, "client.li@demo.local"),
        ("吳承翰", "male", "1989-11-02", chen, True, None),
        ("黃思涵", "female", "2001-07-25", chen, False, None),
        ("周柏宇", "male", "1993-01-18", lin, False, None),
        ("蔡雨潔", "female", "1998-09-09", lin, False, None),
        ("許家豪", "male", "1985-05-30", lin, False, None),
    ]
    client_ids, client_accounts = [], []
    for i, (name, gender, dob, therapist, with_account, email) in enumerate(clients):
        created = o("POST", "/client-profiles", {
            "name": name, "gender": gender, "dateOfBirth": dob, "idNumber": f"A1{i:02d}456789",
            "phone": f"09221110{i:02d}", "email": email, "therapistUserId": therapist,
            "createAccount": with_account, "referralSource": "網路搜尋",
        })
        client_ids.append(created["id"])
        if with_account:
            creds = created["initialCredentials"]
            first = login(creds["username"], creds["password"])
            call("PUT", "/auth/change-password", first, "client",
                 {"currentPassword": creds["password"], "newPassword": DEMO_PASSWORD})
            client_accounts.append(creds["username"])

    # ── 預約：每位個案兩次（本週與下週），最早的幾筆標為完成 ──
    def free_slots(therapist):
        end = (today + dt.timedelta(days=14)).isoformat()
        return o("GET", f"/appointments/available-slots?startDate={today}&endDate={end}&therapistUserId={therapist}")

    slots = {chen: free_slots(chen), lin: free_slots(lin)}
    appointments = []  # (appointment_id, client_index, therapist)
    for idx, (_, _, _, therapist, _, _) in enumerate(clients):
        pool = slots[therapist]
        for visit in ("initial", "follow_up"):
            # 每次取間隔幾格的時段，讓行事曆分散而不是全擠在同一天早上
            slot = pool.pop(min(len(pool) - 1, 7 * len(appointments) % len(pool)))
            apt = o("POST", "/appointments", {
                "clientProfileId": client_ids[idx], "scheduleSlotId": slot["id"],
                "sessionType": "individual", "visitType": visit,
            })
            appointments.append((apt["id"], idx, therapist, slot["date"]))

    appointments.sort(key=lambda a: a[3])
    tokens = {chen: login("demo_chen"), lin: login("demo_lin")}
    soap = [
        ("近期工作壓力大，睡眠品質下降", "語速偏快，談到主管時情緒明顯", "適應性壓力反應", "建立信任關係，下次探索壓力來源"),
        ("與伴侶溝通常起衝突", "能具體描述衝突情境，反思能力佳", "溝通模式議題", "練習「我訊息」表達，下次回顧"),
        ("對未來方向感到焦慮", "情緒穩定，對諮商目標清楚", "生涯轉換期焦慮", "進行價值澄清活動"),
    ]
    for n, (apt_id, idx, therapist, _) in enumerate(appointments[:3]):
        o("POST", f"/appointments/{apt_id}/complete")
        s, ob, a, p = soap[n]
        note = call("POST", "/session-notes", tokens[therapist], "therapist", {
            "clientProfileId": client_ids[idx], "appointmentId": apt_id, "format": "soap",
            "soapS": s, "soapO": ob, "soapA": a, "soapP": p,
        })
        if n < 2:
            call("POST", f"/session-notes/{note['id']}/lock", tokens[therapist], "therapist")
        payment = o("POST", f"/billing/on-completed/{apt_id}")
        if n < 2:
            o("POST", f"/billing/{payment['id']}/confirm", {"paymentMethod": "cash"})
    o("POST", f"/appointments/{appointments[3][0]}/cancel", {"note": "個案臨時有事，改約下週"})

    # ── 其他：同意書、套餐、常態預約、備忘、考核 ─────
    for cid in client_ids[:3]:
        o("POST", "/consent-records/sign", {"consentTemplateId": consent_id, "clientProfileId": cid, "signedRemotely": False})
    o("POST", "/billing/purchase-package", {"clientProfileId": client_ids[0], "packageId": package_id, "paymentMethod": "credit_card"})
    o("POST", "/appointment-series", {
        "clientProfileId": client_ids[0], "therapistUserId": chen, "dayOfWeek": 3,
        "startTime": "14:00", "frequency": "weekly", "months": 1,
    })
    call("POST", "/client-memos", tokens[chen], "therapist", {
        "clientProfileId": client_ids[0], "category": "personality", "content": "個性內向，需要較多時間暖身；偏好下午時段。",
    })
    period = f"{today.year}-Q{(today.month - 1) // 3 + 1}"
    o("POST", "/performance-reviews", {
        "therapistUserId": chen, "period": period, "professionalScore": 5, "clientFeedbackScore": 4,
        "teamworkScore": 4, "overallScore": 5, "comment": "紀錄完整準時，個案回饋穩定。",
    })
    o("POST", "/violation-records", {
        "therapistUserId": lin, "incidentDate": today.isoformat(), "severity": "low",
        "description": "晤談紀錄超過 48 小時未完成。", "actionTaken": "已口頭提醒",
    })

    print("示範資料建立完成")
    print("  機構：晴光心理諮商所")
    print("  後台：demo_owner（擁有者）、demo_admin（行政）、demo_chen / demo_lin（心理師）")
    print(f"  個案端：{', '.join(client_accounts)}")
    print(f"  密碼一律為 {DEMO_PASSWORD}")


if __name__ == "__main__":
    try:
        main()
    except ApiError as e:
        sys.exit(f"建立示範資料失敗：{e}")
    except urllib.error.URLError as e:
        sys.exit(f"連不上 API（{BASE}）：{e.reason}。請先 dotnet run")
