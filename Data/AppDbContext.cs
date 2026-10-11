using System.Reflection;
using consulcore_backend.Authorization;
using consulcore_backend.Data.Encryption;
using consulcore_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace consulcore_backend.Data;

public class AppDbContext : DbContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEncryptionService _encryptionService;
    private readonly int? _currentOrgId;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IHttpContextAccessor httpContextAccessor,
        IEncryptionService encryptionService)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
        _encryptionService = encryptionService;

        var orgClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(JwtClaims.OrganizationId);
        _currentOrgId = orgClaim != null ? int.Parse(orgClaim.Value) : null;
    }

    // === DbSets ===
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ClientProfile> ClientProfiles => Set<ClientProfile>();
    public DbSet<TherapistClient> TherapistClients => Set<TherapistClient>();
    public DbSet<ConsentTemplate> ConsentTemplates => Set<ConsentTemplate>();
    public DbSet<ConsentRecord> ConsentRecords => Set<ConsentRecord>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<TherapistRoom> TherapistRooms => Set<TherapistRoom>();
    public DbSet<ScheduleTemplate> ScheduleTemplates => Set<ScheduleTemplate>();
    public DbSet<ScheduleSlot> ScheduleSlots => Set<ScheduleSlot>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentSeries> AppointmentSeries => Set<AppointmentSeries>();
    public DbSet<FormTemplate> FormTemplates => Set<FormTemplate>();
    public DbSet<FormSection> FormSections => Set<FormSection>();
    public DbSet<FormField> FormFields => Set<FormField>();
    public DbSet<FormSubmission> FormSubmissions => Set<FormSubmission>();
    public DbSet<FormFieldValue> FormFieldValues => Set<FormFieldValue>();
    public DbSet<SessionNote> SessionNotes => Set<SessionNote>();
    public DbSet<PhraseTemplate> PhraseTemplates => Set<PhraseTemplate>();
    public DbSet<ClientMemo> ClientMemos => Set<ClientMemo>();
    public DbSet<FeeSchedule> FeeSchedules => Set<FeeSchedule>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<ClientPackage> ClientPackages => Set<ClientPackage>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<ClientDiscount> ClientDiscounts => Set<ClientDiscount>();
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<PerformanceReview> PerformanceReviews => Set<PerformanceReview>();
    public DbSet<ViolationRecord> ViolationRecords => Set<ViolationRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 帳號 / Email 比對與唯一性不分大小寫。MySQL 預設 collation 即不分大小寫，
        // SQLite 預設為 binary，需明確指定 NOCASE 才能維持相同行為。
        if (Database.IsSqlite())
        {
            modelBuilder.Entity<User>().Property(u => u.Username).UseCollation("NOCASE");
            modelBuilder.Entity<User>().Property(u => u.Email).UseCollation("NOCASE");
        }

        // === Unique Constraints ===
        modelBuilder.Entity<User>()
            .HasIndex(u => new { u.OrganizationId, u.Username })
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => new { u.OrganizationId, u.Email })
            .IsUnique();

        modelBuilder.Entity<Permission>()
            .HasIndex(p => p.Code)
            .IsUnique();

        modelBuilder.Entity<Role>()
            .HasIndex(r => new { r.OrganizationId, r.Name })
            .IsUnique();

        modelBuilder.Entity<UserRole>()
            .HasIndex(ur => new { ur.UserId, ur.RoleId })
            .IsUnique();

        modelBuilder.Entity<RolePermission>()
            .HasIndex(rp => new { rp.RoleId, rp.PermissionId })
            .IsUnique();

        // === Global Query Filters ===
        // Permission 是全域共用，不加 filter
        modelBuilder.Entity<Organization>().HasQueryFilter(e => e.Id == _currentOrgId);
        modelBuilder.Entity<User>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<Role>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<UserRole>().HasQueryFilter(e => e.User.OrganizationId == _currentOrgId);
        modelBuilder.Entity<RolePermission>().HasQueryFilter(e => e.Role.OrganizationId == _currentOrgId);
        modelBuilder.Entity<AuditLog>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<ClientProfile>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<TherapistClient>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<ConsentTemplate>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<ConsentRecord>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<Room>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<TherapistRoom>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<ScheduleTemplate>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<ScheduleSlot>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<Appointment>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<AppointmentSeries>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<FormTemplate>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<FormSubmission>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<SessionNote>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<PhraseTemplate>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<ClientMemo>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<FeeSchedule>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<Package>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<ClientPackage>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<Discount>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<ClientDiscount>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<PaymentRecord>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<NotificationTemplate>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<NotificationLog>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<PerformanceReview>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);
        modelBuilder.Entity<ViolationRecord>().HasQueryFilter(e => e.OrganizationId == _currentOrgId);

        // 以下實體本身無 OrganizationId，透過父實體關聯套用租戶隔離
        // （同時讓 required 關係兩端的 query filter 一致，消除 EF 模型驗證警告）
        modelBuilder.Entity<FormSection>().HasQueryFilter(e => e.Template.OrganizationId == _currentOrgId);
        modelBuilder.Entity<FormField>().HasQueryFilter(e => e.Section.Template.OrganizationId == _currentOrgId);
        modelBuilder.Entity<FormFieldValue>().HasQueryFilter(e => e.Submission.OrganizationId == _currentOrgId);
        modelBuilder.Entity<NotificationPreference>().HasQueryFilter(e => e.User.OrganizationId == _currentOrgId);

        // === 自動套用 EncryptedStringConverter ===
        var converter = new EncryptedStringConverter(_encryptionService);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.ClrType.GetProperties())
            {
                if (property.GetCustomAttribute<EncryptedAttribute>() != null
                    && property.PropertyType == typeof(string))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property(property.Name)
                        .HasConversion(converter);
                }
            }
        }
    }
}
