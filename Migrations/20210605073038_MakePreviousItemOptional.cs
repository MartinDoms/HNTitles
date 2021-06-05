using Microsoft.EntityFrameworkCore.Migrations;

namespace HNTitles.Migrations
{
    public partial class MakePreviousItemOptional : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Items_PreviousItemId",
                table: "Items");

            migrationBuilder.AlterColumn<int>(
                name: "PreviousItemId",
                table: "Items",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Items_PreviousItemId",
                table: "Items",
                column: "PreviousItemId",
                principalTable: "Items",
                principalColumn: "ItemId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Items_Items_PreviousItemId",
                table: "Items");

            migrationBuilder.AlterColumn<int>(
                name: "PreviousItemId",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Items_Items_PreviousItemId",
                table: "Items",
                column: "PreviousItemId",
                principalTable: "Items",
                principalColumn: "ItemId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
