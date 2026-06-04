using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noo.Api.Migrations
{
    /// <inheritdoc />
    public partial class RefactorAssignedWorkHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assigned_work_status_history");

            migrationBuilder.CreateTable(
                name: "assigned_work_history",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    type = table.Column<string>(type: "ENUM('Created', 'StartedSolving', 'SolveDeadlineShifted', 'Solved', 'StartedChecking', 'CheckDeadlineShifted', 'Checked', 'SentOnRecheck', 'SentOnResolve', 'HelperMentorAdded', 'MainMentorChanged')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    changed_at = table.Column<DateTime>(type: "DATETIME(0)", nullable: false),
                    value = table.Column<string>(type: "json", nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    assigned_work_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    changed_by_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assigned_work_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_assigned_work_history_assigned_work_assigned_work_id",
                        column: x => x.assigned_work_id,
                        principalTable: "assigned_work",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_assigned_work_history_user_changed_by_id",
                        column: x => x.changed_by_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_assigned_work_history_assigned_work_id",
                table: "assigned_work_history",
                column: "assigned_work_id");

            migrationBuilder.CreateIndex(
                name: "IX_assigned_work_history_changed_by_id",
                table: "assigned_work_history",
                column: "changed_by_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assigned_work_history");

            migrationBuilder.CreateTable(
                name: "assigned_work_status_history",
                columns: table => new
                {
                    id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    assigned_work_id = table.Column<byte[]>(type: "BINARY(16)", nullable: false),
                    changed_by_id = table.Column<byte[]>(type: "BINARY(16)", nullable: true),
                    changed_at = table.Column<DateTime>(type: "DATETIME(0)", nullable: false),
                    created_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    type = table.Column<string>(type: "ENUM('StartedSolving', 'SolveDeadlineShifted', 'Solved', 'StartedChecking', 'CheckDeadlineShifted', 'Checked', 'SentOnRecheck', 'SentOnResolve')", nullable: false, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    updated_at = table.Column<DateTime>(type: "TIMESTAMP(6)", nullable: true),
                    value = table.Column<string>(type: "json", nullable: true, collation: "utf8mb4_general_ci")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assigned_work_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_assigned_work_status_history_assigned_work_assigned_work_id",
                        column: x => x.assigned_work_id,
                        principalTable: "assigned_work",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_assigned_work_status_history_user_changed_by_id",
                        column: x => x.changed_by_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_assigned_work_status_history_assigned_work_id",
                table: "assigned_work_status_history",
                column: "assigned_work_id");

            migrationBuilder.CreateIndex(
                name: "IX_assigned_work_status_history_changed_by_id",
                table: "assigned_work_status_history",
                column: "changed_by_id");
        }
    }
}
