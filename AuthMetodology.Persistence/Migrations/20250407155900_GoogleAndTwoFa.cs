using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthMetodology.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GoogleAndTwoFa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "idGoogle",
                schema: "authSchema",
                table: "auth",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is2FAEnabled",
                schema: "authSchema",
                table: "auth",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "idGoogle",
                schema: "authSchema",
                table: "auth");

            migrationBuilder.DropColumn(
                name: "is2FAEnabled",
                schema: "authSchema",
                table: "auth");
        }
    }
}
