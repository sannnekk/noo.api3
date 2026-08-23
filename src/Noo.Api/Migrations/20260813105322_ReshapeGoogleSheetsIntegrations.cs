using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noo.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReshapeGoogleSheetsIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Integrations created against the previous shape cannot be carried forward: their
            // export types no longer exist, their single-value selector cannot express the new
            // parameters, and they only ever stored a short-lived access token, so none of them
            // could run again anyway. They also have no owner, which the new foreign key requires.
            migrationBuilder.Sql("DELETE FROM `google_sheets_integration`;");

            migrationBuilder.DropColumn(
                name: "cron_pattern",
                table: "google_sheets_integration");

            migrationBuilder.DropColumn(
                name: "selector_value",
                table: "google_sheets_integration");

            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "google_sheets_integration",
                type: "ENUM('Users', 'Courses', 'PollResults', 'AssignedWorks')",
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "ENUM('UserCourse', 'UserWork', 'UserRole', 'PollResults')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AddColumn<byte>(
                name: "consecutive_failure_count",
                table: "google_sheets_integration",
                type: "TINYINT UNSIGNED",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<int>(
                name: "last_row_count",
                table: "google_sheets_integration",
                type: "INT(11)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_run_at",
                table: "google_sheets_integration",
                type: "DATETIME(0)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "owner_id",
                table: "google_sheets_integration",
                type: "BINARY(16)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "parameters",
                table: "google_sheets_integration",
                type: "json",
                nullable: false,
                collation: "utf8mb4_general_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "run_started_at",
                table: "google_sheets_integration",
                type: "DATETIME(0)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "run_state",
                table: "google_sheets_integration",
                type: "ENUM('Idle', 'Queued', 'Running')",
                nullable: false,
                defaultValue: "Idle",
                collation: "utf8mb4_general_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "schedule",
                table: "google_sheets_integration",
                type: "ENUM('Manual', 'Hourly', 'Daily', 'Weekly')",
                nullable: false,
                defaultValue: "Manual",
                collation: "utf8mb4_general_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_google_sheets_integration_owner_id",
                table: "google_sheets_integration",
                column: "owner_id");

            migrationBuilder.AddForeignKey(
                name: "FK_google_sheets_integration_user_owner_id",
                table: "google_sheets_integration",
                column: "owner_id",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_google_sheets_integration_user_owner_id",
                table: "google_sheets_integration");

            migrationBuilder.DropIndex(
                name: "IX_google_sheets_integration_owner_id",
                table: "google_sheets_integration");

            migrationBuilder.DropColumn(
                name: "consecutive_failure_count",
                table: "google_sheets_integration");

            migrationBuilder.DropColumn(
                name: "last_row_count",
                table: "google_sheets_integration");

            migrationBuilder.DropColumn(
                name: "next_run_at",
                table: "google_sheets_integration");

            migrationBuilder.DropColumn(
                name: "owner_id",
                table: "google_sheets_integration");

            migrationBuilder.DropColumn(
                name: "parameters",
                table: "google_sheets_integration");

            migrationBuilder.DropColumn(
                name: "run_started_at",
                table: "google_sheets_integration");

            migrationBuilder.DropColumn(
                name: "run_state",
                table: "google_sheets_integration");

            migrationBuilder.DropColumn(
                name: "schedule",
                table: "google_sheets_integration");

            migrationBuilder.AlterColumn<string>(
                name: "type",
                table: "google_sheets_integration",
                type: "ENUM('UserCourse', 'UserWork', 'UserRole', 'PollResults')",
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "ENUM('Users', 'Courses', 'PollResults', 'AssignedWorks')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "cron_pattern",
                table: "google_sheets_integration",
                type: "VARCHAR(63)",
                maxLength: 63,
                nullable: false,
                defaultValue: "",
                collation: "utf8mb4_general_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "selector_value",
                table: "google_sheets_integration",
                type: "VARCHAR(63)",
                nullable: true,
                collation: "utf8mb4_general_ci")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
