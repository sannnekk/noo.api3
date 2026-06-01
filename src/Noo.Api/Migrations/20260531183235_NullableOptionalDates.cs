using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noo.Api.Migrations
{
    /// <inheritdoc />
    public partial class NullableOptionalDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_at",
                table: "poll",
                type: "DATETIME(0)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "DATETIME(0)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "start_date",
                table: "course",
                type: "DATETIME(0)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "DATETIME(0)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "end_date",
                table: "course",
                type: "DATETIME(0)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "DATETIME(0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_at",
                table: "poll",
                type: "DATETIME(0)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "DATETIME(0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "start_date",
                table: "course",
                type: "DATETIME(0)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "DATETIME(0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "end_date",
                table: "course",
                type: "DATETIME(0)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "DATETIME(0)",
                oldNullable: true);
        }
    }
}
