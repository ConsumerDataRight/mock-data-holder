using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CDR.DataHolder.Repository.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAccountOwnershipSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migrate Existing Data to match the seed-data.json
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='UNKNOWN' FROM Account Acc WHERE Acc.AccountId='1122334455' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='ONE_PARTY' FROM Account Acc WHERE Acc.AccountId='98765987' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='UNKNOWN' FROM Account Acc WHERE Acc.AccountId='98765988' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='TWO_PARTY' FROM Account Acc WHERE Acc.AccountId='1235782' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='MANY_PARTY' FROM Account Acc WHERE Acc.AccountId='0000001' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='MANY_PARTY' FROM Account Acc WHERE Acc.AccountId='0000002' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='MANY_PARTY' FROM Account Acc WHERE Acc.AccountId='0000003' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='MANY_PARTY' FROM Account Acc WHERE Acc.AccountId='0000004' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='MANY_PARTY' FROM Account Acc WHERE Acc.AccountId='0000005' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='MANY_PARTY' FROM Account Acc WHERE Acc.AccountId='0000006' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='MANY_PARTY' FROM Account Acc WHERE Acc.AccountId='0000007' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='UNKNOWN' FROM Account Acc WHERE Acc.AccountId='0000008' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='MANY_PARTY' FROM Account Acc WHERE Acc.AccountId='0000009' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='MANY_PARTY' FROM Account Acc WHERE Acc.AccountId='0000010' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='UNKNOWN' FROM Account Acc WHERE Acc.AccountId='1000001' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='UNKNOWN' FROM Account Acc WHERE Acc.AccountId='1000002' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='MANY_PARTY' FROM Account Acc WHERE Acc.AccountId='1000003' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='MANY_PARTY' FROM Account Acc WHERE Acc.AccountId='1000004' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='ONE_PARTY' FROM Account Acc WHERE Acc.AccountId='1000005' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='ONE_PARTY' FROM Account Acc WHERE Acc.AccountId='1000006' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='ONE_PARTY' FROM Account Acc WHERE Acc.AccountId='1000007' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='ONE_PARTY' FROM Account Acc WHERE Acc.AccountId='1000008' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='ONE_PARTY' FROM Account Acc WHERE Acc.AccountId='1000009' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='ONE_PARTY' FROM Account Acc WHERE Acc.AccountId='1000010' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='UNKNOWN' FROM Account Acc WHERE Acc.AccountId='2000001' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='ONE_PARTY' FROM Account Acc WHERE Acc.AccountId='2000002' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='ONE_PARTY' FROM Account Acc WHERE Acc.AccountId='2000003' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='TWO_PARTY' FROM Account Acc WHERE Acc.AccountId='2000004' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='TWO_PARTY' FROM Account Acc WHERE Acc.AccountId='2000005' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='TWO_PARTY' FROM Account Acc WHERE Acc.AccountId='2000006' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='UNKNOWN' FROM Account Acc WHERE Acc.AccountId='2000007' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='UNKNOWN' FROM Account Acc WHERE Acc.AccountId='2000008' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='ONE_PARTY' FROM Account Acc WHERE Acc.AccountId='2000009' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='ONE_PARTY' FROM Account Acc WHERE Acc.AccountId='2000010' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='UNKNOWN' FROM Account Acc WHERE Acc.AccountId='321562' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='UNKNOWN' FROM Account Acc WHERE Acc.AccountId='112324' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='UNKNOWN' FROM Account Acc WHERE Acc.AccountId='4023452' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='ONE_PARTY' FROM Account Acc WHERE Acc.AccountId='2456654' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='UNKNOWN' FROM Account Acc WHERE Acc.AccountId='513452' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='UNKNOWN' FROM Account Acc WHERE Acc.AccountId='123955' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='UNKNOWN' FROM Account Acc WHERE Acc.AccountId='123935' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='ONE_PARTY' FROM Account Acc WHERE Acc.AccountId='96565987' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='ONE_PARTY' FROM Account Acc WHERE Acc.AccountId='1100002' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='ONE_PARTY' FROM Account Acc WHERE Acc.AccountId='96534987' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='UNKNOWN' FROM Account Acc WHERE Acc.AccountId='54676423' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='UNKNOWN' FROM Account Acc WHERE Acc.AccountId='54676422' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='UNKNOWN' FROM Account Acc WHERE Acc.AccountId='95959332' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='UNKNOWN' FROM Account Acc WHERE Acc.AccountId='835672345' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='ONE_PARTY' FROM Account Acc WHERE Acc.AccountId='85123425' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='UNKNOWN' FROM Account Acc WHERE Acc.AccountId='8123415' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='UNKNOWN' FROM Account Acc WHERE Acc.AccountId='4123455' AND ISNULL(Acc.AccountOwnership,'') = ''");
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='UNKNOWN' FROM Account Acc WHERE Acc.AccountId='8982345' AND ISNULL(Acc.AccountOwnership,'') = ''");

            // Fix the incorrect ENUM value from MULTI_PARTY to MANY_PARTY
            migrationBuilder.Sql("UPDATE Acc SET Acc.AccountOwnership='MANY_PARTY' FROM Account Acc WHERE Acc.AccountOwnership = 'MULTI_PARTY'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not applicable.
        }
    }
}
