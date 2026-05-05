using Microsoft.EntityFrameworkCore;

namespace Backend.Models;

public partial class MtcaSep490G26Context
{
    // DbSets for new entities (scaffolded context will also have these after regen — no conflict)
    public virtual DbSet<QuestionBank>                  QuestionBanks                  { get; set; }
    public virtual DbSet<QuestionPromotionRequest>      QuestionPromotionRequests      { get; set; }
    public virtual DbSet<QuestionPromotionRequestItem>  QuestionPromotionRequestItems  { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Exam>(entity =>
        {
            entity.Property(e => e.Status)
                .ValueGeneratedNever();
        });

        modelBuilder.Entity<Semester>().Property(e => e.ConcurrencyStamp).IsRowVersion();
        modelBuilder.Entity<Subject>().Property(e => e.ConcurrencyStamp).IsRowVersion();
        modelBuilder.Entity<Chapter>().Property(e => e.ConcurrencyStamp).IsRowVersion();

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.MustChangePassword)
                .HasDefaultValue(false);
        });

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.Property(e => e.GradingStatus)
                .HasDefaultValue((byte)0);

            entity.Property(e => e.GradingError)
                .HasMaxLength(500);
        });

        modelBuilder.Entity<StudentAnswer>(entity =>
        {
            entity.Property(e => e.PointsEarned)
                .HasColumnType("decimal(5, 2)");

            entity.HasIndex(e => new { e.SubmissionId, e.IsCorrect })
                .HasDatabaseName("IX_StudentAnswers_Submission_IsCorrect");
        });

        // ── QuestionBank ────────────────────────────────────────────────────
        modelBuilder.Entity<QuestionBank>(e =>
        {
            e.HasKey(x => x.QuestionBankId);
            e.Property(x => x.ConcurrencyStamp).IsRowVersion();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Status).HasDefaultValue(1);
            e.Property(x => x.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");
            // NOTE: UpdatedAtUtc DEFAULT only fires at INSERT. Service layer must set it on every UPDATE.
            e.Property(x => x.UpdatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

            e.HasOne(x => x.Subject)
                .WithMany(s => s.QuestionBanks)
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_QB_Subjects");

            e.HasOne(x => x.Owner)
                .WithMany(u => u.OwnedBanks)
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_QB_Owner");

            e.HasOne(x => x.CreatedByUser)
                .WithMany(u => u.CreatedBanks)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_QB_CreatedBy");

            e.HasOne(x => x.UpdatedByUser)
                .WithMany(u => u.UpdatedBanks)
                .HasForeignKey(x => x.UpdatedByUserId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_QB_UpdatedBy");

            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_QB_OwnerShape",
                    "(OwnerType = 1 AND OwnerUserId IS NOT NULL) OR (OwnerType = 2 AND OwnerUserId IS NULL)");
                t.HasCheckConstraint("CK_QB_NameNotEmpty",
                    "LEN(LTRIM(RTRIM(Name))) > 0");
            });
        });

        // ── Question → QuestionBank FK ──────────────────────────────────────
        modelBuilder.Entity<Question>()
            .HasOne(x => x.QuestionBank)
            .WithMany(b => b.Questions)
            .HasForeignKey(x => x.QuestionBankId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Questions_QuestionBanks");

        // ── QuestionPromotionRequest ─────────────────────────────────────────
        modelBuilder.Entity<QuestionPromotionRequest>(e =>
        {
            e.HasKey(x => x.PromotionRequestId);
            e.Property(x => x.ConcurrencyStamp).IsRowVersion();
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.Status).HasDefaultValue(1);
            e.Property(x => x.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

            e.HasOne(x => x.SourcePersonalBank)
                .WithMany(b => b.SourcePromotionRequests)
                .HasForeignKey(x => x.SourcePersonalBankId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_QPR_Source");

            e.HasOne(x => x.TargetSharedBank)
                .WithMany(b => b.TargetPromotionRequests)
                .HasForeignKey(x => x.TargetSharedBankId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_QPR_Target");

            e.HasOne(x => x.RequestedByUser)
                .WithMany(u => u.SentPromotionRequests)
                .HasForeignKey(x => x.RequestedByUserId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_QPR_RequestedBy");

            e.HasOne(x => x.ResolvedByUser)
                .WithMany(u => u.ResolvedPromotionRequests)
                .HasForeignKey(x => x.ResolvedByUserId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_QPR_ResolvedBy");
        });

        // ── QuestionPromotionRequestItem ─────────────────────────────────────
        modelBuilder.Entity<QuestionPromotionRequestItem>(e =>
        {
            e.HasKey(x => new { x.PromotionRequestId, x.QuestionId });
            e.Property(x => x.ConcurrencyStamp).IsRowVersion();
            e.Property(x => x.RejectionReason).HasMaxLength(500);
            e.Property(x => x.Status).HasDefaultValue(1);

            e.HasOne(x => x.Request)
                .WithMany(r => r.Items)
                .HasForeignKey(x => x.PromotionRequestId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_QPRI_Request");

            e.HasOne(x => x.Question)
                .WithMany()
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_QPRI_Question");

            e.HasOne(x => x.ResolvedByUser)
                .WithMany()
                .HasForeignKey(x => x.ResolvedByUserId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_QPRI_ResolvedBy");

            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_QPRI_RejectionReason",
                    "(Status = 3 AND RejectionReason IS NOT NULL AND LEN(LTRIM(RTRIM(RejectionReason))) > 0) OR Status <> 3");
            });
        });
    }
}

