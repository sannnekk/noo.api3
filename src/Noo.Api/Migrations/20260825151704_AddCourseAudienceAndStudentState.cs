using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseAudienceAndStudentState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "course_audience",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    course_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    kind = table.Column<string>(type: "ENUM('Everyone', 'SubscriptionTier')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    target_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    granted_by_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_audience", x => x.id);
                    table.ForeignKey(
                        name: "FK_course_audience_course_course_id",
                        column: x => x.course_id,
                        principalTable: "course",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_course_audience_user_granted_by_id",
                        column: x => x.granted_by_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "course_student_state",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    course_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    student_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    is_pinned = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    is_archived = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_student_state", x => x.id);
                    table.ForeignKey(
                        name: "FK_course_student_state_course_course_id",
                        column: x => x.course_id,
                        principalTable: "course",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_course_student_state_user_student_id",
                        column: x => x.student_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_course_audience_course_id_kind_target_id",
                table: "course_audience",
                columns: new[] { "course_id", "kind", "target_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_course_audience_granted_by_id",
                table: "course_audience",
                column: "granted_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_course_student_state_course_id_student_id",
                table: "course_student_state",
                columns: new[] { "course_id", "student_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_course_student_state_student_id",
                table: "course_student_state",
                column: "student_id");

            // Move the per-student display flags off the membership before the columns go. Only
            // rows that actually carry a flag are worth a state row; the rest default to false.
            migrationBuilder.Sql(
                """
                INSERT INTO course_student_state
                    (id, course_id, student_id, is_pinned, is_archived, created_at)
                SELECT
                    UNHEX(REPLACE(UUID(), '-', '')),
                    course_id,
                    student_id,
                    is_pinned_by_student,
                    is_archived_by_student,
                    created_at
                FROM course_membership
                WHERE is_pinned_by_student = 1 OR is_archived_by_student = 1;
                """
            );

            migrationBuilder.DropColumn(
                name: "is_archived_by_student",
                table: "course_membership");

            migrationBuilder.DropColumn(
                name: "is_pinned_by_student",
                table: "course_membership");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_archived_by_student",
                table: "course_membership",
                type: "TINYINT(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_pinned_by_student",
                table: "course_membership",
                type: "TINYINT(1)",
                nullable: false,
                defaultValue: false);

            // State for a course the student was never assigned to has nowhere to go back to,
            // so only rows with a matching membership survive the rollback.
            migrationBuilder.Sql(
                """
                UPDATE course_membership m
                JOIN course_student_state s
                    ON s.course_id = m.course_id AND s.student_id = m.student_id
                SET m.is_pinned_by_student = s.is_pinned,
                    m.is_archived_by_student = s.is_archived;
                """
            );

            migrationBuilder.DropTable(
                name: "course_audience");

            migrationBuilder.DropTable(
                name: "course_student_state");
        }
    }
}
