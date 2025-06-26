using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CDR.DataHolder.Repository.Migrations
{
    /// <summary>
    /// LogEventsDrService creation migration script.
    /// </summary>
    public partial class LogEventsDrService : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogEventsDrService",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Environment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProcessId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProcessName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ThreadId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MethodName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SourceContext = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogEventsDrService", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogEventsDrService");
        }
    }
}
