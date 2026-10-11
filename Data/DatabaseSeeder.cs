using consulcore_backend.Authorization;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Permissions.AnyAsync())
            return;

        var permissions = new List<Permission>
        {
            new() { Code = Permissions.ClientView, Name = "檢視個案", Description = "檢視個案資料" },
            new() { Code = Permissions.ClientCreate, Name = "建立個案", Description = "建立新個案" },
            new() { Code = Permissions.ClientUpdate, Name = "編輯個案", Description = "編輯個案資料" },
            new() { Code = Permissions.ClientDelete, Name = "刪除個案", Description = "刪除個案資料" },
            new() { Code = Permissions.ClientManage, Name = "管理個案", Description = "管理個案指派與移轉" },
            new() { Code = Permissions.AppointmentView, Name = "檢視預約", Description = "檢視預約紀錄" },
            new() { Code = Permissions.AppointmentCreate, Name = "建立預約", Description = "建立新預約" },
            new() { Code = Permissions.AppointmentUpdate, Name = "編輯預約", Description = "編輯預約資料" },
            new() { Code = Permissions.AppointmentDelete, Name = "刪除預約", Description = "刪除預約紀錄" },
            new() { Code = Permissions.ScheduleView, Name = "檢視排班", Description = "檢視排班資料" },
            new() { Code = Permissions.ScheduleCreate, Name = "建立排班", Description = "建立排班模板" },
            new() { Code = Permissions.ScheduleUpdate, Name = "編輯排班", Description = "編輯排班資料" },
            new() { Code = Permissions.ScheduleDelete, Name = "刪除排班", Description = "刪除排班模板" },
            new() { Code = Permissions.SessionView, Name = "檢視晤談", Description = "檢視晤談紀錄" },
            new() { Code = Permissions.SessionCreate, Name = "建立晤談", Description = "建立晤談紀錄" },
            new() { Code = Permissions.SessionUpdate, Name = "編輯晤談", Description = "編輯晤談紀錄" },
            new() { Code = Permissions.SessionLock, Name = "鎖定晤談", Description = "鎖定/解鎖晤談紀錄" },
            new() { Code = Permissions.ConsentView, Name = "檢視同意書", Description = "檢視同意書" },
            new() { Code = Permissions.ConsentCreate, Name = "建立同意書", Description = "建立同意書模板" },
            new() { Code = Permissions.ConsentUpdate, Name = "編輯同意書", Description = "編輯同意書模板" },
            new() { Code = Permissions.ConsentVoid, Name = "作廢同意書", Description = "作廢已簽署的同意書" },
            new() { Code = Permissions.BillingView, Name = "檢視收費", Description = "檢視收費紀錄" },
            new() { Code = Permissions.BillingCreate, Name = "建立收費", Description = "建立收費紀錄" },
            new() { Code = Permissions.BillingUpdate, Name = "編輯收費", Description = "編輯收費紀錄" },
            new() { Code = Permissions.BillingRefund, Name = "退費", Description = "處理退費" },
            new() { Code = Permissions.NotificationManage, Name = "管理通知", Description = "管理通知模板與設定" },
            new() { Code = Permissions.StaffManage, Name = "管理人員", Description = "管理機構內人員帳號" },
            new() { Code = Permissions.RoomManage, Name = "管理診間", Description = "管理診間設定" },
            new() { Code = Permissions.AuditView, Name = "檢視稽核", Description = "檢視稽核紀錄" },
            new() { Code = Permissions.PerformanceView, Name = "檢視考核", Description = "檢視考核紀錄" },
            new() { Code = Permissions.PerformanceManage, Name = "管理考核", Description = "管理考核與違規紀錄" },
            new() { Code = Permissions.OrgSettings, Name = "機構設定", Description = "管理機構設定" },
        };

        db.Permissions.AddRange(permissions);
        await db.SaveChangesAsync();
    }

    public static async Task SeedDefaultFormTemplateAsync(AppDbContext db, int orgId, int createdBy)
    {
        var exists = await db.FormTemplates
            .IgnoreQueryFilters()
            .AnyAsync(t => t.OrganizationId == orgId && t.IsSystemDefault);
        if (exists) return;

        var template = new FormTemplate
        {
            OrganizationId = orgId,
            Name = "初診評估表",
            Type = FormTemplateType.IntakeAssessment,
            IsSystemDefault = true,
            CreatedBy = createdBy,
        };
        db.FormTemplates.Add(template);
        await db.SaveChangesAsync();

        var sections = new (string Title, int Order, (string Label, string Type, bool Req, string? Opts, string? Ph)[] Fields)[]
        {
            ("基本資料", 1, [
                ("轉介來源", "select", true, "[\"自行前來\",\"醫師轉介\",\"親友介紹\",\"網路搜尋\",\"其他\"]", null),
                ("目前職業", "text", false, null, "請填寫目前職業"),
                ("教育程度", "select", false, "[\"國小\",\"國中\",\"高中/職\",\"大學/專科\",\"碩士\",\"博士\"]", null),
            ]),
            ("主訴問題", 2, [
                ("主要困擾", "textarea", true, null, "請描述目前最困擾您的問題"),
                ("困擾持續時間", "select", true, "[\"一週內\",\"一個月內\",\"三個月內\",\"半年內\",\"一年以上\"]", null),
                ("困擾嚴重程度", "scale", true, null, null),
            ]),
            ("心理健康史", 3, [
                ("過去是否接受過心理諮商/治療", "radio", true, "[\"是\",\"否\"]", null),
                ("若是，請說明", "textarea", false, null, "請簡述過去的諮商經驗"),
                ("目前是否服用精神科藥物", "radio", true, "[\"是\",\"否\"]", null),
                ("藥物名稱與劑量", "textarea", false, null, "若有服藥，請列出藥物名稱"),
            ]),
            ("身體健康", 4, [
                ("目前是否有身體疾病", "radio", true, "[\"是\",\"否\"]", null),
                ("疾病說明", "textarea", false, null, "請說明目前的身體狀況"),
                ("睡眠品質", "select", true, "[\"良好\",\"偶爾失眠\",\"經常失眠\",\"嚴重失眠\"]", null),
                ("運動習慣", "select", false, "[\"每週3次以上\",\"每週1-2次\",\"偶爾\",\"幾乎不運動\"]", null),
            ]),
            ("家庭與社交", 5, [
                ("家庭組成", "textarea", false, null, "請簡述家庭成員"),
                ("家庭關係品質", "scale", false, null, null),
                ("社交支持程度", "scale", false, null, null),
                ("目前主要壓力來源", "checkbox", false, "[\"工作\",\"家庭\",\"感情\",\"經濟\",\"人際\",\"健康\",\"學業\",\"其他\"]", null),
            ]),
            ("風險評估", 6, [
                ("過去是否有自我傷害行為", "radio", true, "[\"是\",\"否\"]", null),
                ("過去是否有自殺意念", "radio", true, "[\"是\",\"否\"]", null),
                ("目前是否有自殺意念", "radio", true, "[\"是\",\"否\"]", null),
                ("是否有傷害他人的想法", "radio", true, "[\"是\",\"否\"]", null),
            ]),
            ("諮商期待", 7, [
                ("您希望透過諮商達到什麼目標", "textarea", true, null, "請描述您對諮商的期待"),
                ("您偏好的諮商方式", "select", false, "[\"認知行為\",\"心理動力\",\"人本取向\",\"不確定/皆可\"]", null),
                ("其他備註", "textarea", false, null, "其他您希望心理師知道的事項"),
            ]),
        };

        foreach (var (title, order, fields) in sections)
        {
            var section = new FormSection
            {
                TemplateId = template.Id,
                Title = title,
                SortOrder = order,
                IsRequired = order == 6,
            };
            db.FormSections.Add(section);
            await db.SaveChangesAsync();

            var fieldOrder = 1;
            foreach (var (label, fieldType, required, options, placeholder) in fields)
            {
                db.FormFields.Add(new FormField
                {
                    SectionId = section.Id,
                    Label = label,
                    FieldType = fieldType,
                    Options = options,
                    IsRequired = required,
                    SortOrder = fieldOrder++,
                    Placeholder = placeholder,
                    ValidationRules = fieldType == "scale" ? "{\"min\":1,\"max\":10}" : null,
                });
            }
            await db.SaveChangesAsync();
        }
    }

    public static async Task SeedNotificationTemplatesAsync(AppDbContext db, int orgId)
    {
        var exists = await db.NotificationTemplates
            .IgnoreQueryFilters()
            .AnyAsync(t => t.OrganizationId == orgId);
        if (exists) return;

        var events = new (string Type, string Subject, string Body, bool Required)[]
        {
            (NotificationEventType.AccountCreated, "帳號已建立", "您好 {{client_name}}，您的諮商帳號已建立。預設密碼為身分證後4碼+生日MMDD，請盡快登入修改。", true),
            (NotificationEventType.AppointmentCreated, "預約確認", "{{client_name}} 您好，您的預約已確認：{{date}} {{time}} 與 {{therapist_name}}。", true),
            (NotificationEventType.AppointmentCancelled, "預約取消", "{{client_name}} 您好，您於 {{date}} {{time}} 的預約已取消。", true),
            (NotificationEventType.AppointmentReminder, "預約提醒", "{{client_name}} 您好，提醒您明天 {{time}} 有預約。", false),
            (NotificationEventType.TherapistLeave, "心理師請假通知", "{{client_name}} 您好，您的心理師 {{therapist_name}} 於 {{date}} 請假，預約需要重新安排。", true),
            (NotificationEventType.SeriesRenewal, "常態性預約即將到期", "{{therapist_name}} 您好，個案 {{client_name}} 的常態性預約剩餘不足2次，請安排續約。", false),
            (NotificationEventType.PackageLow, "套餐剩餘不足", "個案 {{client_name}} 的套餐「{{package_name}}」剩餘 {{remaining}} 次，請提醒購買。", false),
            (NotificationEventType.PackageExpiring, "套餐即將到期", "個案 {{client_name}} 的套餐「{{package_name}}」即將於 {{expires_at}} 到期。", false),
        };

        // 每個 event_type × 3 channel（in_app / email / sms）各建一份範本
        var channels = new[]
        {
            NotificationChannel.InApp,
            NotificationChannel.Email,
            NotificationChannel.Sms,
        };

        foreach (var (type, subject, body, required) in events)
        {
            foreach (var channel in channels)
            {
                db.NotificationTemplates.Add(new NotificationTemplate
                {
                    OrganizationId = orgId, EventType = type,
                    Channel = channel, Subject = subject,
                    BodyTemplate = body, IsRequired = required,
                });
            }
        }
        await db.SaveChangesAsync();
    }
}
