using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNooTubeVideoFavourite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "nootube_video_favourite",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    user_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    video_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nootube_video_favourite", x => x.id);
                    table.ForeignKey(
                        name: "FK_nootube_video_favourite_nootube_video_video_id",
                        column: x => x.video_id,
                        principalTable: "nootube_video",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_nootube_video_favourite_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_nootube_video_favourite_user_id_video_id",
                table: "nootube_video_favourite",
                columns: new[] { "user_id", "video_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nootube_video_favourite_video_id",
                table: "nootube_video_favourite",
                column: "video_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "nootube_video_favourite");
        }
    }
}
