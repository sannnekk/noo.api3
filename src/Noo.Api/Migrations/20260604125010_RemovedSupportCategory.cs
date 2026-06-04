using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noo.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemovedSupportCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_support_article_support_category_category_id",
                table: "support_article");

            migrationBuilder.DropTable(
                name: "support_category");

            migrationBuilder.DropIndex(
                name: "IX_support_article_category_id",
                table: "support_article");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "support_article");

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "support_article",
                type: "ENUM('Courses', 'Payment', 'Works')",
                nullable: false,
                defaultValue: "Courses",
                collation: "utf8mb4_general_ci")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "slug",
                table: "support_article",
                type: "VARCHAR(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                collation: "utf8mb4_general_ci")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category",
                table: "support_article");

            migrationBuilder.DropColumn(
                name: "slug",
                table: "support_article");

            migrationBuilder.AddColumn<byte[]>(
                name: "category_id",
                table: "support_article",
                type: "BINARY(16)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "support_category",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    parent_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    is_active = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    is_pinned = table.Column<bool>(type: "TINYINT(1)", nullable: false),
                    name = table.Column<string>(type: "VARCHAR(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    order = table.Column<int>(type: "INT", nullable: false),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_category", x => x.id);
                    table.ForeignKey(
                        name: "FK_support_category_support_category_parent_id",
                        column: x => x.parent_id,
                        principalTable: "support_category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_support_article_category_id",
                table: "support_article",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_support_category_parent_id",
                table: "support_category",
                column: "parent_id");

            migrationBuilder.AddForeignKey(
                name: "FK_support_article_support_category_category_id",
                table: "support_article",
                column: "category_id",
                principalTable: "support_category",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
