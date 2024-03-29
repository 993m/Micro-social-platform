﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proiect.Data.Migrations
{
    public partial class BazaLinkGrupuriUseri : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationUsersInGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUsersInGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationUsersInGroups_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationUsersInGroups_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsersInGroups_GroupId",
                table: "ApplicationUsersInGroups",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsersInGroups_UserId",
                table: "ApplicationUsersInGroups",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationUsersInGroups");
        }
    }
}
