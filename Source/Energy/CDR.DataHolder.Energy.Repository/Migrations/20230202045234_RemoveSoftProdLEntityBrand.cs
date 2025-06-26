using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CDR.DataHolder.Repository.Migrations
{
    /// <summary>
    /// Remove SoftwareProduct, Brand and LegalEntity tables migration script.
    /// </summary>
    public partial class RemoveSoftProdLEntityBrand : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SoftwareProduct");

            migrationBuilder.DropTable(
                name: "Brand");

            migrationBuilder.DropTable(
                name: "LegalEntity");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LegalEntity",
                columns: table => new
                {
                    LegalEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegalEntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LogoUri = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalEntity", x => x.LegalEntityId);
                });

            migrationBuilder.CreateTable(
                name: "Brand",
                columns: table => new
                {
                    BrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegalEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrandName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LogoUri = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
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
                name: "SoftwareProduct",
                columns: table => new
                {
                    SoftwareProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LogoUri = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SoftwareProductDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SoftwareProductName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_Brand_LegalEntityId",
                table: "Brand",
                column: "LegalEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_SoftwareProduct_BrandId",
                table: "SoftwareProduct",
                column: "BrandId");
        }
    }
}
