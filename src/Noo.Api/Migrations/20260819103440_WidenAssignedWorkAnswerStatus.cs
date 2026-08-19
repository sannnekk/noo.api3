using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noo.Api.Migrations
{
    /// <inheritdoc />
    public partial class WidenAssignedWorkAnswerStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "assigned_work_answer",
                type: "ENUM('NotSubmitted', 'Submitted', 'Checked')",
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "ENUM('NotSubmitted', 'Submitted')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            // Marking a work checked has always tried to write "Checked" into a column that
            // did not accept it, so answers of already-checked works never got there.
            migrationBuilder.Sql(
                """
                UPDATE `assigned_work_answer` a
                JOIN `assigned_work` w ON a.`assigned_work_id` = w.`id`
                SET a.`status` = 'Checked'
                WHERE w.`check_status` IN ('CheckedInDeadline', 'CheckedAfterDeadline', 'CheckedAutomatically')
                  AND a.`status` <> 'NotSubmitted';
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE `assigned_work_answer` SET `status` = 'Submitted' WHERE `status` = 'Checked';"
            );

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "assigned_work_answer",
                type: "ENUM('NotSubmitted', 'Submitted')",
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "ENUM('NotSubmitted', 'Submitted', 'Checked')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");
        }
    }
}
