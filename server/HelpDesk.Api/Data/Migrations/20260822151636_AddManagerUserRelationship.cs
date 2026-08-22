using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDesk.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddManagerUserRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ManagerUserId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ManagerUserId",
                table: "Users",
                column: "ManagerUserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Users_Manager_NotSelf",
                table: "Users",
                sql: "\"ManagerUserId\" IS NULL OR \"ManagerUserId\" <> \"Id\"");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_ManagerUserId",
                table: "Users",
                column: "ManagerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_ManagerUserId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ManagerUserId",
                table: "Users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Users_Manager_NotSelf",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ManagerUserId",
                table: "Users");
        }
    }
}
