using Microsoft.EntityFrameworkCore.Migrations;

namespace HNTitles.Migrations
{
    public partial class StoreHnIdSeparately : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HnItemId",
                table: "Items",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HnItemId",
                table: "Items");
        }
    }
}
