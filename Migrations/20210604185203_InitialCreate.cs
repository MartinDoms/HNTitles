using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HNTitles.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    URL = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.ItemId);
                });

            migrationBuilder.CreateTable(
                name: "ItemEntries",
                columns: table => new
                {
                    ItemEntryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    PreviousEntryItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    PreviousEntryItemEntryId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemEntries", x => x.ItemEntryId);
                    table.ForeignKey(
                        name: "FK_ItemEntries_ItemEntries_PreviousEntryItemEntryId",
                        column: x => x.PreviousEntryItemEntryId,
                        principalTable: "ItemEntries",
                        principalColumn: "ItemEntryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemEntries_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemEntries_ItemId",
                table: "ItemEntries",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemEntries_PreviousEntryItemEntryId",
                table: "ItemEntries",
                column: "PreviousEntryItemEntryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemEntries");

            migrationBuilder.DropTable(
                name: "Items");
        }
    }
}
