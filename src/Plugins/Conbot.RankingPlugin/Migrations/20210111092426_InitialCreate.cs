#pragma warning disable
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
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShowLevelUpAnnouncements = table.Column<bool>(type: "INTEGER", nullable: true),
                    LevelUpAnnouncementsChannelId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    LevelUpAnnouncementsMinimumLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    RoleRewardsType = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildConfigurations", x => x.GuildId);
                });

            migrationBuilder.CreateTable(
                name: "Ranks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    IsBot = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExperiencePoints = table.Column<int>(type: "INTEGER", nullable: false),
                    RankedMessages = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalMessages = table.Column<int>(type: "INTEGER", nullable: false),
                    LastMessage = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ranks", x => x.Id);
                    table.UniqueConstraint("AK_Ranks_GuildId_UserId", x => new { x.GuildId, x.UserId });
                });

            migrationBuilder.CreateTable(
                name: "IgnoredChannels",
                columns: table => new
                {
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IgnoredChannels", x => x.ChannelId);
                    table.ForeignKey(
                        name: "FK_IgnoredChannels_GuildConfigurations_GuildId",
                        column: x => x.GuildId,
                        principalTable: "GuildConfigurations",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleRewards",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleRewards", x => x.Id);
                    table.UniqueConstraint("AK_RoleRewards_GuildId_Level", x => new { x.GuildId, x.Level });
                    table.UniqueConstraint("AK_RoleRewards_GuildId_Level_RoleId", x => new { x.GuildId, x.Level, x.RoleId });
                    table.UniqueConstraint("AK_RoleRewards_GuildId_RoleId", x => new { x.GuildId, x.RoleId });
                    table.UniqueConstraint("AK_RoleRewards_Level_RoleId", x => new { x.Level, x.RoleId });
                    table.ForeignKey(
                        name: "FK_RoleRewards_GuildConfigurations_GuildId",
                        column: x => x.GuildId,
                        principalTable: "GuildConfigurations",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IgnoredChannels_GuildId",
                table: "IgnoredChannels",
                column: "GuildId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IgnoredChannels");

            migrationBuilder.DropTable(
                name: "Ranks");

            migrationBuilder.DropTable(
                name: "RoleRewards");

            migrationBuilder.DropTable(
                name: "GuildConfigurations");
        }
    }
}
