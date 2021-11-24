using Microsoft.EntityFrameworkCore.Migrations;

namespace JobExecutorModel.data
{
    public partial class JobExecutionMessageAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JobExecutionMessage",
                table: "Job",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JobExecutionMessage",
                table: "Job");
        }
    }
}
