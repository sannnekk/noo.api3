using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noo.Api.Migrations
{
    /// <inheritdoc />
    public partial class NewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_nootube_video_user_uploaded_by_user_id",
                table: "nootube_video");

            migrationBuilder.RenameColumn(
                name: "uploaded_by_user_id",
                table: "nootube_video",
                newName: "uploaded_by_id");

            migrationBuilder.RenameIndex(
                name: "IX_nootube_video_uploaded_by_user_id",
                table: "nootube_video",
                newName: "IX_nootube_video_uploaded_by_id");

            migrationBuilder.AlterColumn<string>(
                name: "reaction",
                table: "nootube_video_reaction",
                type: "ENUM('Like', 'Dislike', 'Heart', 'Laugh', 'Sad', 'Mindblowing')",
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "ENUM('VideoReaction', 'Like', 'Dislike', 'Heart', 'Laugh', 'Sad', 'Mindblowing')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "state",
                table: "nootube_video",
                type: "ENUM('NotUploaded, Uploading, Encoding, Uploaded, Published')",
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "ENUM('VideoState', 'NotUploaded, Uploading, Uploaded, Published')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "service_type",
                table: "nootube_video",
                type: "ENUM('kinescope')",
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "ENUM('NooTubeServiceType', 'NooTube, YouTube, VkVideo, Rutube')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "nootube_video",
                type: "TINYINT(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_listed",
                table: "nootube_video",
                type: "TINYINT(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_nootube_video_user_uploaded_by_id",
                table: "nootube_video",
                column: "uploaded_by_id",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_nootube_video_user_uploaded_by_id",
                table: "nootube_video");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "nootube_video");

            migrationBuilder.DropColumn(
                name: "is_listed",
                table: "nootube_video");

            migrationBuilder.RenameColumn(
                name: "uploaded_by_id",
                table: "nootube_video",
                newName: "uploaded_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_nootube_video_uploaded_by_id",
                table: "nootube_video",
                newName: "IX_nootube_video_uploaded_by_user_id");

            migrationBuilder.AlterColumn<string>(
                name: "reaction",
                table: "nootube_video_reaction",
                type: "ENUM('VideoReaction', 'Like', 'Dislike', 'Heart', 'Laugh', 'Sad', 'Mindblowing')",
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "ENUM('Like', 'Dislike', 'Heart', 'Laugh', 'Sad', 'Mindblowing')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "state",
                table: "nootube_video",
                type: "ENUM('VideoState', 'NotUploaded, Uploading, Uploaded, Published')",
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "ENUM('NotUploaded, Uploading, Encoding, Uploaded, Published')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "service_type",
                table: "nootube_video",
                type: "ENUM('NooTubeServiceType', 'NooTube, YouTube, VkVideo, Rutube')",
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "ENUM('kinescope')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AddForeignKey(
                name: "FK_nootube_video_user_uploaded_by_user_id",
                table: "nootube_video",
                column: "uploaded_by_user_id",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
