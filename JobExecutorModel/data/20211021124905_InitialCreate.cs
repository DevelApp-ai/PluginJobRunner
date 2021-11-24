using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace JobExecutorModel.data
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Job",
                columns: table => new
                {
                    JobId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Enqueued = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JobExecutionDuration = table.Column<TimeSpan>(type: "time", nullable: false),
                    JobStatus = table.Column<int>(type: "int", nullable: false),
                    JobExecutor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JsonData = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Job", x => x.JobId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Job");
        }
    }
}
