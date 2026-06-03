using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBackgroundImageToUserSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "background_image_id",
                table: "user_settings",
                type: "BINARY(16)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_settings_background_image_id",
                table: "user_settings",
                column: "background_image_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_user_settings_media_background_image_id",
                table: "user_settings",
                column: "background_image_id",
                principalTable: "media",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_settings_media_background_image_id",
                table: "user_settings");

            migrationBuilder.DropIndex(
                name: "IX_user_settings_background_image_id",
                table: "user_settings");

            migrationBuilder.DropColumn(
                name: "background_image_id",
                table: "user_settings");
        }
    }
}
