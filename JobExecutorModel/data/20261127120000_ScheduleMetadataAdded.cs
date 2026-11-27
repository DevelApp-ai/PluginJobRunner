using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace JobExecutorModel.data
{
    public partial class ScheduleMetadataAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Schedule/trigger metadata for the shared job substrate
            // (decision DevelApp-Repo-Global #89, PJR-2). All columns are
            // nullable and additive: existing jobs and consumers are
            // unaffected; portable job records set them.
            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "Job",
                type: "nvarchar(450)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmittedBy",
                table: "Job",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TriggerId",
                table: "Job",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TriggerExpression",
                table: "Job",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextFireAtUtc",
                table: "Job",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastOutcomeAtUtc",
                table: "Job",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Job_CorrelationId",
                table: "Job",
                column: "CorrelationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Job_CorrelationId",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "EmittedBy",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "TriggerId",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "TriggerExpression",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "NextFireAtUtc",
                table: "Job");

            migrationBuilder.DropColumn(
                name: "LastOutcomeAtUtc",
                table: "Job");
        }
    }
}
