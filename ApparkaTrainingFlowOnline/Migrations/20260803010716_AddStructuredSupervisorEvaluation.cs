using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApparkaTrainingFlowOnline.Migrations
{
    /// <inheritdoc />
    public partial class AddStructuredSupervisorEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GuidanceProvided",
                table: "RubricEvaluations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MainImprovement",
                table: "ActivityEvidences",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObservedStrength",
                table: "ActivityEvidences",
                type: "text",
                nullable: false,
                defaultValue: "NotSelected");

            migrationBuilder.AddColumn<string>(
                name: "OverallAssessment",
                table: "ActivityEvidences",
                type: "text",
                nullable: false,
                defaultValue: "NotEvaluated");

            migrationBuilder.AddColumn<string>(
                name: "OverallEvidence",
                table: "ActivityEvidences",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GuidanceProvided",
                table: "RubricEvaluations");

            migrationBuilder.DropColumn(
                name: "MainImprovement",
                table: "ActivityEvidences");

            migrationBuilder.DropColumn(
                name: "ObservedStrength",
                table: "ActivityEvidences");

            migrationBuilder.DropColumn(
                name: "OverallAssessment",
                table: "ActivityEvidences");

            migrationBuilder.DropColumn(
                name: "OverallEvidence",
                table: "ActivityEvidences");
        }
    }
}
