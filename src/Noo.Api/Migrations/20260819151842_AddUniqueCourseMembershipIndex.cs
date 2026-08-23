using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueCourseMembershipIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nothing stopped a student being put on the same course twice, so the index
            // below would have nothing to attach to until the duplicates are gone. Of each
            // set, keep the membership they still hold — the oldest active one, or failing
            // that simply the oldest, ULIDs being chronological.
            migrationBuilder.Sql(
                """
                DELETE cm FROM `course_membership` cm
                JOIN (
                    SELECT
                        `course_id`,
                        `student_id`,
                        MIN(CASE WHEN `is_active` THEN `id` END) AS keep_active,
                        MIN(`id`) AS keep_any
                    FROM `course_membership`
                    GROUP BY `course_id`, `student_id`
                    HAVING COUNT(*) > 1
                ) d ON d.`course_id` = cm.`course_id` AND d.`student_id` = cm.`student_id`
                WHERE cm.`id` <> COALESCE(d.keep_active, d.keep_any);
                """
            );

            // The new index has to exist before the old one goes: the foreign key on
            // course_id needs an index to lean on, and (course_id, student_id) only
            // becomes one for it once it is there.
            migrationBuilder.CreateIndex(
                name: "IX_course_membership_course_id_student_id",
                table: "course_membership",
                columns: new[] { "course_id", "student_id" },
                unique: true);

            migrationBuilder.DropIndex(
                name: "IX_course_membership_course_id",
                table: "course_membership");
        }

        /// <inheritdoc />
        /// <remarks>
        /// Reverting drops the constraint but cannot bring the duplicate rows back.
        /// </remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Same ordering in reverse, for the same reason.
            migrationBuilder.CreateIndex(
                name: "IX_course_membership_course_id",
                table: "course_membership",
                column: "course_id");

            migrationBuilder.DropIndex(
                name: "IX_course_membership_course_id_student_id",
                table: "course_membership");
        }
    }
}
