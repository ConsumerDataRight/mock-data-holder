using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CDR.DataHolder.Repository.Migrations
{
    public partial class RemoveClientCustomer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientCustomer");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientCustomer",
                columns: table => new
                {
                    ClientCustomerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClientId = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_ClientCustomer_CustomerId",
                table: "ClientCustomer",
                column: "CustomerId");
        }
    }
}
