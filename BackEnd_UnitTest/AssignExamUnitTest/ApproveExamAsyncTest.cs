using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.AssignExamUnitTest
{
    public class ApproveExamAsync_UTCID_Tests
    {
        private readonly Mock<IAssignExamRepository> _repoMock;
        private readonly AssignExamService _service;
        private readonly CancellationToken _ct = CancellationToken.None;

        public ApproveExamAsync_UTCID_Tests()
        {
            _repoMock = new Mock<IAssignExamRepository>(MockBehavior.Strict);
            _service = new AssignExamService(_repoMock.Object);
        }

        [Fact(DisplayName = "ApproveExamAsync - UTCID01 - Valid examId -> update status to Published")]
        public async Task ApproveExamAsync_UTCID01_ValidExamId_ShouldUpdateStatusToPublished()
        {
            // Arrange
            int examId = 1;
            var exam = CreateExam(examId);
            var questionIds = new List<int> { 101, 102, 103 };

            _repoMock.Setup(r => r.GetExamByIdAsync(examId, _ct))
                     .ReturnsAsync(exam);
            _repoMock.Setup(r => r.UpdateExamStatusAsync(examId, ExamStatus.Published, _ct))
                     .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetAllQuestionIdsInExamAsync(examId, _ct))
                     .ReturnsAsync(questionIds);
            _repoMock.Setup(r => r.UpdateQuestionsToInprogressAsync(questionIds, _ct))
                     .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.UpdateBlueprintToInprogressAsync(examId, _ct))
                     .Returns(Task.CompletedTask);

            // Act
            await _service.ApproveExamAsync(examId, _ct);

            // Assert
            _repoMock.Verify(r => r.GetExamByIdAsync(examId, _ct), Times.Once);
            _repoMock.Verify(r => r.UpdateExamStatusAsync(examId, ExamStatus.Published, _ct), Times.Once);
            _repoMock.Verify(r => r.GetAllQuestionIdsInExamAsync(examId, _ct), Times.Once);
            _repoMock.Verify(r => r.UpdateQuestionsToInprogressAsync(questionIds, _ct), Times.Once);
            _repoMock.Verify(r => r.UpdateBlueprintToInprogressAsync(examId, _ct), Times.Once);
            _repoMock.VerifyAll();
        }

        [Fact(DisplayName = "ApproveExamAsync - UTCID02 - Exam not found -> propagate KeyNotFoundException")]
        public async Task ApproveExamAsync_UTCID02_ExamNotFound_ShouldPropagateKeyNotFoundException()
        {
            // Arrange
            int examId = 999;

            _repoMock.Setup(r => r.GetExamByIdAsync(examId, _ct))
                     .ReturnsAsync((Exam?)null);

            // Act
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.ApproveExamAsync(examId, _ct));

            // Assert
            Assert.Equal("Exam not found.", ex.Message);
            _repoMock.Verify(r => r.GetExamByIdAsync(examId, _ct), Times.Once);
            _repoMock.Verify(r => r.UpdateExamStatusAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _repoMock.VerifyAll();
        }

        [Fact(DisplayName = "ApproveExamAsync - UTCID03 - ExamId = 0 and repository returns exam -> continue flow")]
        public async Task ApproveExamAsync_UTCID03_ExamIdZero_WhenExamExists_ShouldContinueFlow()
        {
            // Arrange
            int examId = 0;
            var exam = CreateExam(examId);
            var questionIds = new List<int>();

            _repoMock.Setup(r => r.GetExamByIdAsync(examId, _ct))
                     .ReturnsAsync(exam);
            _repoMock.Setup(r => r.UpdateExamStatusAsync(examId, ExamStatus.Published, _ct))
                     .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.GetAllQuestionIdsInExamAsync(examId, _ct))
                     .ReturnsAsync(questionIds);
            _repoMock.Setup(r => r.UpdateQuestionsToInprogressAsync(questionIds, _ct))
                     .Returns(Task.CompletedTask);
            _repoMock.Setup(r => r.UpdateBlueprintToInprogressAsync(examId, _ct))
                     .Returns(Task.CompletedTask);

            // Act
            await _service.ApproveExamAsync(examId, _ct);

            // Assert
            _repoMock.Verify(r => r.GetExamByIdAsync(examId, _ct), Times.Once);
            _repoMock.Verify(r => r.UpdateExamStatusAsync(examId, ExamStatus.Published, _ct), Times.Once);
            _repoMock.Verify(r => r.GetAllQuestionIdsInExamAsync(examId, _ct), Times.Once);
            _repoMock.Verify(r => r.UpdateQuestionsToInprogressAsync(questionIds, _ct), Times.Once);
            _repoMock.Verify(r => r.UpdateBlueprintToInprogressAsync(examId, _ct), Times.Once);
            _repoMock.VerifyAll();
        }

        [Fact(DisplayName = "ApproveExamAsync - UTCID04 - Repository throws exception -> propagate exception")]
        public async Task ApproveExamAsync_UTCID04_RepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            int examId = 2;
            var exam = CreateExam(examId);

            _repoMock.Setup(r => r.GetExamByIdAsync(examId, _ct))
                     .ReturnsAsync(exam);
            _repoMock.Setup(r => r.UpdateExamStatusAsync(examId, ExamStatus.Published, _ct))
                     .ThrowsAsync(new Exception("Database error"));

            // Act
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _service.ApproveExamAsync(examId, _ct));

            // Assert
            Assert.Equal("Database error", ex.Message);
            _repoMock.Verify(r => r.GetExamByIdAsync(examId, _ct), Times.Once);
            _repoMock.Verify(r => r.UpdateExamStatusAsync(examId, ExamStatus.Published, _ct), Times.Once);
            _repoMock.VerifyAll();
        }

        private static Exam CreateExam(int examId)
        {
            return new Exam
            {
                ExamId = examId,
                TeacherId = 1,
                SubjectId = 5,
                Title = $"Exam {examId}",
                Duration = 60,
                ShowScore = 1,
                ShowAnswer = 1,
                MaxAttempts = 1,
                AnswerTimingMode = 0,
                Status = 0,
                OpenAt = null,
                CloseAt = null,
                UpdatedAtUtc = DateTime.UtcNow,
                ConcurrencyStamp = Array.Empty<byte>()
            };
        }
    }
}
