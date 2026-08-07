using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "saved_task",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    user_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    task_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    assigned_work_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_task", x => x.id);
                    table.ForeignKey(
                        name: "FK_saved_task_assigned_work_assigned_work_id",
                        column: x => x.assigned_work_id,
                        principalTable: "assigned_work",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_saved_task_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_saved_task_work_task_task_id",
                        column: x => x.task_id,
                        principalTable: "work_task",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_saved_task_assigned_work_id",
                table: "saved_task",
                column: "assigned_work_id");

            migrationBuilder.CreateIndex(
                name: "IX_saved_task_task_id",
                table: "saved_task",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_saved_task_user_id_task_id",
                table: "saved_task",
                columns: new[] { "user_id", "task_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "saved_task");
        }
    }
}
