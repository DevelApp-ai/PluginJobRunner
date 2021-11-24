using Microsoft.EntityFrameworkCore.Migrations;

namespace JobExecutorModel.data
{
    public partial class JsonDataToJobData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "JsonData",
                table: "Job",
                newName: "JobData");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "JobData",
                table: "Job",
                newName: "JsonData");
        }
    }
}
