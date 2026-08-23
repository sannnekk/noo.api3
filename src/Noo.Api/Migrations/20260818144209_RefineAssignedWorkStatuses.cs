using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noo.Api.Migrations
{
    /// <inheritdoc />
    public partial class RefineAssignedWorkStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Both members have to live in the column at once so the existing rows can be
            // rewritten before the old ones are dropped.
            migrationBuilder.Sql(
                "ALTER TABLE `assigned_work` MODIFY COLUMN `solve_status` ENUM('NotSolved', 'InProgress', 'Solved', 'SolvedInDeadline', 'SolvedAfterDeadline') CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL;"
            );
            migrationBuilder.Sql(
                "ALTER TABLE `assigned_work` MODIFY COLUMN `check_status` ENUM('NotChecked', 'InProgress', 'Checked', 'CheckedInDeadline', 'CheckedAfterDeadline', 'CheckedAutomatically') CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL;"
            );

            migrationBuilder.Sql(
                """
                UPDATE `assigned_work`
                SET `solve_status` = CASE
                    WHEN `solve_deadline_at` IS NOT NULL AND `solved_at` > `solve_deadline_at`
                        THEN 'SolvedAfterDeadline'
                    ELSE 'SolvedInDeadline'
                END
                WHERE `solve_status` = 'Solved';
                """
            );

            // A work checked at the very moment it was handed in was checked by the automatic
            // checker, not by a mentor.
            migrationBuilder.Sql(
                """
                UPDATE `assigned_work`
                SET `check_status` = CASE
                    WHEN `checked_at` = `solved_at`
                        THEN 'CheckedAutomatically'
                    WHEN `check_deadline_at` IS NOT NULL AND `checked_at` > `check_deadline_at`
                        THEN 'CheckedAfterDeadline'
                    ELSE 'CheckedInDeadline'
                END
                WHERE `check_status` = 'Checked';
                """
            );

            migrationBuilder.AlterColumn<string>(
                name: "solve_status",
                table: "assigned_work",
                type: "ENUM('NotSolved', 'InProgress', 'SolvedInDeadline', 'SolvedAfterDeadline')",
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "ENUM('NotSolved', 'InProgress', 'Solved')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "check_status",
                table: "assigned_work",
                type: "ENUM('NotChecked', 'InProgress', 'CheckedInDeadline', 'CheckedAfterDeadline', 'CheckedAutomatically')",
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "ENUM('NotChecked', 'InProgress', 'Checked')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE `assigned_work` MODIFY COLUMN `solve_status` ENUM('NotSolved', 'InProgress', 'Solved', 'SolvedInDeadline', 'SolvedAfterDeadline') CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL;"
            );
            migrationBuilder.Sql(
                "ALTER TABLE `assigned_work` MODIFY COLUMN `check_status` ENUM('NotChecked', 'InProgress', 'Checked', 'CheckedInDeadline', 'CheckedAfterDeadline', 'CheckedAutomatically') CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL;"
            );

            migrationBuilder.Sql(
                "UPDATE `assigned_work` SET `solve_status` = 'Solved' WHERE `solve_status` IN ('SolvedInDeadline', 'SolvedAfterDeadline');"
            );
            migrationBuilder.Sql(
                "UPDATE `assigned_work` SET `check_status` = 'Checked' WHERE `check_status` IN ('CheckedInDeadline', 'CheckedAfterDeadline', 'CheckedAutomatically');"
            );

            migrationBuilder.AlterColumn<string>(
                name: "solve_status",
                table: "assigned_work",
                type: "ENUM('NotSolved', 'InProgress', 'Solved')",
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "ENUM('NotSolved', 'InProgress', 'SolvedInDeadline', 'SolvedAfterDeadline')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "check_status",
                table: "assigned_work",
                type: "ENUM('NotChecked', 'InProgress', 'Checked')",
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "ENUM('NotChecked', 'InProgress', 'CheckedInDeadline', 'CheckedAfterDeadline', 'CheckedAutomatically')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");
        }
    }
}
