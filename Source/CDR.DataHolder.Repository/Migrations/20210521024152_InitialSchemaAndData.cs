using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CDR.DataHolder.Repository.Migrations
{
    public partial class InitialSchemaAndData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LegalEntity",
                columns: table => new
                {
                    LegalEntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LegalEntityName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Industry = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 25, nullable: false),
                    LogoUri = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalEntity", x => x.LegalEntityId);
                });

            migrationBuilder.CreateTable(
                name: "Organisation",
                columns: table => new
                {
                    OrganisationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AgentFirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AgentLastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AgentRole = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BusinessName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    LegalName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ShortName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Abn = table.Column<string>(type: "TEXT", maxLength: 11, nullable: true),
                    Acn = table.Column<string>(type: "TEXT", maxLength: 9, nullable: true),
                    IsAcnCRegistered = table.Column<bool>(type: "INTEGER", nullable: true),
                    IndustryCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    IndustryCodeVersion = table.Column<string>(type: "TEXT", nullable: true),
                    OrganisationType = table.Column<string>(type: "TEXT", nullable: true),
                    RegisteredCountry = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    EstablishmentDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organisation", x => x.OrganisationId);
                });

            migrationBuilder.CreateTable(
                name: "Person",
                columns: table => new
                {
                    PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MiddleNames = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Prefix = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Suffix = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    OccupationCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    OccupationCodeVersion = table.Column<string>(type: "TEXT", nullable: true),
                    LastUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Person", x => x.PersonId);
                });

            migrationBuilder.CreateTable(
                name: "Brand",
                columns: table => new
                {
                    BrandId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BrandName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LogoUri = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 25, nullable: false),
                    LegalEntityId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brand", x => x.BrandId);
                    table.ForeignKey(
                        name: "FK_Brand_LegalEntity_LegalEntityId",
                        column: x => x.LegalEntityId,
                        principalTable: "LegalEntity",
                        principalColumn: "LegalEntityId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Customer",
                columns: table => new
                {
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerUType = table.Column<string>(type: "TEXT", nullable: true),
                    PersonId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OrganisationId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customer", x => x.CustomerId);
                    table.ForeignKey(
                        name: "FK_Customer_Organisation_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisation",
                        principalColumn: "OrganisationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Customer_Person_PersonId",
                        column: x => x.PersonId,
                        principalTable: "Person",
                        principalColumn: "PersonId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoftwareProduct",
                columns: table => new
                {
                    SoftwareProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SoftwareProductName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SoftwareProductDesc = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    LogoUri = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 25, nullable: false),
                    BrandId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoftwareProduct", x => x.SoftwareProductId);
                    table.ForeignKey(
                        name: "FK_SoftwareProduct_Brand_BrandId",
                        column: x => x.BrandId,
                        principalTable: "Brand",
                        principalColumn: "BrandId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Account",
                columns: table => new
                {
                    AccountId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NickName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OpenStatus = table.Column<string>(type: "TEXT", nullable: true),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MaskedName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ProductCategory = table.Column<string>(type: "TEXT", nullable: true),
                    ProductName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Account", x => x.AccountId);
                    table.ForeignKey(
                        name: "FK_Account_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientCustomer",
                columns: table => new
                {
                    ClientCustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientCustomer", x => x.ClientCustomerId);
                    table.ForeignKey(
                        name: "FK_ClientCustomer_Customer_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customer",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transaction",
                columns: table => new
                {
                    TransactionId = table.Column<string>(type: "TEXT", nullable: false),
                    TransactionType = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PostingDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ValueDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExecutionDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Amount = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: true),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MerchantName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    MerchantCategoryCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    BillerCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    BillerName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Crn = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ApcaNumber = table.Column<string>(type: "TEXT", maxLength: 6, nullable: true),
                    AccountId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transaction", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_Transaction_Account_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Account_CustomerId",
                table: "Account",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Brand_LegalEntityId",
                table: "Brand",
                column: "LegalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientCustomer_CustomerId",
                table: "ClientCustomer",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Customer_OrganisationId",
                table: "Customer",
                column: "OrganisationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customer_PersonId",
                table: "Customer",
                column: "PersonId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareProduct_BrandId",
                table: "SoftwareProduct",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_AccountId",
                table: "Transaction",
                column: "AccountId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientCustomer");

            migrationBuilder.DropTable(
                name: "SoftwareProduct");

            migrationBuilder.DropTable(
                name: "Transaction");

            migrationBuilder.DropTable(
                name: "Brand");

            migrationBuilder.DropTable(
                name: "Account");

            migrationBuilder.DropTable(
                name: "LegalEntity");

            migrationBuilder.DropTable(
                name: "Customer");

            migrationBuilder.DropTable(
                name: "Organisation");

            migrationBuilder.DropTable(
                name: "Person");
        }
    }
}
