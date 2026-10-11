using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace consulcore_backend.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "consent_templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "TEXT", nullable: false),
                    version = table.Column<int>(type: "INTEGER", nullable: false),
                    trigger_event = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consent_templates", x => x.id);
                    table.ForeignKey(
                        name: "FK_consent_templates_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "form_templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    is_system_default = table.Column<bool>(type: "INTEGER", nullable: false),
                    version = table.Column<int>(type: "INTEGER", nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_by = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_templates", x => x.id);
                    table.ForeignKey(
                        name: "FK_form_templates_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    event_type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    channel = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    subject = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    body_template = table.Column<string>(type: "TEXT", nullable: false),
                    is_required = table.Column<bool>(type: "INTEGER", nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_templates", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_templates_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "packages",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    session_type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    total_sessions = table.Column<int>(type: "INTEGER", nullable: false),
                    total_amount = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    per_session_amount = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    valid_days = table.Column<int>(type: "INTEGER", nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_packages", x => x.id);
                    table.ForeignKey(
                        name: "FK_packages_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    is_system_default = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_roles_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rooms",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rooms", x => x.id);
                    table.ForeignKey(
                        name: "FK_rooms_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                    email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, collation: "NOCASE"),
                    password_hash = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    display_name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    phone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_users_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "form_sections",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    template_id = table.Column<int>(type: "INTEGER", nullable: false),
                    title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    sort_order = table.Column<int>(type: "INTEGER", nullable: false),
                    is_required = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_sections", x => x.id);
                    table.ForeignKey(
                        name: "FK_form_sections_form_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "form_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notification_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    template_id = table.Column<int>(type: "INTEGER", nullable: true),
                    recipient_id = table.Column<int>(type: "INTEGER", nullable: false),
                    recipient_type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    channel = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    subject = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "TEXT", nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    sent_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    read_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    error_message = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_logs_notification_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "notification_templates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_notification_logs_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    role_id = table.Column<int>(type: "INTEGER", nullable: false),
                    permission_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    user_id = table.Column<int>(type: "INTEGER", nullable: false),
                    action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    resource_type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    resource_id = table.Column<int>(type: "INTEGER", nullable: false),
                    description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    changes = table.Column<string>(type: "TEXT", nullable: true),
                    ip_address = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_logs_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_profiles",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    user_id = table.Column<int>(type: "INTEGER", nullable: true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    id_number = table.Column<string>(type: "TEXT", nullable: true),
                    gender = table.Column<string>(type: "TEXT", nullable: true),
                    date_of_birth = table.Column<string>(type: "TEXT", nullable: true),
                    phone = table.Column<string>(type: "TEXT", nullable: true),
                    email = table.Column<string>(type: "TEXT", nullable: true),
                    address = table.Column<string>(type: "TEXT", nullable: true),
                    emergency_contact_name = table.Column<string>(type: "TEXT", nullable: true),
                    emergency_contact_phone = table.Column<string>(type: "TEXT", nullable: true),
                    emergency_contact_relationship = table.Column<string>(type: "TEXT", nullable: true),
                    occupation = table.Column<string>(type: "TEXT", nullable: true),
                    referral_source = table.Column<string>(type: "TEXT", nullable: true),
                    notes = table.Column<string>(type: "TEXT", nullable: true),
                    medical_history = table.Column<string>(type: "TEXT", nullable: true),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    must_change_password = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_client_profiles_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_client_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "discounts",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    therapist_user_id = table.Column<int>(type: "INTEGER", nullable: true),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    discount_type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    percentage_value = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    fixed_amount = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    applicable_role = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_discounts_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_discounts_users_therapist_user_id",
                        column: x => x.therapist_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "fee_schedules",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    session_type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    visit_type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    therapist_user_id = table.Column<int>(type: "INTEGER", nullable: true),
                    amount = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fee_schedules", x => x.id);
                    table.ForeignKey(
                        name: "FK_fee_schedules_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_fee_schedules_users_therapist_user_id",
                        column: x => x.therapist_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "notification_preferences",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<int>(type: "INTEGER", nullable: false),
                    event_type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    email_enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    sms_enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    in_app_enabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_preferences", x => x.id);
                    table.ForeignKey(
                        name: "FK_notification_preferences_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "performance_reviews",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    therapist_user_id = table.Column<int>(type: "INTEGER", nullable: false),
                    reviewer_id = table.Column<int>(type: "INTEGER", nullable: false),
                    period = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    professional_score = table.Column<int>(type: "INTEGER", nullable: false),
                    client_feedback_score = table.Column<int>(type: "INTEGER", nullable: false),
                    teamwork_score = table.Column<int>(type: "INTEGER", nullable: false),
                    overall_score = table.Column<int>(type: "INTEGER", nullable: false),
                    comment = table.Column<string>(type: "TEXT", nullable: true),
                    visibility = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    visible_from = table.Column<DateTime>(type: "TEXT", nullable: true),
                    visible_until = table.Column<DateTime>(type: "TEXT", nullable: true),
                    opened_by = table.Column<int>(type: "INTEGER", nullable: true),
                    opened_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_performance_reviews", x => x.id);
                    table.ForeignKey(
                        name: "FK_performance_reviews_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_performance_reviews_users_reviewer_id",
                        column: x => x.reviewer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_performance_reviews_users_therapist_user_id",
                        column: x => x.therapist_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "phrase_templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    therapist_user_id = table.Column<int>(type: "INTEGER", nullable: false),
                    category = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    content = table.Column<string>(type: "TEXT", nullable: false),
                    sort_order = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_phrase_templates", x => x.id);
                    table.ForeignKey(
                        name: "FK_phrase_templates_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_phrase_templates_users_therapist_user_id",
                        column: x => x.therapist_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "schedule_slots",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    therapist_user_id = table.Column<int>(type: "INTEGER", nullable: false),
                    room_id = table.Column<int>(type: "INTEGER", nullable: true),
                    date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    is_reserved_hold = table.Column<bool>(type: "INTEGER", nullable: false),
                    note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_slots", x => x.id);
                    table.ForeignKey(
                        name: "FK_schedule_slots_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_schedule_slots_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_schedule_slots_users_therapist_user_id",
                        column: x => x.therapist_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "schedule_templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    therapist_user_id = table.Column<int>(type: "INTEGER", nullable: false),
                    day_of_week = table.Column<int>(type: "INTEGER", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    slot_duration = table.Column<int>(type: "INTEGER", nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_schedule_templates", x => x.id);
                    table.ForeignKey(
                        name: "FK_schedule_templates_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_schedule_templates_users_therapist_user_id",
                        column: x => x.therapist_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "therapist_rooms",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    therapist_user_id = table.Column<int>(type: "INTEGER", nullable: false),
                    room_id = table.Column<int>(type: "INTEGER", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_therapist_rooms", x => x.id);
                    table.ForeignKey(
                        name: "FK_therapist_rooms_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_therapist_rooms_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_therapist_rooms_users_therapist_user_id",
                        column: x => x.therapist_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<int>(type: "INTEGER", nullable: false),
                    role_id = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "violation_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    therapist_user_id = table.Column<int>(type: "INTEGER", nullable: false),
                    reported_by = table.Column<int>(type: "INTEGER", nullable: false),
                    incident_date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    severity = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: false),
                    action_taken = table.Column<string>(type: "TEXT", nullable: true),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    resolved_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_violation_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_violation_records_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_violation_records_users_therapist_user_id",
                        column: x => x.therapist_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "form_fields",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    section_id = table.Column<int>(type: "INTEGER", nullable: false),
                    label = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    field_type = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    options = table.Column<string>(type: "TEXT", nullable: true),
                    is_required = table.Column<bool>(type: "INTEGER", nullable: false),
                    sort_order = table.Column<int>(type: "INTEGER", nullable: false),
                    placeholder = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    validation_rules = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_fields", x => x.id);
                    table.ForeignKey(
                        name: "FK_form_fields_form_sections_section_id",
                        column: x => x.section_id,
                        principalTable: "form_sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "appointment_series",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    client_profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    therapist_user_id = table.Column<int>(type: "INTEGER", nullable: false),
                    day_of_week = table.Column<int>(type: "INTEGER", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    frequency = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    expires_at = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointment_series", x => x.id);
                    table.ForeignKey(
                        name: "FK_appointment_series_client_profiles_client_profile_id",
                        column: x => x.client_profile_id,
                        principalTable: "client_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_appointment_series_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_appointment_series_users_therapist_user_id",
                        column: x => x.therapist_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_memos",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    client_profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    therapist_user_id = table.Column<int>(type: "INTEGER", nullable: false),
                    category = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    content = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_memos", x => x.id);
                    table.ForeignKey(
                        name: "FK_client_memos_client_profiles_client_profile_id",
                        column: x => x.client_profile_id,
                        principalTable: "client_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_client_memos_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_client_memos_users_therapist_user_id",
                        column: x => x.therapist_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "consent_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    consent_template_id = table.Column<int>(type: "INTEGER", nullable: false),
                    client_profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    content_snapshot = table.Column<string>(type: "TEXT", nullable: false),
                    template_version = table.Column<int>(type: "INTEGER", nullable: false),
                    signature_image = table.Column<string>(type: "TEXT", nullable: true),
                    signed_remotely = table.Column<bool>(type: "INTEGER", nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    signed_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    voided_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    void_reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consent_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_consent_records_client_profiles_client_profile_id",
                        column: x => x.client_profile_id,
                        principalTable: "client_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_consent_records_consent_templates_consent_template_id",
                        column: x => x.consent_template_id,
                        principalTable: "consent_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_consent_records_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "form_submissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    template_id = table.Column<int>(type: "INTEGER", nullable: false),
                    template_version = table.Column<int>(type: "INTEGER", nullable: false),
                    client_profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    therapist_user_id = table.Column<int>(type: "INTEGER", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    is_encrypted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_submissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_form_submissions_client_profiles_client_profile_id",
                        column: x => x.client_profile_id,
                        principalTable: "client_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_form_submissions_form_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "form_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_form_submissions_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_form_submissions_users_therapist_user_id",
                        column: x => x.therapist_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "therapist_clients",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    therapist_user_id = table.Column<int>(type: "INTEGER", nullable: false),
                    client_profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ended_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ended_reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_therapist_clients", x => x.id);
                    table.ForeignKey(
                        name: "FK_therapist_clients_client_profiles_client_profile_id",
                        column: x => x.client_profile_id,
                        principalTable: "client_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_therapist_clients_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_therapist_clients_users_therapist_user_id",
                        column: x => x.therapist_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_discounts",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    client_profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    discount_id = table.Column<int>(type: "INTEGER", nullable: false),
                    assigned_by = table.Column<int>(type: "INTEGER", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    is_active = table.Column<bool>(type: "INTEGER", nullable: false),
                    note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_discounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_client_discounts_client_profiles_client_profile_id",
                        column: x => x.client_profile_id,
                        principalTable: "client_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_client_discounts_discounts_discount_id",
                        column: x => x.discount_id,
                        principalTable: "discounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_client_discounts_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "appointments",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    client_profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    therapist_user_id = table.Column<int>(type: "INTEGER", nullable: false),
                    room_id = table.Column<int>(type: "INTEGER", nullable: true),
                    schedule_slot_id = table.Column<int>(type: "INTEGER", nullable: false),
                    series_id = table.Column<int>(type: "INTEGER", nullable: true),
                    session_type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    visit_type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    created_by = table.Column<int>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointments", x => x.id);
                    table.ForeignKey(
                        name: "FK_appointments_appointment_series_series_id",
                        column: x => x.series_id,
                        principalTable: "appointment_series",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_appointments_client_profiles_client_profile_id",
                        column: x => x.client_profile_id,
                        principalTable: "client_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_appointments_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_appointments_rooms_room_id",
                        column: x => x.room_id,
                        principalTable: "rooms",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_appointments_schedule_slots_schedule_slot_id",
                        column: x => x.schedule_slot_id,
                        principalTable: "schedule_slots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_appointments_users_therapist_user_id",
                        column: x => x.therapist_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "form_field_values",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    submission_id = table.Column<int>(type: "INTEGER", nullable: false),
                    field_id = table.Column<int>(type: "INTEGER", nullable: false),
                    value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_form_field_values", x => x.id);
                    table.ForeignKey(
                        name: "FK_form_field_values_form_fields_field_id",
                        column: x => x.field_id,
                        principalTable: "form_fields",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_form_field_values_form_submissions_submission_id",
                        column: x => x.submission_id,
                        principalTable: "form_submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    client_profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    appointment_id = table.Column<int>(type: "INTEGER", nullable: true),
                    client_package_id = table.Column<int>(type: "INTEGER", nullable: true),
                    discount_id = table.Column<int>(type: "INTEGER", nullable: true),
                    original_amount = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    final_amount = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    payment_method = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    payment_type = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    recorded_by = table.Column<int>(type: "INTEGER", nullable: false),
                    paid_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_records_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_payment_records_client_profiles_client_profile_id",
                        column: x => x.client_profile_id,
                        principalTable: "client_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_payment_records_discounts_discount_id",
                        column: x => x.discount_id,
                        principalTable: "discounts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_payment_records_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "session_notes",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    client_profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    therapist_user_id = table.Column<int>(type: "INTEGER", nullable: false),
                    appointment_id = table.Column<int>(type: "INTEGER", nullable: true),
                    type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    format = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    soap_s = table.Column<string>(type: "TEXT", nullable: true),
                    soap_o = table.Column<string>(type: "TEXT", nullable: true),
                    soap_a = table.Column<string>(type: "TEXT", nullable: true),
                    soap_p = table.Column<string>(type: "TEXT", nullable: true),
                    free_text_content = table.Column<string>(type: "TEXT", nullable: true),
                    intake_summary_imported = table.Column<bool>(type: "INTEGER", nullable: false),
                    is_locked = table.Column<bool>(type: "INTEGER", nullable: false),
                    locked_at = table.Column<DateTime>(type: "TEXT", nullable: true),
                    locked_by = table.Column<int>(type: "INTEGER", nullable: true),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_notes", x => x.id);
                    table.ForeignKey(
                        name: "FK_session_notes_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_session_notes_client_profiles_client_profile_id",
                        column: x => x.client_profile_id,
                        principalTable: "client_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_session_notes_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_session_notes_users_therapist_user_id",
                        column: x => x.therapist_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_packages",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    organization_id = table.Column<int>(type: "INTEGER", nullable: false),
                    client_profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    package_id = table.Column<int>(type: "INTEGER", nullable: false),
                    purchased_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    expires_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    sessions_used = table.Column<int>(type: "INTEGER", nullable: false),
                    sessions_remaining = table.Column<int>(type: "INTEGER", nullable: false),
                    status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    payment_record_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client_packages", x => x.id);
                    table.ForeignKey(
                        name: "FK_client_packages_client_profiles_client_profile_id",
                        column: x => x.client_profile_id,
                        principalTable: "client_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_client_packages_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_client_packages_packages_package_id",
                        column: x => x.package_id,
                        principalTable: "packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_client_packages_payment_records_payment_record_id",
                        column: x => x.payment_record_id,
                        principalTable: "payment_records",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_appointment_series_client_profile_id",
                table: "appointment_series",
                column: "client_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_appointment_series_organization_id",
                table: "appointment_series",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_appointment_series_therapist_user_id",
                table: "appointment_series",
                column: "therapist_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_client_profile_id",
                table: "appointments",
                column: "client_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_organization_id",
                table: "appointments",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_room_id",
                table: "appointments",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_schedule_slot_id",
                table: "appointments",
                column: "schedule_slot_id");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_series_id",
                table: "appointments",
                column: "series_id");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_therapist_user_id",
                table: "appointments",
                column: "therapist_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_organization_id",
                table: "audit_logs",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_client_discounts_client_profile_id",
                table: "client_discounts",
                column: "client_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_client_discounts_discount_id",
                table: "client_discounts",
                column: "discount_id");

            migrationBuilder.CreateIndex(
                name: "IX_client_discounts_organization_id",
                table: "client_discounts",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_client_memos_client_profile_id",
                table: "client_memos",
                column: "client_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_client_memos_organization_id",
                table: "client_memos",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_client_memos_therapist_user_id",
                table: "client_memos",
                column: "therapist_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_client_packages_client_profile_id",
                table: "client_packages",
                column: "client_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_client_packages_organization_id",
                table: "client_packages",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_client_packages_package_id",
                table: "client_packages",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "IX_client_packages_payment_record_id",
                table: "client_packages",
                column: "payment_record_id");

            migrationBuilder.CreateIndex(
                name: "IX_client_profiles_organization_id",
                table: "client_profiles",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_client_profiles_user_id",
                table: "client_profiles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_consent_records_client_profile_id",
                table: "consent_records",
                column: "client_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_consent_records_consent_template_id",
                table: "consent_records",
                column: "consent_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_consent_records_organization_id",
                table: "consent_records",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_consent_templates_organization_id",
                table: "consent_templates",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_discounts_organization_id",
                table: "discounts",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_discounts_therapist_user_id",
                table: "discounts",
                column: "therapist_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_fee_schedules_organization_id",
                table: "fee_schedules",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_fee_schedules_therapist_user_id",
                table: "fee_schedules",
                column: "therapist_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_form_field_values_field_id",
                table: "form_field_values",
                column: "field_id");

            migrationBuilder.CreateIndex(
                name: "IX_form_field_values_submission_id",
                table: "form_field_values",
                column: "submission_id");

            migrationBuilder.CreateIndex(
                name: "IX_form_fields_section_id",
                table: "form_fields",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "IX_form_sections_template_id",
                table: "form_sections",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_form_submissions_client_profile_id",
                table: "form_submissions",
                column: "client_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_form_submissions_organization_id",
                table: "form_submissions",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_form_submissions_template_id",
                table: "form_submissions",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_form_submissions_therapist_user_id",
                table: "form_submissions",
                column: "therapist_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_form_templates_organization_id",
                table: "form_templates",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_logs_organization_id",
                table: "notification_logs",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_logs_template_id",
                table: "notification_logs",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_preferences_user_id",
                table: "notification_preferences",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_templates_organization_id",
                table: "notification_templates",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_packages_organization_id",
                table: "packages",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_records_appointment_id",
                table: "payment_records",
                column: "appointment_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_records_client_profile_id",
                table: "payment_records",
                column: "client_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_records_discount_id",
                table: "payment_records",
                column: "discount_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_records_organization_id",
                table: "payment_records",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_performance_reviews_organization_id",
                table: "performance_reviews",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_performance_reviews_reviewer_id",
                table: "performance_reviews",
                column: "reviewer_id");

            migrationBuilder.CreateIndex(
                name: "IX_performance_reviews_therapist_user_id",
                table: "performance_reviews",
                column: "therapist_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_code",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_phrase_templates_organization_id",
                table: "phrase_templates",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_phrase_templates_therapist_user_id",
                table: "phrase_templates",
                column: "therapist_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_role_id_permission_id",
                table: "role_permissions",
                columns: new[] { "role_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_organization_id_name",
                table: "roles",
                columns: new[] { "organization_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rooms_organization_id",
                table: "rooms",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_slots_organization_id",
                table: "schedule_slots",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_slots_room_id",
                table: "schedule_slots",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_slots_therapist_user_id",
                table: "schedule_slots",
                column: "therapist_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_templates_organization_id",
                table: "schedule_templates",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_templates_therapist_user_id",
                table: "schedule_templates",
                column: "therapist_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_session_notes_appointment_id",
                table: "session_notes",
                column: "appointment_id");

            migrationBuilder.CreateIndex(
                name: "IX_session_notes_client_profile_id",
                table: "session_notes",
                column: "client_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_session_notes_organization_id",
                table: "session_notes",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_session_notes_therapist_user_id",
                table: "session_notes",
                column: "therapist_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_therapist_clients_client_profile_id",
                table: "therapist_clients",
                column: "client_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_therapist_clients_organization_id",
                table: "therapist_clients",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_therapist_clients_therapist_user_id",
                table: "therapist_clients",
                column: "therapist_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_therapist_rooms_organization_id",
                table: "therapist_rooms",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_therapist_rooms_room_id",
                table: "therapist_rooms",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_therapist_rooms_therapist_user_id",
                table: "therapist_rooms",
                column: "therapist_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_user_id_role_id",
                table: "user_roles",
                columns: new[] { "user_id", "role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_organization_id_email",
                table: "users",
                columns: new[] { "organization_id", "email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_organization_id_username",
                table: "users",
                columns: new[] { "organization_id", "username" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_violation_records_organization_id",
                table: "violation_records",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_violation_records_therapist_user_id",
                table: "violation_records",
                column: "therapist_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "client_discounts");

            migrationBuilder.DropTable(
                name: "client_memos");

            migrationBuilder.DropTable(
                name: "client_packages");

            migrationBuilder.DropTable(
                name: "consent_records");

            migrationBuilder.DropTable(
                name: "fee_schedules");

            migrationBuilder.DropTable(
                name: "form_field_values");

            migrationBuilder.DropTable(
                name: "notification_logs");

            migrationBuilder.DropTable(
                name: "notification_preferences");

            migrationBuilder.DropTable(
                name: "performance_reviews");

            migrationBuilder.DropTable(
                name: "phrase_templates");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "schedule_templates");

            migrationBuilder.DropTable(
                name: "session_notes");

            migrationBuilder.DropTable(
                name: "therapist_clients");

            migrationBuilder.DropTable(
                name: "therapist_rooms");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "violation_records");

            migrationBuilder.DropTable(
                name: "packages");

            migrationBuilder.DropTable(
                name: "payment_records");

            migrationBuilder.DropTable(
                name: "consent_templates");

            migrationBuilder.DropTable(
                name: "form_fields");

            migrationBuilder.DropTable(
                name: "form_submissions");

            migrationBuilder.DropTable(
                name: "notification_templates");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "appointments");

            migrationBuilder.DropTable(
                name: "discounts");

            migrationBuilder.DropTable(
                name: "form_sections");

            migrationBuilder.DropTable(
                name: "appointment_series");

            migrationBuilder.DropTable(
                name: "schedule_slots");

            migrationBuilder.DropTable(
                name: "form_templates");

            migrationBuilder.DropTable(
                name: "client_profiles");

            migrationBuilder.DropTable(
                name: "rooms");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "organizations");
        }
    }
}
