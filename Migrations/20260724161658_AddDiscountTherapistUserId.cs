using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace consulcore_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountTherapistUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "therapist_user_id",
                table: "discounts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_discounts_therapist_user_id",
                table: "discounts",
                column: "therapist_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_discounts_users_therapist_user_id",
                table: "discounts",
                column: "therapist_user_id",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_discounts_users_therapist_user_id",
                table: "discounts");

            migrationBuilder.DropIndex(
                name: "IX_discounts_therapist_user_id",
                table: "discounts");

            migrationBuilder.DropColumn(
                name: "therapist_user_id",
                table: "discounts");
        }
    }
}
