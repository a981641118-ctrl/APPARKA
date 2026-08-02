using ApparkaTrainingFlowOnline.Models;
using Microsoft.EntityFrameworkCore;

namespace ApparkaTrainingFlowOnline.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<SupervisorLocation> SupervisorLocations => Set<SupervisorLocation>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<TrainingAssignment> TrainingAssignments => Set<TrainingAssignment>();
    public DbSet<ActivityTemplate> ActivityTemplates => Set<ActivityTemplate>();
    public DbSet<ActivityEvidence> ActivityEvidences => Set<ActivityEvidence>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<ActivityAnswer> ActivityAnswers => Set<ActivityAnswer>();
    public DbSet<ActivityQuestionSelection> ActivityQuestionSelections => Set<ActivityQuestionSelection>();
    public DbSet<RubricEvaluation> RubricEvaluations => Set<RubricEvaluation>();
    public DbSet<ValidationSession> ValidationSessions => Set<ValidationSession>();
    public DbSet<FinalExamAttempt> FinalExamAttempts => Set<FinalExamAttempt>();
    public DbSet<FinalExamAnswer> FinalExamAnswers => Set<FinalExamAnswer>();
    public DbSet<LearningMaterial> LearningMaterials => Set<LearningMaterial>();
    public DbSet<LearningMaterialProgress> LearningMaterialProgress => Set<LearningMaterialProgress>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<AppUser>().HasIndex(x => x.EmployeeCode).IsUnique();
        modelBuilder.Entity<Location>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<SupervisorLocation>().HasKey(x => new { x.SupervisorId, x.LocationId });
        modelBuilder.Entity<SupervisorLocation>()
            .HasOne(x => x.Supervisor).WithMany(x => x.SupervisorLocations)
            .HasForeignKey(x => x.SupervisorId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SupervisorLocation>()
            .HasOne(x => x.Location).WithMany(x => x.Supervisors)
            .HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ActivityTemplate>().HasIndex(x => x.Sequence).IsUnique();
        modelBuilder.Entity<ActivityEvidence>().HasIndex(x => new { x.AssignmentId, x.Sequence }).IsUnique();
        modelBuilder.Entity<ActivityQuestionSelection>().HasIndex(x => new { x.EvidenceId, x.QuestionId }).IsUnique();
        modelBuilder.Entity<ActivityQuestionSelection>().HasIndex(x => new { x.EvidenceId, x.DisplayOrder }).IsUnique();
        modelBuilder.Entity<FinalExamAttempt>().HasIndex(x => new { x.AssignmentId, x.AttemptNumber }).IsUnique();
        modelBuilder.Entity<FinalExamAnswer>().HasIndex(x => new { x.AttemptId, x.DisplayOrder }).IsUnique();
        modelBuilder.Entity<FinalExamAnswer>().HasIndex(x => new { x.AttemptId, x.QuestionId }).IsUnique();
        modelBuilder.Entity<ValidationSession>().HasIndex(x => x.CodeHash);
        modelBuilder.Entity<LearningMaterialProgress>().HasIndex(x => new { x.UserId, x.MaterialId }).IsUnique();
        modelBuilder.Entity<Question>().HasIndex(x => x.ContentKey).IsUnique();

        modelBuilder.Entity<TrainingAssignment>()
            .HasOne(x => x.Collaborator).WithMany().HasForeignKey(x => x.CollaboratorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TrainingAssignment>()
            .HasOne(x => x.Supervisor).WithMany().HasForeignKey(x => x.SupervisorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TrainingAssignment>()
            .HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ValidationSession>()
            .HasOne(x => x.GeneratedBy).WithMany().HasForeignKey(x => x.GeneratedById).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TrainingAssignment>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<ActivityEvidence>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<RubricEvaluation>().Property(x => x.Rating).HasConversion<string>();
        modelBuilder.Entity<AuditLog>().Property(x => x.Severity).HasConversion<string>();
    }
}
