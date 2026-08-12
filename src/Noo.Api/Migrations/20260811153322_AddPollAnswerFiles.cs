using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPollAnswerFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "category",
                table: "media",
                type: "ENUM('UserAvatar','VideoCover','VideoRichText','CourseCover','CourseAttachment','CourseRichText','WorkRichText','ProfileBackground','AssignedWorkStudentRichText','AssignedWorkMentorRichText','AssignedWorkStudentCommentRichText','AssignedWorkMentorCommentRichText','HelpRichText','SnippetRichText','PollAnswerFile')",
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "ENUM('UserAvatar','VideoCover','VideoRichText','CourseCover','CourseAttachment','CourseRichText','WorkRichText','ProfileBackground','AssignedWorkStudentRichText','AssignedWorkMentorRichText','AssignedWorkStudentCommentRichText','AssignedWorkMentorCommentRichText','HelpRichText')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "media_mm_Medias_poll_answer",
                columns: table => new
                {
                    media_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    poll_answer_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_mm_Medias_poll_answer", x => new { x.media_id, x.poll_answer_id });
                    table.ForeignKey(
                        name: "FK_media_mm_Medias_poll_answer_media_media_id",
                        column: x => x.media_id,
                        principalTable: "media",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_media_mm_Medias_poll_answer_poll_answer_poll_answer_id",
                        column: x => x.poll_answer_id,
                        principalTable: "poll_answer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_media_mm_Medias_poll_answer_poll_answer_id",
                table: "media_mm_Medias_poll_answer",
                column: "poll_answer_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "media_mm_Medias_poll_answer");

            migrationBuilder.AlterColumn<string>(
                name: "category",
                table: "media",
                type: "ENUM('UserAvatar','VideoCover','VideoRichText','CourseCover','CourseAttachment','CourseRichText','WorkRichText','ProfileBackground','AssignedWorkStudentRichText','AssignedWorkMentorRichText','AssignedWorkStudentCommentRichText','AssignedWorkMentorCommentRichText','HelpRichText')",
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "ENUM('UserAvatar','VideoCover','VideoRichText','CourseCover','CourseAttachment','CourseRichText','WorkRichText','ProfileBackground','AssignedWorkStudentRichText','AssignedWorkMentorRichText','AssignedWorkStudentCommentRichText','AssignedWorkMentorCommentRichText','HelpRichText','SnippetRichText','PollAnswerFile')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");
        }
    }
}
