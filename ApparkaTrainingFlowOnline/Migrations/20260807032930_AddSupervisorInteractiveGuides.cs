using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApparkaTrainingFlowOnline.Migrations
{
    /// <inheritdoc />
    public partial class AddSupervisorInteractiveGuides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SupervisorDashboardGuideCompletedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SupervisorReviewGuideCompletedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupervisorDashboardGuideCompletedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SupervisorReviewGuideCompletedAt",
                table: "Users");
        }
    }
}
