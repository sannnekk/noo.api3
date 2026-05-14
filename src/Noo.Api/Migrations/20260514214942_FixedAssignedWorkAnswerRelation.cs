using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noo.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixedAssignedWorkAnswerRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_assigned_work_answer_assigned_work_AssignedWorkModelId",
                table: "assigned_work_answer");

            migrationBuilder.DropIndex(
                name: "IX_assigned_work_answer_AssignedWorkModelId",
                table: "assigned_work_answer");

            migrationBuilder.DropColumn(
                name: "AssignedWorkModelId",
                table: "assigned_work_answer");

            migrationBuilder.CreateIndex(
                name: "IX_assigned_work_answer_assigned_work_id",
                table: "assigned_work_answer",
                column: "assigned_work_id");

            migrationBuilder.AddForeignKey(
                name: "FK_assigned_work_answer_assigned_work_assigned_work_id",
                table: "assigned_work_answer",
                column: "assigned_work_id",
                principalTable: "assigned_work",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_assigned_work_answer_assigned_work_assigned_work_id",
                table: "assigned_work_answer");

            migrationBuilder.DropIndex(
                name: "IX_assigned_work_answer_assigned_work_id",
                table: "assigned_work_answer");

            migrationBuilder.AddColumn<byte[]>(
                name: "AssignedWorkModelId",
                table: "assigned_work_answer",
                type: "BINARY(16)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_assigned_work_answer_AssignedWorkModelId",
                table: "assigned_work_answer",
                column: "AssignedWorkModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_assigned_work_answer_assigned_work_AssignedWorkModelId",
                table: "assigned_work_answer",
                column: "AssignedWorkModelId",
                principalTable: "assigned_work",
                principalColumn: "id");
        }
    }
}
