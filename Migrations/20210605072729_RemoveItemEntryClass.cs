using Microsoft.EntityFrameworkCore.Migrations;

namespace HNTitles.Migrations
{
    public partial class RemoveItemEntryClass : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemEntries");

            migrationBuilder.AddColumn<int>(
                name: "PreviousItemId",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "RecordedAt",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Items_PreviousItemId",
                table: "Items",
                column: "PreviousItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Items_PreviousItemId",
                table: "Items",
                column: "PreviousItemId",
                principalTable: "Items",
                principalColumn: "ItemId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Items_PreviousItemId",
                table: "Items");

            migrationBuilder.DropIndex(
                name: "IX_Items_PreviousItemId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "PreviousItemId",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "RecordedAt",
                table: "Items");

            migrationBuilder.CreateTable(
                name: "ItemEntries",
                columns: table => new
                {
                    ItemEntryId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    PreviousEntryItemEntryId = table.Column<int>(type: "INTEGER", nullable: true),
                    PreviousEntryItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordedAt = table.Column<long>(type: "INTEGER", nullable: false)
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
    }
}
