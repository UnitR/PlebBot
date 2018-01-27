using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace PlebBot.Data.Migrations
{
    public partial class DiscordIdUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_DiscordId",
                schema: "public",
                table: "Users",
                column: "DiscordId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_DiscordId",
                schema: "public",
                table: "Users");
        }
    }
}
