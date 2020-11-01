using Microsoft.EntityFrameworkCore.Migrations;

namespace Conbot.PrefixPlugin.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Prefixes",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(nullable: false),
                    Text = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prefixes", x => new { x.GuildId, x.Text });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Prefixes");
        }
    }
}
