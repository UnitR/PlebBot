using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace PlebBot.Data.Migrations
{
    public partial class DropIsAssignable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAssignable",
                schema: "public",
                table: "Roles");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAssignable",
                schema: "public",
                table: "Roles",
                nullable: false,
                defaultValue: false);
        }
    }
}
