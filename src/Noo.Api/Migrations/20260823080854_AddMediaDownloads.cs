using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaDownloads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "media_download",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    media_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    user_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    course_material_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_download", x => x.id);
                    table.ForeignKey(
                        name: "FK_media_download_course_material_course_material_id",
                        column: x => x.course_material_id,
                        principalTable: "course_material",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_media_download_media_media_id",
                        column: x => x.media_id,
                        principalTable: "media",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_media_download_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_media_download_course_material_id_id",
                table: "media_download",
                columns: new[] { "course_material_id", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_media_download_media_id_id",
                table: "media_download",
                columns: new[] { "media_id", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_media_download_user_id_id",
                table: "media_download",
                columns: new[] { "user_id", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "media_download");
        }
    }
}
