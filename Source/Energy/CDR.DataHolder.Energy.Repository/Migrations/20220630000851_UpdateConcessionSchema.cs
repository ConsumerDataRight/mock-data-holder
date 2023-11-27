using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CDR.DataHolder.Repository.Migrations
{
    public partial class UpdateConcessionSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyDiscount",
                table: "AccountConcession");

            migrationBuilder.DropColumn(
                name: "MonthlyDiscount",
                table: "AccountConcession");

            migrationBuilder.DropColumn(
                name: "PercentageDiscount",
                table: "AccountConcession");

            migrationBuilder.DropColumn(
                name: "YearlyDiscount",
                table: "AccountConcession");

            migrationBuilder.AddColumn<string>(
                name: "Amount",
                table: "AccountConcession",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppliedTo",
                table: "AccountConcession",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscountFrequency",
                table: "AccountConcession",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Percentage",
                table: "AccountConcession",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "AccountConcession",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "AccountConcession");

            migrationBuilder.DropColumn(
                name: "AppliedTo",
                table: "AccountConcession");

            migrationBuilder.DropColumn(
                name: "DiscountFrequency",
                table: "AccountConcession");

            migrationBuilder.DropColumn(
                name: "Percentage",
                table: "AccountConcession");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "AccountConcession");

            migrationBuilder.AddColumn<decimal>(
                name: "DailyDiscount",
                table: "AccountConcession",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyDiscount",
                table: "AccountConcession",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PercentageDiscount",
                table: "AccountConcession",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "YearlyDiscount",
                table: "AccountConcession",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
