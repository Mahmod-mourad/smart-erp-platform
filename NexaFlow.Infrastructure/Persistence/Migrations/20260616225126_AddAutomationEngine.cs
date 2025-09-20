using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexaFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomationEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TriggerType = table.Column<int>(type: "int", nullable: false),
                    TriggerConfig = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionsConfig = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TriggerData = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowLogs_WorkflowRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "WorkflowRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowLogs_RuleId_ExecutedAt",
                table: "WorkflowLogs",
                columns: new[] { "RuleId", "ExecutedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowLogs_TenantId",
                table: "WorkflowLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRules_TenantId",
                table: "WorkflowRules",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRules_TenantId_IsActive",
                table: "WorkflowRules",
                columns: new[] { "TenantId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowLogs");

            migrationBuilder.DropTable(
                name: "WorkflowRules");
        }
    }
}
