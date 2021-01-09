#pragma warning disable
using Microsoft.EntityFrameworkCore.Migrations;

namespace TimeZonePlugin.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildTimeZones",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TimeZoneId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildTimeZones", x => x.GuildId);
                });

            migrationBuilder.CreateTable(
                name: "UserTimeZones",
                columns: table => new
                {
                    UserId = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TimeZoneId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTimeZones", x => x.UserId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildTimeZones");

            migrationBuilder.DropTable(
                name: "UserTimeZones");
        }
    }
}
