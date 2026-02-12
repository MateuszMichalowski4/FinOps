using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CategorizationService.Migrations
{
    /// <inheritdoc />
    public partial class InitCategorizationDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoryAssignments",
                columns: table => new
                {
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CategoryId = table.Column<string>(type: "text", nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryAssignments", x => x.TransactionId);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedEvents",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedEvents", x => x.EventId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAssignments_UserId",
                table: "CategoryAssignments",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryAssignments");

            migrationBuilder.DropTable(
                name: "ProcessedEvents");
        }
    }
}
