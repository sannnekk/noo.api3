using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noo.Api.Migrations
{
    /// <inheritdoc />
    public partial class BiologySubject : Migration
    {
        private const string _id = "01H0X6Z9Z4X5KXGZ1Z8Y5A3V7R"; // Predefined ULID for the subject

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "subject",
                columns: ["id", "name", "color", "created_at", "updated_at"],
                values: [
                    Ulid.Parse(_id).ToByteArray(),
                    "Биология",
                    "#68de48",
                    DateTime.UtcNow,
                    null
                ]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "subject",
                keyColumn: "id",
                keyValue: Ulid.Parse(_id).ToByteArray());
        }
    }
}
