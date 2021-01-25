using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Conbot.ModerationPlugin.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildConfigurations",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildConfigurations", x => x.GuildId);
                });

            migrationBuilder.CreateTable(
                name: "PreconfiguredMutedRoles",
                columns: table => new
                {
                    RoleId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreconfiguredMutedRoles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "TemporaryMutedUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndsAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemporaryMutedUsers", x => x.Id);
                    table.UniqueConstraint("AK_TemporaryMutedUsers_GuildId_UserId", x => new { x.GuildId, x.UserId });
                    table.ForeignKey(
                        name: "FK_TemporaryMutedUsers_GuildConfigurations_GuildId",
                        column: x => x.GuildId,
                        principalTable: "GuildConfigurations",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreconfiguredMutedRoles");

            migrationBuilder.DropTable(
                name: "TemporaryMutedUsers");

            migrationBuilder.DropTable(
                name: "GuildConfigurations");
        }
    }
}
