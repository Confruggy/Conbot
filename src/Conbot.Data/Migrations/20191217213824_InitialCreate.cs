using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Conbot.Data.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Content = table.Column<string>(nullable: false),
                    OwnerId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                    table.UniqueConstraint("AK_Tags_GuildId_Name", x => new { x.GuildId, x.Name });
                });

            migrationBuilder.CreateTable(
                name: "TagAliases",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    OwnerId = table.Column<ulong>(nullable: false),
                    TagId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagAliases", x => x.Id);
                    table.UniqueConstraint("AK_TagAliases_GuildId_Name", x => new { x.GuildId, x.Name });
                    table.ForeignKey(
                        name: "FK_TagAliases_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TagCreations",
                columns: table => new
                {
                    TagId = table.Column<int>(nullable: false),
                    GuildId = table.Column<ulong>(nullable: true),
                    ChannelId = table.Column<ulong>(nullable: false),
                    MessageId = table.Column<ulong>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagCreations", x => x.TagId);
                    table.ForeignKey(
                        name: "FK_TagCreations_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TagModifications",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelId = table.Column<ulong>(nullable: false),
                    MessageId = table.Column<ulong>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(nullable: false),
                    GuildId = table.Column<ulong>(nullable: false),
                    TagId = table.Column<int>(nullable: false),
                    NewContent = table.Column<string>(nullable: false),
                    OldContent = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagModifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TagModifications_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TagAliasCreations",
                columns: table => new
                {
                    TagAliasId = table.Column<int>(nullable: false),
                    GuildId = table.Column<ulong>(nullable: true),
                    ChannelId = table.Column<ulong>(nullable: false),
                    MessageId = table.Column<ulong>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagAliasCreations", x => x.TagAliasId);
                    table.ForeignKey(
                        name: "FK_TagAliasCreations_TagAliases_TagAliasId",
                        column: x => x.TagAliasId,
                        principalTable: "TagAliases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TagUses",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(nullable: true),
                    ChannelId = table.Column<ulong>(nullable: false),
                    MessageId = table.Column<ulong>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(nullable: false),
                    TagId = table.Column<int>(nullable: false),
                    UsedAliasId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagUses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TagUses_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TagUses_TagAliases_UsedAliasId",
                        column: x => x.UsedAliasId,
                        principalTable: "TagAliases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TagAliases_TagId",
                table: "TagAliases",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_TagModifications_TagId",
                table: "TagModifications",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_TagUses_TagId",
                table: "TagUses",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_TagUses_UsedAliasId",
                table: "TagUses",
                column: "UsedAliasId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TagAliasCreations");

            migrationBuilder.DropTable(
                name: "TagCreations");

            migrationBuilder.DropTable(
                name: "TagModifications");

            migrationBuilder.DropTable(
                name: "TagUses");

            migrationBuilder.DropTable(
                name: "TagAliases");

            migrationBuilder.DropTable(
                name: "Tags");
        }
    }
}
