using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthMetodology.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "authSchema");

            migrationBuilder.CreateTable(
                name: "auth",
                schema: "authSchema",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    monkey = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auth",
                schema: "authSchema");
        }
    }
}
