using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Backend.Common.Errors;
using Backend.DTOs;
using Backend.Jobs;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Moq;
using Xunit;

namespace Backend_UnitTest.AssignExamTests
{
    public class SwapPaperQuestionAsync_UTCID_Tests
    {
        private readonly Mock<IAssignExamRepository> _repoMock;
        private readonly AssignExamService _service;
        private readonly CancellationToken _ct = CancellationToken.None;

        public SwapPaperQuestionAsync_UTCID_Tests()
        {
            _repoMock = new Mock<IAssignExamRepository>(MockBehavior.Strict);
            var currentUserMock = new Mock<ICurrentUserService>();
            var schedulerMock = new Mock<IExamStatusScheduler>();
            _service = new AssignExamService(
                _repoMock.Object,
                currentUserMock.Object,
                schedulerMock.Object,
                TimeProvider.System);
        }

        private static SwapQuestionRequestDto MakeRequest(int? paperId, int? oldId, int? newId, bool? swapGlobal = false) =>
            new SwapQuestionRequestDto { PaperId = paperId, OldQuestionId = oldId, NewQuestionId = newId, SwapGlobal = swapGlobal };

        [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID01 - Valid request -> swap in paper successfully")]
        public async Task SwapPaperQuestionAsync_UTCID01_ValidRequest_ShouldSwapInPaper()
        {
            // Arrange
            var request = MakeRequest(1, 100, 200, false);
            var paper = CreatePaperWithOldQuestion(paperId: 1, examId: 10, oldQuestionId: 100, chapterId: 5, difficulty: 2);
            var newQuestion = CreateQuestion(questionId: 200, chapterId: 5, difficulty: 2, status: "Active");

            _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(request.PaperId!.Value, _ct)).ReturnsAsync(paper);
            _repoMock.Setup(r => r.GetQuestionByIdAsync(request.NewQuestionId!.Value, _ct)).ReturnsAsync(newQuestion);
            _repoMock.Setup(r => r.SwapPaperQuestionAsync(request.PaperId!.Value, request.OldQuestionId!.Value, request.NewQuestionId!.Value, _ct))
                     .Returns(Task.CompletedTask);

            // Act
            var result = await _service.SwapPaperQuestionAsync(request, _ct);

            // Assert
            Assert.True(result.IsSuccess);
            _repoMock.Verify(r => r.SwapPaperQuestionAsync(request.PaperId!.Value, request.OldQuestionId!.Value, request.NewQuestionId!.Value, _ct), Times.Once);
            _repoMock.Verify(r => r.SwapExamQuestionGloballyAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _repoMock.VerifyAll();
        }

        [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID02 - Missing PaperId -> SwapMissingFields error")]
        public async Task SwapPaperQuestionAsync_UTCID02_MissingPaperId_ShouldReturnSwapMissingFieldsError()
        {
            // Arrange
            var request = MakeRequest(null, 100, 200, false);

            // Act
            var result = await _service.SwapPaperQuestionAsync(request, _ct);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(AssignExamErrors.SwapMissingFields.Code, result.Error.Code);
        }

        [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID03 - Paper not found -> PaperNotFound error")]
        public async Task SwapPaperQuestionAsync_UTCID03_PaperNotFound_ShouldReturnPaperNotFoundError()
        {
            // Arrange
            var request = MakeRequest(999, 100, 200, false);
            _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(request.PaperId!.Value, _ct)).ReturnsAsync((Paper?)null);

            // Act
            var result = await _service.SwapPaperQuestionAsync(request, _ct);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(AssignExamErrors.PaperNotFound.Code, result.Error.Code);
            _repoMock.Verify(r => r.GetQuestionByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _repoMock.VerifyAll();
        }

        [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID04 - Old question not in paper -> QuestionNotInPaper error")]
        public async Task SwapPaperQuestionAsync_UTCID04_OldQuestionNotInPaper_ShouldReturnQuestionNotInPaperError()
        {
            // Arrange
            var request = MakeRequest(1, 999, 200, false);
            var paper = CreatePaperWithOldQuestion(paperId: 1, examId: 10, oldQuestionId: 100, chapterId: 5, difficulty: 2);
            _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(request.PaperId!.Value, _ct)).ReturnsAsync(paper);

            // Act
            var result = await _service.SwapPaperQuestionAsync(request, _ct);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(AssignExamErrors.QuestionNotInPaper.Code, result.Error.Code);
            _repoMock.Verify(r => r.GetQuestionByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _repoMock.VerifyAll();
        }

        [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID05 - New question not found -> QuestionNotFound error")]
        public async Task SwapPaperQuestionAsync_UTCID05_NewQuestionNotFound_ShouldReturnQuestionNotFoundError()
        {
            // Arrange
            var request = MakeRequest(1, 100, 999, false);
            var paper = CreatePaperWithOldQuestion(paperId: 1, examId: 10, oldQuestionId: 100, chapterId: 5, difficulty: 2);
            _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(request.PaperId!.Value, _ct)).ReturnsAsync(paper);
            _repoMock.Setup(r => r.GetQuestionByIdAsync(request.NewQuestionId!.Value, _ct)).ReturnsAsync((Question?)null);

            // Act
            var result = await _service.SwapPaperQuestionAsync(request, _ct);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(AssignExamErrors.QuestionNotFound.Code, result.Error.Code);
            _repoMock.VerifyAll();
        }

        [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID06 - New question inactive -> QuestionInactive error")]
        public async Task SwapPaperQuestionAsync_UTCID06_NewQuestionInactive_ShouldReturnQuestionInactiveError()
        {
            // Arrange
            var request = MakeRequest(1, 100, 201, false);
            var paper = CreatePaperWithOldQuestion(paperId: 1, examId: 10, oldQuestionId: 100, chapterId: 5, difficulty: 2);
            var newQuestion = CreateQuestion(questionId: 201, chapterId: 5, difficulty: 2, status: "Draft");
            _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(request.PaperId!.Value, _ct)).ReturnsAsync(paper);
            _repoMock.Setup(r => r.GetQuestionByIdAsync(request.NewQuestionId!.Value, _ct)).ReturnsAsync(newQuestion);

            // Act
            var result = await _service.SwapPaperQuestionAsync(request, _ct);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(AssignExamErrors.QuestionInactive.Code, result.Error.Code);
            _repoMock.VerifyAll();
        }

        [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID07 - Difficulty mismatch -> DifficultyMismatch error")]
        public async Task SwapPaperQuestionAsync_UTCID07_DifficultyMismatch_ShouldReturnDifficultyMismatchError()
        {
            // Arrange
            var request = MakeRequest(1, 100, 202, false);
            var paper = CreatePaperWithOldQuestion(paperId: 1, examId: 10, oldQuestionId: 100, chapterId: 5, difficulty: 2);
            var newQuestion = CreateQuestion(questionId: 202, chapterId: 5, difficulty: 3, status: "Active");
            _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(request.PaperId!.Value, _ct)).ReturnsAsync(paper);
            _repoMock.Setup(r => r.GetQuestionByIdAsync(request.NewQuestionId!.Value, _ct)).ReturnsAsync(newQuestion);

            // Act
            var result = await _service.SwapPaperQuestionAsync(request, _ct);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(AssignExamErrors.DifficultyMismatch.Code, result.Error.Code);
            _repoMock.VerifyAll();
        }

        [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID08 - Chapter mismatch -> ChapterMismatch error")]
        public async Task SwapPaperQuestionAsync_UTCID08_ChapterMismatch_ShouldReturnChapterMismatchError()
        {
            // Arrange
            var request = MakeRequest(1, 100, 203, false);
            var paper = CreatePaperWithOldQuestion(paperId: 1, examId: 10, oldQuestionId: 100, chapterId: 5, difficulty: 2);
            var newQuestion = CreateQuestion(questionId: 203, chapterId: 6, difficulty: 2, status: "Active");
            _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(request.PaperId!.Value, _ct)).ReturnsAsync(paper);
            _repoMock.Setup(r => r.GetQuestionByIdAsync(request.NewQuestionId!.Value, _ct)).ReturnsAsync(newQuestion);

            // Act
            var result = await _service.SwapPaperQuestionAsync(request, _ct);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(AssignExamErrors.ChapterMismatch.Code, result.Error.Code);
            _repoMock.VerifyAll();
        }

        [Fact(DisplayName = "SwapPaperQuestionAsync - UTCID09 - SwapGlobal true -> swap globally successfully")]
        public async Task SwapPaperQuestionAsync_UTCID09_SwapGlobalTrue_ShouldSwapGlobally()
        {
            // Arrange
            var request = MakeRequest(1, 100, 200, true);
            var paper = CreatePaperWithOldQuestion(paperId: 1, examId: 10, oldQuestionId: 100, chapterId: 5, difficulty: 2);
            var newQuestion = CreateQuestion(questionId: 200, chapterId: 5, difficulty: 2, status: "Inprogress");

            _repoMock.Setup(r => r.GetPaperWithQuestionsAsync(request.PaperId!.Value, _ct)).ReturnsAsync(paper);
            _repoMock.Setup(r => r.GetQuestionByIdAsync(request.NewQuestionId!.Value, _ct)).ReturnsAsync(newQuestion);
            _repoMock.Setup(r => r.SwapExamQuestionGloballyAsync(paper.ExamId ?? 0, request.OldQuestionId!.Value, request.NewQuestionId!.Value, _ct))
                     .Returns(Task.CompletedTask);

            // Act
            var result = await _service.SwapPaperQuestionAsync(request, _ct);

            // Assert
            Assert.True(result.IsSuccess);
            _repoMock.Verify(r => r.SwapExamQuestionGloballyAsync(paper.ExamId ?? 0, request.OldQuestionId!.Value, request.NewQuestionId!.Value, _ct), Times.Once);
            _repoMock.Verify(r => r.SwapPaperQuestionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _repoMock.VerifyAll();
        }

        private static Paper CreatePaperWithOldQuestion(int paperId, int examId, int oldQuestionId, int chapterId, int difficulty)
        {
            return new Paper
            {
                PaperId = paperId,
                ExamId = examId,
                Code = 1,
                Exam = new Exam
                {
                    ExamId = examId,
                    TeacherId = 1,
                    SubjectId = 5,
                    Title = "Exam",
                    Duration = 60,
                    ShowScore = 1,
                    ShowAnswer = 1,
                    MaxAttempts = 1,
                    AnswerTimingMode = 0,
                    Status = 0,
                    UpdatedAtUtc = DateTime.UtcNow,
                    ConcurrencyStamp = Array.Empty<byte>()
                },
                Questions = new List<Question>
                {
                    CreateQuestion(oldQuestionId, chapterId, difficulty, "Active"),
                    CreateQuestion(oldQuestionId + 1, chapterId, difficulty, "Active")
                }
            };
        }

        private static Question CreateQuestion(int questionId, int chapterId, int difficulty, string status)
        {
            return new Question
            {
                QuestionId = questionId,
                CreatedByUserId = 1,
                QuestionType = "MCQ",
                QuestionContent = $"Q{questionId}",
                ChapterId = chapterId,
                Difficulty = difficulty,
                UpdatedAtUtc = DateTime.UtcNow,
                Status = status,
                ConcurrencyStamp = Array.Empty<byte>(),
                CreatedByUser = new User { UserId = 1, Email = "teacher@example.com", ConcurrencyStamp = Array.Empty<byte>() },
                Chapter = new Chapter { ChapterId = chapterId, SubjectId = 5, Name = $"Chương {chapterId}" }
            };
        }
    }
}
