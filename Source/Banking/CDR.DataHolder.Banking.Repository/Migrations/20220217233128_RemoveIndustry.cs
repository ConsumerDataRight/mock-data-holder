using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CDR.DataHolder.Repository.Migrations
{
    public partial class RemoveIndustry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Industry",
                table: "LegalEntity");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Industry",
                table: "LegalEntity",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: string.Empty);
        }
    }
}
