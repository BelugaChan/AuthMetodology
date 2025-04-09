using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthMetodology.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "refreshToken",
                schema: "authSchema",
                table: "auth",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "refreshTokenExpiry",
                schema: "authSchema",
                table: "auth",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "refreshToken",
                schema: "authSchema",
                table: "auth");

            migrationBuilder.DropColumn(
                name: "refreshTokenExpiry",
                schema: "authSchema",
                table: "auth");
        }
    }
}
