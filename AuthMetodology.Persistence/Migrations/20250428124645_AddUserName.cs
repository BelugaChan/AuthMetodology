﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthMetodology.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "userName",
                schema: "authSchema",
                table: "auth",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "userName",
                schema: "authSchema",
                table: "auth");
        }
    }
}
