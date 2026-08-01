using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ApparkaTrainingFlowOnline.Migrations
{
    /// <inheritdoc />
    public partial class AddInteractiveTrainingContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FinalExamAnswers_AttemptId",
                table: "FinalExamAnswers");

            migrationBuilder.AddColumn<string>(
                name: "ContentKey",
                table: "Questions",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Questions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReviewTopic",
                table: "Questions",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContentJson",
                table: "LearningMaterials",
                type: "text",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<int>(
                name: "EstimatedMinutes",
                table: "LearningMaterials",
                type: "integer",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "LearningMaterials",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "FinalExamAnswers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OptionOrder",
                table: "FinalExamAnswers",
                type: "character varying(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "ABCD");

            migrationBuilder.Sql(
                """
                WITH ranked_answers AS (
                    SELECT "Id", ROW_NUMBER() OVER (PARTITION BY "AttemptId" ORDER BY "Id") AS position
                    FROM "FinalExamAnswers"
                )
                UPDATE "FinalExamAnswers" AS answer
                SET "DisplayOrder" = ranked_answers.position::integer
                FROM ranked_answers
                WHERE answer."Id" = ranked_answers."Id";
                """);

            migrationBuilder.CreateTable(
                name: "ActivityQuestionSelections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EvidenceId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    OptionOrder = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityQuestionSelections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityQuestionSelections_ActivityEvidences_EvidenceId",
                        column: x => x.EvidenceId,
                        principalTable: "ActivityEvidences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityQuestionSelections_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ContentKey",
                table: "Questions",
                column: "ContentKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinalExamAnswers_AttemptId_DisplayOrder",
                table: "FinalExamAnswers",
                columns: new[] { "AttemptId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinalExamAnswers_AttemptId_QuestionId",
                table: "FinalExamAnswers",
                columns: new[] { "AttemptId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityQuestionSelections_EvidenceId_DisplayOrder",
                table: "ActivityQuestionSelections",
                columns: new[] { "EvidenceId", "DisplayOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityQuestionSelections_EvidenceId_QuestionId",
                table: "ActivityQuestionSelections",
                columns: new[] { "EvidenceId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityQuestionSelections_QuestionId",
                table: "ActivityQuestionSelections",
                column: "QuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityQuestionSelections");

            migrationBuilder.DropIndex(
                name: "IX_Questions_ContentKey",
                table: "Questions");

            migrationBuilder.DropIndex(
                name: "IX_FinalExamAnswers_AttemptId_DisplayOrder",
                table: "FinalExamAnswers");

            migrationBuilder.DropIndex(
                name: "IX_FinalExamAnswers_AttemptId_QuestionId",
                table: "FinalExamAnswers");

            migrationBuilder.DropColumn(
                name: "ContentKey",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ReviewTopic",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ContentJson",
                table: "LearningMaterials");

            migrationBuilder.DropColumn(
                name: "EstimatedMinutes",
                table: "LearningMaterials");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "LearningMaterials");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "FinalExamAnswers");

            migrationBuilder.DropColumn(
                name: "OptionOrder",
                table: "FinalExamAnswers");

            migrationBuilder.CreateIndex(
                name: "IX_FinalExamAnswers_AttemptId",
                table: "FinalExamAnswers",
                column: "AttemptId");
        }
    }
}
