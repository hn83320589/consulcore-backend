using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace consulcore_backend.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class QueryIndexesAndSlotUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_schedule_slots_organization_id",
                table: "schedule_slots");

            migrationBuilder.DropIndex(
                name: "IX_schedule_slots_therapist_user_id",
                table: "schedule_slots");

            migrationBuilder.DropIndex(
                name: "IX_payment_records_organization_id",
                table: "payment_records");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_organization_id",
                table: "audit_logs");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_slots_organization_id_date",
                table: "schedule_slots",
                columns: new[] { "organization_id", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_schedule_slots_therapist_user_id_date_start_time",
                table: "schedule_slots",
                columns: new[] { "therapist_user_id", "date", "start_time" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_records_organization_id_created_at",
                table: "payment_records",
                columns: new[] { "organization_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_notification_logs_recipient_id_channel_status",
                table: "notification_logs",
                columns: new[] { "recipient_id", "channel", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_organization_id_created_at",
                table: "audit_logs",
                columns: new[] { "organization_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_resource_type_resource_id",
                table: "audit_logs",
                columns: new[] { "resource_type", "resource_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_schedule_slots_organization_id_date",
                table: "schedule_slots");

            migrationBuilder.DropIndex(
                name: "IX_schedule_slots_therapist_user_id_date_start_time",
                table: "schedule_slots");

            migrationBuilder.DropIndex(
                name: "IX_payment_records_organization_id_created_at",
                table: "payment_records");

            migrationBuilder.DropIndex(
                name: "IX_notification_logs_recipient_id_channel_status",
                table: "notification_logs");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_organization_id_created_at",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_audit_logs_resource_type_resource_id",
                table: "audit_logs");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_slots_organization_id",
                table: "schedule_slots",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_schedule_slots_therapist_user_id",
                table: "schedule_slots",
                column: "therapist_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_records_organization_id",
                table: "payment_records",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_organization_id",
                table: "audit_logs",
                column: "organization_id");
        }
    }
}
