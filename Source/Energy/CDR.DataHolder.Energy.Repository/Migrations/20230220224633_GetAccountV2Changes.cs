using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CDR.DataHolder.Repository.Migrations
{
    /// <summary>
    /// Update Account table For GetAccountV2 changes migration script.
    /// </summary>
    public partial class GetAccountV2Changes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OpenStatus",
                table: "Account",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // Migrate Existing Data
            // Set all accounts to OPEN
            migrationBuilder.Sql("UPDATE a SET a.OpenStatus='OPEN' FROM dbo.Account a WHERE a.OpenStatus IS NULL");

            // Set some seed data accounts to CLOSED
            migrationBuilder.Sql("UPDATE a SET a.OpenStatus='CLOSED' FROM dbo.Account a WHERE a.AccountId IN ('0011223319', '0011223320', '0011223321', '4ee1a8db-13af-44d7-b54b-e94dff3df548')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpenStatus",
                table: "Account");
        }
    }
}
