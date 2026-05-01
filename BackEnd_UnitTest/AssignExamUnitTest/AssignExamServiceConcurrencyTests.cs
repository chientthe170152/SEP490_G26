using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Backend.Common.Errors;
using Backend.Constants;
using Backend.DTOs;
using Backend.Jobs;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace Backend_UnitTest.AssignExamUnitTest
{
    public class AssignExamServiceConcurrencyTests
    {
        private readonly Mock<IAssignExamRepository> _repo = new();
        private readonly Mock<IExamStatusScheduler> _scheduler = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly AssignExamService _service;
        private static readonly DateTime FixedNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public AssignExamServiceConcurrencyTests()
        {
            _service = new AssignExamService(_repo.Object, _currentUser.Object, _scheduler.Object,
                new FakeTimeProvider(new DateTimeOffset(FixedNow, TimeSpan.Zero)));
            _currentUser.Setup(u => u.UserId).Returns(1);
        }

        [Fact(DisplayName = "ApproveExamAsync - UTCID-Concurrency - DbUpdateConcurrencyException -> ConcurrentUpdate")]
        public async Task Approve_Concurrency()
        {
            _repo.Setup(r => r.GetExamByIdAsync(1, default))
                .ReturnsAsync(new Exam { ExamId = 1, Status = ExamStatus.Ready });
            _repo.Setup(r => r.UpdateExamStatusAsync(1, ExamStatus.Published, default))
                .ThrowsAsync(new DbUpdateConcurrencyException());

            var result = await _service.ApproveExamAsync(1);

            Assert.Equal(AssignExamErrors.ConcurrentUpdate.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CancelExamAsync - UTCID-Concurrency")]
        public async Task Cancel_Concurrency()
        {
            _repo.Setup(r => r.GetExamByIdAsync(1, default))
                .ReturnsAsync(new Exam { ExamId = 1, Status = ExamStatus.Published });
            _repo.Setup(r => r.HasSubmissionsForExamAsync(1, default)).ReturnsAsync(false);
            _repo.Setup(r => r.UpdateExamStatusAsync(1, ExamStatus.Cancelled, default))
                .ThrowsAsync(new DbUpdateConcurrencyException());

            var result = await _service.CancelExamAsync(1);

            Assert.Equal(AssignExamErrors.ConcurrentUpdate.Code, result.Error.Code);
        }

        [Fact(DisplayName = "RestoreExamAsync - UTCID-Concurrency")]
        public async Task Restore_Concurrency()
        {
            _repo.Setup(r => r.GetExamByIdAsync(1, default))
                .ReturnsAsync(new Exam
                {
                    ExamId = 1, Status = ExamStatus.Cancelled,
                    OpenAt = FixedNow.AddDays(2),
                    CloseAt = FixedNow.AddDays(3),
                    Duration = 60
                });
            _repo.Setup(r => r.UpdateExamStatusAsync(1, ExamStatus.Published, default))
                .ThrowsAsync(new DbUpdateConcurrencyException());

            var result = await _service.RestoreExamAsync(1);

            Assert.Equal(AssignExamErrors.ConcurrentUpdate.Code, result.Error.Code);
        }

        [Fact(DisplayName = "UpdateExamInfoAsync - UTCID-Concurrency")]
        public async Task UpdateExamInfo_Concurrency()
        {
            _repo.Setup(r => r.GetExamByIdAsync(1, default))
                .ReturnsAsync(new Exam { ExamId = 1, Status = ExamStatus.Ready });
            _repo.Setup(r => r.UpdateExamInfoAsync(1, It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), default))
                .ThrowsAsync(new DbUpdateConcurrencyException());

            var result = await _service.UpdateExamInfoAsync(1, new UpdateExamInfoRequest
            {
                Title = "T",
                VisibleFrom = FixedNow.AddDays(1),
                OpenAt = FixedNow.AddDays(2),
                CloseAt = FixedNow.AddDays(3)
            });

            Assert.Equal(AssignExamErrors.ConcurrentUpdate.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateAssignExamAsync - UTCID-TxRollback - SaveExam throws -> rollback + throw")]
        public async Task Create_TransactionRollback_ShouldThrow()
        {
            _repo.Setup(r => r.IsUserActiveAsync(1, default)).ReturnsAsync(true);
            _repo.Setup(r => r.GetBlueprintWithChaptersAsync(100, default))
                .ReturnsAsync(new ExamBlueprint
                {
                    ExamBlueprintId = 100, SubjectId = 1,
                    ExamBlueprintChapters = new List<ExamBlueprintChapter>
                    {
                        new() { ChapterId = 1, Difficulty = 1, TotalOfQuestions = 1 }
                    }
                });
            _repo.Setup(r => r.GetAllQuestionIdsForBlueprintRowAsync(1, 1, It.IsAny<string[]>(), default))
                .ReturnsAsync(new List<int> { 1, 2 });
            _repo.Setup(r => r.GetClassByIdAsync(10, default))
                .ReturnsAsync(new Class { ClassId = 10, TeacherId = 1, SubjectId = 1 });

            var tx = new Mock<IDbContextTransaction>();
            tx.Setup(t => t.RollbackAsync(default)).Returns(Task.CompletedTask);
            tx.Setup(t => t.Dispose());
            _repo.Setup(r => r.BeginTransactionAsync(default)).ReturnsAsync(tx.Object);
            _repo.Setup(r => r.SaveExamAsync(It.IsAny<Exam>(), default)).ThrowsAsync(new InvalidOperationException("DB error"));

            var req = new CreateAssignExamRequest
            {
                Title = "X", Duration = 60, MaxAttempts = 1, PaperCount = 1, PaperCode = 1,
                VisibleFrom = FixedNow.AddDays(1), OpenAt = FixedNow.AddDays(2), CloseAt = FixedNow.AddDays(3),
                ShuffleQuestion = false, IsPublic = false, ClassId = 10,
                GenerationMode = "blueprint", ExamBlueprintId = 100
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAssignExamAsync(req));
            tx.Verify(t => t.RollbackAsync(default), Times.Once);
        }

        [Fact(DisplayName = "CreateAssignExamAsync - UTCID-NoQuestions - papers[0] empty -> NoQuestionsSelected")]
        public async Task Create_PapersEmpty_ShouldReturnNoQuestionsSelected()
        {
            _repo.Setup(r => r.IsUserActiveAsync(1, default)).ReturnsAsync(true);
            _repo.Setup(r => r.GetBlueprintWithChaptersAsync(100, default))
                .ReturnsAsync(new ExamBlueprint
                {
                    ExamBlueprintId = 100, SubjectId = 1,
                    ExamBlueprintChapters = new List<ExamBlueprintChapter>() // no chapters -> 0 questions
                });
            _repo.Setup(r => r.GetClassByIdAsync(10, default))
                .ReturnsAsync(new Class { ClassId = 10, TeacherId = 1, SubjectId = 1 });

            var req = new CreateAssignExamRequest
            {
                Title = "X", Duration = 60, MaxAttempts = 1, PaperCount = 1, PaperCode = 1,
                VisibleFrom = FixedNow.AddDays(1), OpenAt = FixedNow.AddDays(2), CloseAt = FixedNow.AddDays(3),
                ShuffleQuestion = false, IsPublic = false, ClassId = 10,
                GenerationMode = "blueprint", ExamBlueprintId = 100
            };

            var result = await _service.CreateAssignExamAsync(req);

            Assert.Equal(AssignExamErrors.NoQuestionsSelected.Code, result.Error.Code);
        }

        private sealed class FakeTimeProvider : TimeProvider
        {
            private readonly DateTimeOffset _now;
            public FakeTimeProvider(DateTimeOffset now) => _now = now;
            public override DateTimeOffset GetUtcNow() => _now;
        }
    }
}
