using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApparkaTrainingFlowOnline.Migrations
{
    /// <inheritdoc />
    public partial class AddExceptionalAccessControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FinalExamExceptionalAccess",
                table: "TrainingAssignments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FinalExamExceptionalAccessExpiresAt",
                table: "TrainingAssignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FinalExamExceptionalAccessGrantedAt",
                table: "TrainingAssignments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinalExamExceptionalAccessGrantedById",
                table: "TrainingAssignments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FinalExamExceptionalAccessIsSimulation",
                table: "TrainingAssignments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FinalExamExceptionalAccessReason",
                table: "TrainingAssignments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasRealExceptionalAccess",
                table: "TrainingAssignments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ExceptionalUnlockIsSimulation",
                table: "ActivityEvidences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ExceptionalUnlockReason",
                table: "ActivityEvidences",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExceptionallyUnlockedAt",
                table: "ActivityEvidences",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExceptionallyUnlockedById",
                table: "ActivityEvidences",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OriginalDueAt",
                table: "ActivityEvidences",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WasExceptionallyUnlocked",
                table: "ActivityEvidences",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalExamExceptionalAccess",
                table: "TrainingAssignments");

            migrationBuilder.DropColumn(
                name: "FinalExamExceptionalAccessExpiresAt",
                table: "TrainingAssignments");

            migrationBuilder.DropColumn(
                name: "FinalExamExceptionalAccessGrantedAt",
                table: "TrainingAssignments");

            migrationBuilder.DropColumn(
                name: "FinalExamExceptionalAccessGrantedById",
                table: "TrainingAssignments");

            migrationBuilder.DropColumn(
                name: "FinalExamExceptionalAccessIsSimulation",
                table: "TrainingAssignments");

            migrationBuilder.DropColumn(
                name: "FinalExamExceptionalAccessReason",
                table: "TrainingAssignments");

            migrationBuilder.DropColumn(
                name: "HasRealExceptionalAccess",
                table: "TrainingAssignments");

            migrationBuilder.DropColumn(
                name: "ExceptionalUnlockIsSimulation",
                table: "ActivityEvidences");

            migrationBuilder.DropColumn(
                name: "ExceptionalUnlockReason",
                table: "ActivityEvidences");

            migrationBuilder.DropColumn(
                name: "ExceptionallyUnlockedAt",
                table: "ActivityEvidences");

            migrationBuilder.DropColumn(
                name: "ExceptionallyUnlockedById",
                table: "ActivityEvidences");

            migrationBuilder.DropColumn(
                name: "OriginalDueAt",
                table: "ActivityEvidences");

            migrationBuilder.DropColumn(
                name: "WasExceptionallyUnlocked",
                table: "ActivityEvidences");
        }
    }
}
