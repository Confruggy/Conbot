using Microsoft.EntityFrameworkCore.Migrations;

namespace Conbot.WelcomePlugin.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Configurations",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShowWelcomeMessages = table.Column<bool>(type: "INTEGER", nullable: false),
                    WelcomeMessageTemplate = table.Column<string>(type: "TEXT", nullable: true),
                    WelcomeChannelId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    ShowGoodbyeMessages = table.Column<bool>(type: "INTEGER", nullable: false),
                    GoodbyeMessageTemplate = table.Column<string>(type: "TEXT", nullable: true),
                    GoodbyeChannelId = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configurations", x => x.GuildId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configurations");
        }
    }
}
