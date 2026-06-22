using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noo.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixedNooTubeStateEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "state",
                table: "nootube_video",
                type: "ENUM('NotUploaded', 'Uploading', 'Encoding', 'Uploaded', 'Published')",
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "ENUM('NotUploaded, Uploading, Encoding, Uploaded, Published')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "state",
                table: "nootube_video",
                type: "ENUM('NotUploaded, Uploading, Encoding, Uploaded, Published')",
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "ENUM('NotUploaded', 'Uploading', 'Encoding', 'Uploaded', 'Published')")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");
        }
    }
}
