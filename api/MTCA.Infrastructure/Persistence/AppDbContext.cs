using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MTCA.Application.Common.Interfaces.Persistence;
using MTCA.Domain.Analytics;
using MTCA.Domain.Classrooms;
using MTCA.Domain.ExamBlueprints;
using MTCA.Domain.ExamSessions;
using MTCA.Domain.Exams;
using MTCA.Domain.Identity;
using MTCA.Domain.Logging;
using MTCA.Domain.MasterData;
using MTCA.Domain.Practice;
using MTCA.Domain.QuestionBank;

namespace MTCA.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IAppDbContext
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<Semester> Semesters => Set<Semester>();

    public DbSet<Classroom> Classrooms => Set<Classroom>();
    public DbSet<ClassroomStudent> ClassroomStudents => Set<ClassroomStudent>();

    public DbSet<Domain.QuestionBank.QuestionBank> QuestionBanks => Set<Domain.QuestionBank.QuestionBank>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionVersion> QuestionVersions => Set<QuestionVersion>();
    public DbSet<QuestionProposal> QuestionProposals => Set<QuestionProposal>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<QuestionBlank> QuestionBlanks => Set<QuestionBlank>();
    public DbSet<PrtRule> PrtRules => Set<PrtRule>();

    public DbSet<ExamBlueprint> ExamBlueprints => Set<ExamBlueprint>();
    public DbSet<ExamBlueprintVersion> ExamBlueprintVersions => Set<ExamBlueprintVersion>();
    public DbSet<BlueprintCell> BlueprintCells => Set<BlueprintCell>();

    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamVariant> ExamVariants => Set<ExamVariant>();
    public DbSet<ExamVariantQuestion> ExamVariantQuestions => Set<ExamVariantQuestion>();

    public DbSet<ExamSession> ExamSessions => Set<ExamSession>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<SubmissionItem> SubmissionItems => Set<SubmissionItem>();

    public DbSet<PracticeSession> PracticeSessions => Set<PracticeSession>();
    public DbSet<PracticeItem> PracticeItems => Set<PracticeItem>();

    public DbSet<GeneratedVariant> GeneratedVariants => Set<GeneratedVariant>();
    public DbSet<EventLog> EventLogs => Set<EventLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
