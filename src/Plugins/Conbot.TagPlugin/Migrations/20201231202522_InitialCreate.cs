using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Conbot.TagPlugin.Migrations
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
                    OwnerId = table.Column<ulong>(nullable: false),
                    CreatorId = table.Column<ulong>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    CreationGuildId = table.Column<ulong>(nullable: false),
                    CreationChannelId = table.Column<ulong>(nullable: false),
                    CreationMessageId = table.Column<ulong>(nullable: true),
                    CreationInteractionId = table.Column<ulong>(nullable: true)
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
                    TagId = table.Column<int>(nullable: false),
                    OwnerId = table.Column<ulong>(nullable: false),
                    CreatorId = table.Column<ulong>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    CreationGuildId = table.Column<ulong>(nullable: false),
                    CreationChannelId = table.Column<ulong>(nullable: false),
                    CreationMessageId = table.Column<ulong>(nullable: true),
                    CreationInteractionId = table.Column<ulong>(nullable: true)
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
                name: "TagModifications",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TagId = table.Column<int>(nullable: false),
                    NewContent = table.Column<string>(nullable: false),
                    OldContent = table.Column<string>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false),
                    ModifiedAt = table.Column<DateTime>(nullable: false),
                    GuildId = table.Column<ulong>(nullable: false),
                    ChannelId = table.Column<ulong>(nullable: false),
                    MessageId = table.Column<ulong>(nullable: true),
                    InteractionId = table.Column<ulong>(nullable: true)
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
                name: "TagOwnerChanges",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TagId = table.Column<int>(nullable: false),
                    NewOwnerId = table.Column<ulong>(nullable: false),
                    OldOwnerId = table.Column<ulong>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false),
                    ChangedAt = table.Column<DateTime>(nullable: false),
                    GuildId = table.Column<ulong>(nullable: false),
                    ChannelId = table.Column<ulong>(nullable: false),
                    MessageId = table.Column<ulong>(nullable: true),
                    InteractionId = table.Column<ulong>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagOwnerChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TagOwnerChanges_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TagAliasOwnerChanges",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TagAliasId = table.Column<int>(nullable: false),
                    NewOwnerId = table.Column<ulong>(nullable: false),
                    OldOwnerId = table.Column<ulong>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false),
                    ChangedAt = table.Column<DateTime>(nullable: false),
                    GuildId = table.Column<ulong>(nullable: false),
                    ChannelId = table.Column<ulong>(nullable: false),
                    MessageId = table.Column<ulong>(nullable: true),
                    InteractionId = table.Column<ulong>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagAliasOwnerChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TagAliasOwnerChanges_TagAliases_TagAliasId",
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
                    TagId = table.Column<int>(nullable: false),
                    UsedAliasId = table.Column<int>(nullable: true),
                    UserId = table.Column<ulong>(nullable: false),
                    UsedAt = table.Column<DateTime>(nullable: false),
                    GuildId = table.Column<ulong>(nullable: false),
                    ChannelId = table.Column<ulong>(nullable: false),
                    MessageId = table.Column<ulong>(nullable: true),
                    InteractionId = table.Column<ulong>(nullable: true)
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
                name: "IX_TagAliasOwnerChanges_TagAliasId",
                table: "TagAliasOwnerChanges",
                column: "TagAliasId");

            migrationBuilder.CreateIndex(
                name: "IX_TagModifications_TagId",
                table: "TagModifications",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_TagOwnerChanges_TagId",
                table: "TagOwnerChanges",
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
                name: "TagAliasOwnerChanges");

            migrationBuilder.DropTable(
                name: "TagModifications");

            migrationBuilder.DropTable(
                name: "TagOwnerChanges");

            migrationBuilder.DropTable(
                name: "TagUses");

            migrationBuilder.DropTable(
                name: "TagAliases");

            migrationBuilder.DropTable(
                name: "Tags");
        }
    }
}
