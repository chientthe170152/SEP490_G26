using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionBankAndPromotion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuestionPurpose",
                table: "Questions");

            migrationBuilder.AddColumn<int>(
                name: "QuestionBankId",
                table: "Questions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "QuestionBanks",
                columns: table => new
                {
                    QuestionBankId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Purpose = table.Column<byte>(type: "tinyint", nullable: false),
                    OwnerType = table.Column<byte>(type: "tinyint", nullable: false),
                    OwnerUserId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ConcurrencyStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionBanks", x => x.QuestionBankId);
                    table.CheckConstraint("CK_QB_NameNotEmpty", "LEN(LTRIM(RTRIM(Name))) > 0");
                    table.CheckConstraint("CK_QB_OwnerShape", "(OwnerType = 1 AND OwnerUserId IS NOT NULL) OR (OwnerType = 2 AND OwnerUserId IS NULL)");
                    table.ForeignKey(
                        name: "FK_QB_CreatedBy",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_QB_Owner",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_QB_Subjects",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "SubjectId");
                    table.ForeignKey(
                        name: "FK_QB_UpdatedBy",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "QuestionPromotionRequests",
                columns: table => new
                {
                    PromotionRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    SourcePersonalBankId = table.Column<int>(type: "int", nullable: false),
                    TargetSharedBankId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedByUserId = table.Column<int>(type: "int", nullable: true),
                    ConcurrencyStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionPromotionRequests", x => x.PromotionRequestId);
                    table.ForeignKey(
                        name: "FK_QPR_RequestedBy",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_QPR_ResolvedBy",
                        column: x => x.ResolvedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_QPR_Source",
                        column: x => x.SourcePersonalBankId,
                        principalTable: "QuestionBanks",
                        principalColumn: "QuestionBankId");
                    table.ForeignKey(
                        name: "FK_QPR_Target",
                        column: x => x.TargetSharedBankId,
                        principalTable: "QuestionBanks",
                        principalColumn: "QuestionBankId");
                });

            migrationBuilder.CreateTable(
                name: "QuestionPromotionRequestItems",
                columns: table => new
                {
                    PromotionRequestId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedByUserId = table.Column<int>(type: "int", nullable: true),
                    ConcurrencyStamp = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionPromotionRequestItems", x => new { x.PromotionRequestId, x.QuestionId });
                    table.CheckConstraint("CK_QPRI_RejectionReason", "(Status = 3 AND RejectionReason IS NOT NULL AND LEN(LTRIM(RTRIM(RejectionReason))) > 0) OR Status <> 3");
                    table.ForeignKey(
                        name: "FK_QPRI_Question",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "QuestionId");
                    table.ForeignKey(
                        name: "FK_QPRI_Request",
                        column: x => x.PromotionRequestId,
                        principalTable: "QuestionPromotionRequests",
                        principalColumn: "PromotionRequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QPRI_ResolvedBy",
                        column: x => x.ResolvedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Questions_QuestionBankId",
                table: "Questions",
                column: "QuestionBankId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionBanks_CreatedByUserId",
                table: "QuestionBanks",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionBanks_OwnerUserId",
                table: "QuestionBanks",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionBanks_SubjectId",
                table: "QuestionBanks",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionBanks_UpdatedByUserId",
                table: "QuestionBanks",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionPromotionRequestItems_QuestionId",
                table: "QuestionPromotionRequestItems",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionPromotionRequestItems_ResolvedByUserId",
                table: "QuestionPromotionRequestItems",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionPromotionRequests_RequestedByUserId",
                table: "QuestionPromotionRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionPromotionRequests_ResolvedByUserId",
                table: "QuestionPromotionRequests",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionPromotionRequests_SourcePersonalBankId",
                table: "QuestionPromotionRequests",
                column: "SourcePersonalBankId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionPromotionRequests_TargetSharedBankId",
                table: "QuestionPromotionRequests",
                column: "TargetSharedBankId");

            // ── Filtered unique index: 1 Shared Exam + 1 Shared Practice per Subject ──────
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX UQ_QB_SharedPerSubject " +
                "ON dbo.QuestionBanks (SubjectId, Purpose) WHERE OwnerType = 2;");

            // ── Filtered index: Personal Bank lookup by owner ─────────────────────────────
            migrationBuilder.Sql(
                "CREATE INDEX IX_QB_Personal " +
                "ON dbo.QuestionBanks (OwnerUserId, SubjectId, Purpose) WHERE OwnerType = 1;");

            // ── Data migration 6.1: Shared Banks for all existing Subjects ────────────────
            // Name formula (approved override of CLAUDE.md "no Vietnamese strings" rule):
            // "Kho chung Kiểm tra {Subject.Name} {Subject.Code}" / "Kho chung Luyện tập ..."
            migrationBuilder.Sql(@"
                DECLARE @adminId INT = (
                    SELECT TOP 1 u.UserId
                    FROM Users u
                    JOIN Roles r ON u.RoleId = r.RoleId
                    WHERE r.Name = N'Admin'
                    ORDER BY u.UserId
                );
                IF @adminId IS NULL SET @adminId = 1;

                INSERT INTO dbo.QuestionBanks
                    (SubjectId, Name, Description, Purpose, OwnerType, OwnerUserId, Status,
                     CreatedByUserId, UpdatedByUserId)
                SELECT
                    s.SubjectId,
                    N'Kho chung Kiem tra ' + s.Name + N' ' + ISNULL(s.Code, N''),
                    NULL, 1, 2, NULL, 1, @adminId, @adminId
                FROM Subjects s
                UNION ALL
                SELECT
                    s.SubjectId,
                    N'Kho chung Luyen tap ' + s.Name + N' ' + ISNULL(s.Code, N''),
                    NULL, 2, 2, NULL, 1, @adminId, @adminId
                FROM Subjects s;
            ");

            // ── Data migration 6.2: Personal Banks for each (teacher, subject) combo ──────
            migrationBuilder.Sql(@"
                INSERT INTO dbo.QuestionBanks
                    (SubjectId, Name, Description, Purpose, OwnerType, OwnerUserId, Status,
                     CreatedByUserId, UpdatedByUserId)
                SELECT DISTINCT
                    c.SubjectId,
                    -- Name rõ hơn để phân biệt với Shared Bank trên admin UI
                    N'Ngan hang ca nhan ' + s.Name + N' ' + ISNULL(s.Code, N''),
                    NULL,
                    1,
                    1,
                    q.CreatedByUserId,
                    1,
                    q.CreatedByUserId,
                    q.CreatedByUserId
                FROM Questions q
                JOIN Chapters c  ON c.ChapterId  = q.ChapterId
                JOIN Subjects s  ON s.SubjectId  = c.SubjectId
                WHERE NOT EXISTS (
                    SELECT 1 FROM QuestionBanks qb
                    WHERE qb.OwnerType   = 1
                      AND qb.OwnerUserId = q.CreatedByUserId
                      AND qb.SubjectId   = c.SubjectId
                );
            ");

            // ── Data migration 6.3: Backfill Questions.QuestionBankId ─────────────────────
            // Match each question to the single Personal Bank for (owner, subject).
            // Purpose=1 (Exam) is the only personal bank per (owner,subject) at this stage
            // (6.2 inserts Purpose=1 only). Phase 3 will allow multiple banks per subject.
            migrationBuilder.Sql(@"
                UPDATE q
                SET q.QuestionBankId = qb.QuestionBankId
                FROM Questions q
                JOIN Chapters c  ON c.ChapterId  = q.ChapterId
                JOIN QuestionBanks qb
                   ON qb.SubjectId   = c.SubjectId
                  AND qb.OwnerType   = 1
                  AND qb.OwnerUserId = q.CreatedByUserId;
            ");

            // ── Composite index on Questions for pool queries ─────────────────────────────
            migrationBuilder.Sql(
                "CREATE INDEX IX_Questions_BankChapterDifficulty " +
                "ON dbo.Questions (QuestionBankId, ChapterId, Difficulty) " +
                "INCLUDE (Status) WHERE Status IN ('Active', 'Inprogress');");

            migrationBuilder.AddForeignKey(
                name: "FK_Questions_QuestionBanks",
                table: "Questions",
                column: "QuestionBankId",
                principalTable: "QuestionBanks",
                principalColumn: "QuestionBankId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Questions_QuestionBanks",
                table: "Questions");

            migrationBuilder.DropTable(
                name: "QuestionPromotionRequestItems");

            migrationBuilder.DropTable(
                name: "QuestionPromotionRequests");

            migrationBuilder.DropTable(
                name: "QuestionBanks");

            migrationBuilder.DropIndex(
                name: "IX_Questions_QuestionBankId",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "QuestionBankId",
                table: "Questions");

            migrationBuilder.AddColumn<byte>(
                name: "QuestionPurpose",
                table: "Questions",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);
        }
    }
}
