using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Conbot.RankingPlugin.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildConfigurations",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShowLevelUpAnnouncements = table.Column<bool>(nullable: false),
                    LevelUpAnnouncementsChannelId = table.Column<ulong>(nullable: true),
                    LevelUpAnnouncementsMinimumLevel = table.Column<int>(nullable: true),
                    RoleRewardsType = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildConfigurations", x => x.GuildId);
                });

            migrationBuilder.CreateTable(
                name: "Ranks",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false),
                    IsBot = table.Column<bool>(nullable: false),
                    ExperiencePoints = table.Column<int>(nullable: false),
                    RankedMessages = table.Column<int>(nullable: false),
                    TotalMessages = table.Column<int>(nullable: false),
                    LastMessage = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ranks", x => x.Id);
                    table.UniqueConstraint("AK_Ranks_GuildId_UserId", x => new { x.GuildId, x.UserId });
                });

            migrationBuilder.CreateTable(
                name: "RoleRewards",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(nullable: false),
                    Level = table.Column<int>(nullable: false),
                    RoleId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleRewards", x => x.Id);
                    table.UniqueConstraint("AK_RoleRewards_GuildId_Level", x => new { x.GuildId, x.Level });
                    table.UniqueConstraint("AK_RoleRewards_GuildId_RoleId", x => new { x.GuildId, x.RoleId });
                    table.UniqueConstraint("AK_RoleRewards_Level_RoleId", x => new { x.Level, x.RoleId });
                    table.UniqueConstraint("AK_RoleRewards_GuildId_Level_RoleId", x => new { x.GuildId, x.Level, x.RoleId });
                    table.ForeignKey(
                        name: "FK_RoleRewards_GuildConfigurations_GuildId",
                        column: x => x.GuildId,
                        principalTable: "GuildConfigurations",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ranks");

            migrationBuilder.DropTable(
                name: "RoleRewards");

            migrationBuilder.DropTable(
                name: "GuildConfigurations");
        }
    }
}
