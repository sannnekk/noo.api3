using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noo.Api.Migrations
{
    /// <inheritdoc />
    public partial class RestructuredCourseMaterialReactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The old rows reference material contents; the new FK targets materials,
            // so they cannot be converted in place and are wiped (test data).
            migrationBuilder.Sql("DELETE FROM course_reaction;");

            migrationBuilder.DropForeignKey(
                name: "FK_course_reaction_course_material_CourseMaterialModelId",
                table: "course_reaction");

            migrationBuilder.DropForeignKey(
                name: "FK_course_reaction_course_material_content_material_content_id",
                table: "course_reaction");

            migrationBuilder.DropIndex(
                name: "IX_course_reaction_CourseMaterialModelId",
                table: "course_reaction");

            migrationBuilder.DropIndex(
                name: "IX_course_reaction_material_content_id",
                table: "course_reaction");

            migrationBuilder.DropColumn(
                name: "CourseMaterialModelId",
                table: "course_reaction");

            migrationBuilder.RenameColumn(
                name: "material_content_id",
                table: "course_reaction",
                newName: "material_id");

            migrationBuilder.CreateIndex(
                name: "IX_course_reaction_material_id_user_id",
                table: "course_reaction",
                columns: new[] { "material_id", "user_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_course_reaction_course_material_material_id",
                table: "course_reaction",
                column: "material_id",
                principalTable: "course_material",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_course_reaction_course_material_material_id",
                table: "course_reaction");

            migrationBuilder.DropIndex(
                name: "IX_course_reaction_material_id_user_id",
                table: "course_reaction");

            migrationBuilder.RenameColumn(
                name: "material_id",
                table: "course_reaction",
                newName: "material_content_id");

            migrationBuilder.AddColumn<byte[]>(
                name: "CourseMaterialModelId",
                table: "course_reaction",
                type: "BINARY(16)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_course_reaction_CourseMaterialModelId",
                table: "course_reaction",
                column: "CourseMaterialModelId");

            migrationBuilder.CreateIndex(
                name: "IX_course_reaction_material_content_id",
                table: "course_reaction",
                column: "material_content_id");

            migrationBuilder.AddForeignKey(
                name: "FK_course_reaction_course_material_CourseMaterialModelId",
                table: "course_reaction",
                column: "CourseMaterialModelId",
                principalTable: "course_material",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_course_reaction_course_material_content_material_content_id",
                table: "course_reaction",
                column: "material_content_id",
                principalTable: "course_material_content",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
