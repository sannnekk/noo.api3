using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Noo.Api.Migrations
{
    /// <inheritdoc />
    public partial class FirstSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "subject",
                columns: new[] { "id", "name", "color", "created_at", "updated_at" },
                values: new object[]
                {
                    Ulid.NewUlid().ToByteArray(),
                    "Биология",
                    "#00bb00",
                    DateTime.UtcNow,
                    null,
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "subject", keyColumn: "name", keyValue: "Биология");
        }
    }
}
