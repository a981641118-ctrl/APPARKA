using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ApparkaTrainingFlowOnline.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    WeekNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    Instructions = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    PracticalChallenge = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Actor = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Role = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Detail = table.Column<string>(type: "character varying(1200)", maxLength: 1200, nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Address = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Email = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "boolean", nullable: false),
                    ActivationToken = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ActivationExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ActivityTemplateId = table.Column<int>(type: "integer", nullable: true),
                    IsFinalExamQuestion = table.Column<bool>(type: "boolean", nullable: false),
                    Prompt = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OptionA = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    OptionB = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    OptionC = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    OptionD = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CorrectOption = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    Explanation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_ActivityTemplates_ActivityTemplateId",
                        column: x => x.ActivityTemplateId,
                        principalTable: "ActivityTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LearningMaterials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PositionId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Summary = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: false),
                    VideoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningMaterials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningMaterials_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CollaboratorId = table.Column<int>(type: "integer", nullable: false),
                    SupervisorId = table.Column<int>(type: "integer", nullable: false),
                    CreatedById = table.Column<int>(type: "integer", nullable: false),
                    LocationId = table.Column<int>(type: "integer", nullable: false),
                    PositionId = table.Column<int>(type: "integer", nullable: false),
                    AccessFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    OverallActivityScore = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingAssignments_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingAssignments_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingAssignments_Users_CollaboratorId",
                        column: x => x.CollaboratorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingAssignments_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingAssignments_Users_SupervisorId",
                        column: x => x.SupervisorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LearningMaterialProgress",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    MaterialId = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningMaterialProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningMaterialProgress_LearningMaterials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "LearningMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LearningMaterialProgress_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityEvidences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssignmentId = table.Column<int>(type: "integer", nullable: false),
                    TemplateId = table.Column<int>(type: "integer", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    WeekNumber = table.Column<int>(type: "integer", nullable: false),
                    AvailableFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DueAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CollaboratorSubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SupervisorSubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AssignedChallenge = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    KnowledgeScore = table.Column<int>(type: "integer", nullable: false),
                    PracticalScore = table.Column<int>(type: "integer", nullable: false),
                    FinalScore = table.Column<int>(type: "integer", nullable: false),
                    StartIp = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    StartUserAgent = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    StartDeviceId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    SupervisorIp = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    SupervisorUserAgent = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: true),
                    SupervisorDeviceId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    PossibleSharedDevice = table.Column<bool>(type: "boolean", nullable: false),
                    SupervisorFeedback = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityEvidences_ActivityTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "ActivityTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityEvidences_TrainingAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "TrainingAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinalExamAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AssignmentId = table.Column<int>(type: "integer", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Passed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinalExamAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinalExamAttempts_TrainingAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "TrainingAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EvidenceId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    SelectedOption = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityAnswers_ActivityEvidences_EvidenceId",
                        column: x => x.EvidenceId,
                        principalTable: "ActivityEvidences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityAnswers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RubricEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EvidenceId = table.Column<int>(type: "integer", nullable: false),
                    Criterion = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    IsCritical = table.Column<bool>(type: "boolean", nullable: false),
                    Rating = table.Column<string>(type: "text", nullable: false),
                    Observation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RubricEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RubricEvaluations_ActivityEvidences_EvidenceId",
                        column: x => x.EvidenceId,
                        principalTable: "ActivityEvidences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ValidationSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EvidenceId = table.Column<int>(type: "integer", nullable: false),
                    GeneratedById = table.Column<int>(type: "integer", nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UsedById = table.Column<int>(type: "integer", nullable: true),
                    UsedIp = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValidationSessions_ActivityEvidences_EvidenceId",
                        column: x => x.EvidenceId,
                        principalTable: "ActivityEvidences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ValidationSessions_Users_GeneratedById",
                        column: x => x.GeneratedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinalExamAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttemptId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    SelectedOption = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinalExamAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinalExamAnswers_FinalExamAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "FinalExamAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinalExamAnswers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityAnswers_EvidenceId",
                table: "ActivityAnswers",
                column: "EvidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityAnswers_QuestionId",
                table: "ActivityAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEvidences_AssignmentId_Sequence",
                table: "ActivityEvidences",
                columns: new[] { "AssignmentId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEvidences_TemplateId",
                table: "ActivityEvidences",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTemplates_Sequence",
                table: "ActivityTemplates",
                column: "Sequence",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinalExamAnswers_AttemptId",
                table: "FinalExamAnswers",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_FinalExamAnswers_QuestionId",
                table: "FinalExamAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_FinalExamAttempts_AssignmentId_AttemptNumber",
                table: "FinalExamAttempts",
                columns: new[] { "AssignmentId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningMaterialProgress_MaterialId",
                table: "LearningMaterialProgress",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningMaterialProgress_UserId_MaterialId",
                table: "LearningMaterialProgress",
                columns: new[] { "UserId", "MaterialId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningMaterials_PositionId",
                table: "LearningMaterials",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_ActivityTemplateId",
                table: "Questions",
                column: "ActivityTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_RubricEvaluations_EvidenceId",
                table: "RubricEvaluations",
                column: "EvidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignments_CollaboratorId",
                table: "TrainingAssignments",
                column: "CollaboratorId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignments_CreatedById",
                table: "TrainingAssignments",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignments_LocationId",
                table: "TrainingAssignments",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignments_PositionId",
                table: "TrainingAssignments",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignments_SupervisorId",
                table: "TrainingAssignments",
                column: "SupervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValidationSessions_CodeHash",
                table: "ValidationSessions",
                column: "CodeHash");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationSessions_EvidenceId",
                table: "ValidationSessions",
                column: "EvidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationSessions_GeneratedById",
                table: "ValidationSessions",
                column: "GeneratedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityAnswers");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "FinalExamAnswers");

            migrationBuilder.DropTable(
                name: "LearningMaterialProgress");

            migrationBuilder.DropTable(
                name: "RubricEvaluations");

            migrationBuilder.DropTable(
                name: "ValidationSessions");

            migrationBuilder.DropTable(
                name: "FinalExamAttempts");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "LearningMaterials");

            migrationBuilder.DropTable(
                name: "ActivityEvidences");

            migrationBuilder.DropTable(
                name: "ActivityTemplates");

            migrationBuilder.DropTable(
                name: "TrainingAssignments");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
