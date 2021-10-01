using Microsoft.EntityFrameworkCore.Migrations;

namespace CDR.DataHolder.Repository.Migrations
{
    public partial class CustomerUpdates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LoginId",
                table: "Customer",
                type: "TEXT",
                maxLength: 8,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoginId",
                table: "Customer");
        }
    }
}
