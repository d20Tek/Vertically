using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IssueTracker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedPedroSilva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedUtc", "Email", "FirstName", "LastName", "UpdatedUtc" },
                values: new object[] { new Guid("44444444-4444-4444-4444-444444444444"), 1308083945472000000L, "pedro@example.com", "Pedro", "Silva", 1308083945472000000L });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));
        }
    }
}
