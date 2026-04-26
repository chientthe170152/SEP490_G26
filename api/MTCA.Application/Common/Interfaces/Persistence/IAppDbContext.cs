using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
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

namespace MTCA.Application.Common.Interfaces.Persistence;

public interface IAppDbContext
{
    DbSet<ApplicationUser> Users { get; }
    DbSet<ApplicationRole> Roles { get; }
    DbSet<IdentityUserRole<Guid>> UserRoles { get; }
    DbSet<UserProfile> UserProfiles { get; }

    DbSet<Subject> Subjects { get; }
    DbSet<Chapter> Chapters { get; }
    DbSet<Semester> Semesters { get; }

    DbSet<Classroom> Classrooms { get; }
    DbSet<ClassroomStudent> ClassroomStudents { get; }

    DbSet<Domain.QuestionBank.QuestionBank> QuestionBanks { get; }
    DbSet<Question> Questions { get; }
    DbSet<QuestionVersion> QuestionVersions { get; }
    DbSet<QuestionProposal> QuestionProposals { get; }
    DbSet<QuestionOption> QuestionOptions { get; }
    DbSet<QuestionBlank> QuestionBlanks { get; }
    DbSet<PrtRule> PrtRules { get; }

    DbSet<ExamBlueprint> ExamBlueprints { get; }
    DbSet<ExamBlueprintVersion> ExamBlueprintVersions { get; }
    DbSet<BlueprintCell> BlueprintCells { get; }

    DbSet<Exam> Exams { get; }
    DbSet<ExamVariant> ExamVariants { get; }
    DbSet<ExamVariantQuestion> ExamVariantQuestions { get; }

    DbSet<ExamSession> ExamSessions { get; }
    DbSet<Submission> Submissions { get; }
    DbSet<SubmissionItem> SubmissionItems { get; }

    DbSet<PracticeSession> PracticeSessions { get; }
    DbSet<PracticeItem> PracticeItems { get; }

    DbSet<GeneratedVariant> GeneratedVariants { get; }
    DbSet<EventLog> EventLogs { get; }

    DatabaseFacade Database { get; }
    ChangeTracker ChangeTracker { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
