using System.Text;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noo.Api.Migrations
{
    /// <inheritdoc />
    public partial class CreateFirstTeacher : Migration
    {
        private const string _id = "01K4Q57CF0R2PWWAAPJPZ5K71R";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "user",
                columns: ["id", "name", "username", "email", "password_hash", "role", "is_blocked", "is_verified", "created_at"],
                values: [
                    Ulid.Parse(_id).ToByteArray(),
                    "Учитель",
                    "teacher",
                    "teacher@example.com",
                    "bw1koNthRjLOrzoxWxTHioKdz4+CdnteP4JvVPUPQYs=", // pre-hashed password
                    "teacher",
                    false,
                    true,
                    DateTime.UtcNow
                ]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "user",
                keyColumn: "id",
                keyValue: _id);
        }
    }
}
