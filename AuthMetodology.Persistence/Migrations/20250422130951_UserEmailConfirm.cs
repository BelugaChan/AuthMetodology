using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthMetodology.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UserEmailConfirm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isEmailConfirmed",
                schema: "authSchema",
                table: "auth",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "resetPasswordToken",
                schema: "authSchema",
                table: "auth",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "resetPasswordTokenExpiry",
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
                name: "isEmailConfirmed",
                schema: "authSchema",
                table: "auth");

            migrationBuilder.DropColumn(
                name: "resetPasswordToken",
                schema: "authSchema",
                table: "auth");

            migrationBuilder.DropColumn(
                name: "resetPasswordTokenExpiry",
                schema: "authSchema",
                table: "auth");
        }
    }
}
