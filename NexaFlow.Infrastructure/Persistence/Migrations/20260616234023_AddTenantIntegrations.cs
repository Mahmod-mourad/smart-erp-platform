using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexaFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantIntegrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    EncryptedConfig = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastTestedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastTestSuccess = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantIntegrations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantIntegrations_TenantId",
                table: "TenantIntegrations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantIntegrations_TenantId_Type",
                table: "TenantIntegrations",
                columns: new[] { "TenantId", "Type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantIntegrations");
        }
    }
}
