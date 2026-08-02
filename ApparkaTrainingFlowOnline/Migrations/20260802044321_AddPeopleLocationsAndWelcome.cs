using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApparkaTrainingFlowOnline.Migrations
{
    /// <inheritdoc />
    public partial class AddPeopleLocationsAndWelcome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmployeeCode",
                table: "Users",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Users",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "WelcomeAcknowledgedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Locations",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "Locations",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "Locations",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "Locations",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Locations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "Locations",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SupervisorLocations",
                columns: table => new
                {
                    SupervisorId = table.Column<int>(type: "integer", nullable: false),
                    LocationId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupervisorLocations", x => new { x.SupervisorId, x.LocationId });
                    table.ForeignKey(
                        name: "FK_SupervisorLocations_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupervisorLocations_Users_SupervisorId",
                        column: x => x.SupervisorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeCode",
                table: "Users",
                column: "EmployeeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Code",
                table: "Locations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupervisorLocations_LocationId",
                table: "SupervisorLocations",
                column: "LocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupervisorLocations");

            migrationBuilder.DropIndex(
                name: "IX_Users_EmployeeCode",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Locations_Code",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "EmployeeCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WelcomeAcknowledgedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "District",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "Locations");
        }
    }
}
