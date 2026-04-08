using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.SubmisssionsUnitTest
{
    public class SubmitExamAsyncTest
    {
        // ────────────────────────────────────────────────────────────────
        //  Helpers
        // ────────────────────────────────────────────────────────────────

        private static SubmissionService CreateService(Mock<ISubmissionRepository> repoMock)
            => new SubmissionService(repoMock.Object);

        /// <summary>
        /// Tạo một Submission đang InProgress với exam duration và closeAt tuỳ chỉnh.
        /// startTime mặc định là 10 phút trước UtcNow.
        /// </summary>
        private static Submission BuildInProgressSubmission(
            int submissionId = 1,
            int duration = 60,
            DateTime? closeAt = null,
            DateTime? startTime = null,
            List<StudentAnswer>? existingAnswers = null,
            int paperId = 10)
        {
            var start = startTime ?? DateTime.UtcNow.AddMinutes(-10);

            return new Submission
            {
                SubmissionId = submissionId,
                PaperId = paperId,
                Status = SubmissionStatus.InProgress,
                CreatedAtUtc = start,
                StudentAnswers = existingAnswers ?? new List<StudentAnswer>(),
                Paper = new Paper
                {
                    PaperId = paperId,
                    Exam = new Exam
                    {
                        Duration = duration,
                        CloseAt = closeAt
                    }
                }
            };
        }

        /// <summary>Tạo request nộp bài đơn giản.</summary>
        private static SubmitExamRequest BuildRequest(
            int examId = 1,
            bool submit = true,
            List<StudentAnswerDto>? answers = null)
        {
            return new SubmitExamRequest
            {
                ExamId = examId,
                Submit = submit,
                StudentAnswers = answers ?? new List<StudentAnswerDto>()
            };
        }

        // ────────────────────────────────────────────────────────────────
        //  TC01 – submission == null  →  KeyNotFoundException
        // ────────────────────────────────────────────────────────────────
        [Fact]
        public async Task TC01_SubmissionNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var repoMock = new Mock<ISubmissionRepository>();
            repoMock
                .Setup(r => r.GetActiveSubmissionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Submission?)null);

            var svc = CreateService(repoMock);
            var request = BuildRequest();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => svc.SubmitExamAsync(studentId: 1, request));

            Assert.Equal(ErrorMessages.SubmissionNotFound, ex.Message);
        }

        // ────────────────────────────────────────────────────────────────
        //  TC02 – submission.Status != InProgress  →  InvalidOperationException
        // ────────────────────────────────────────────────────────────────
        [Fact]
        public async Task TC02_SubmissionAlreadySubmitted_ThrowsInvalidOperationException()
        {
            // Arrange
            var submission = BuildInProgressSubmission();
            submission.Status = SubmissionStatus.Submitted; // bị submit rồi

            var repoMock = new Mock<ISubmissionRepository>();
            repoMock
                .Setup(r => r.GetActiveSubmissionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(submission);

            var svc = CreateService(repoMock);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => svc.SubmitExamAsync(1, BuildRequest()));

            Assert.Equal(ErrorMessages.SubmissionAlreadySubmitted, ex.Message);
        }

        // ────────────────────────────────────────────────────────────────
        //  TC03 – nộp trễ vì vượt deadline theo duration
        // ────────────────────────────────────────────────────────────────
        [Fact]
        public async Task TC03_LateByDuration_ThrowsInvalidOperationException()
        {
            // Arrange: start = 70 phút trước, duration = 60 → deadline đã qua 10 phút
            var submission = BuildInProgressSubmission(
                duration: 60,
                startTime: DateTime.UtcNow.AddMinutes(-70));

            var repoMock = new Mock<ISubmissionRepository>();
            repoMock
                .Setup(r => r.GetActiveSubmissionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(submission);

            var svc = CreateService(repoMock);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => svc.SubmitExamAsync(1, BuildRequest()));

            Assert.Equal(ErrorMessages.SubmissionLateNotAllowed, ex.Message);
        }

        // ────────────────────────────────────────────────────────────────
        //  TC04 – nộp trễ vì vượt CloseAt của exam (dù còn duration)
        // ────────────────────────────────────────────────────────────────
        [Fact]
        public async Task TC04_LateByCloseAt_ThrowsInvalidOperationException()
        {
            // Arrange: duration = 120 phút (còn dư), nhưng CloseAt đã qua
            var submission = BuildInProgressSubmission(
                duration: 120,
                closeAt: DateTime.UtcNow.AddMinutes(-1),
                startTime: DateTime.UtcNow.AddMinutes(-10));

            var repoMock = new Mock<ISubmissionRepository>();
            repoMock
                .Setup(r => r.GetActiveSubmissionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(submission);

            var svc = CreateService(repoMock);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => svc.SubmitExamAsync(1, BuildRequest()));

            Assert.Equal(ErrorMessages.SubmissionLateNotAllowed, ex.Message);
        }

        // ────────────────────────────────────────────────────────────────
        //  TC05 – QuestionAnswerId không thuộc Paper  →  ArgumentException
        // ────────────────────────────────────────────────────────────────
        [Fact]
        public async Task TC05_InvalidQuestionAnswerId_ThrowsArgumentException()
        {
            // Arrange
            var submission = BuildInProgressSubmission(duration: 60);
            var repoMock = new Mock<ISubmissionRepository>();

            repoMock
                .Setup(r => r.GetActiveSubmissionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(submission);

            // validIds chứa id=100, nhưng request gửi id=999
            repoMock
                .Setup(r => r.GetValidQuestionAnswerIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HashSet<int> { 100 });

            var request = BuildRequest(answers: new List<StudentAnswerDto>
            {
                new StudentAnswerDto { QuestionAnswerId = 999, Response = "x" }
            });

            var svc = CreateService(repoMock);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => svc.SubmitExamAsync(1, request));

            Assert.Equal(ErrorMessages.InvalidQuestionAnswer, ex.Message);
        }

        // ────────────────────────────────────────────────────────────────
        //  TC06 – Happy path: Submit=true, toAdd>0, toRemove>0, isLate=false
        //         Kiểm tra đồng thời: Add, Remove, Status=2, response đúng
        // ────────────────────────────────────────────────────────────────
        [Fact]
        public async Task TC06_FullSubmit_AddAndRemove_ReturnsCorrectResponse()
        {
            // Arrange
            // Existing: answer id=1 (sẽ bị remove), id=2 (sẽ được update)
            var existingAnswers = new List<StudentAnswer>
            {
                new StudentAnswer { QuestionAnswerId = 1, Response = "old1", SubmissionId = 1 },
                new StudentAnswer { QuestionAnswerId = 2, Response = "old2", SubmissionId = 1 }
            };

            var submission = BuildInProgressSubmission(
                duration: 60,
                existingAnswers: existingAnswers);

            var repoMock = new Mock<ISubmissionRepository>();

            repoMock
                .Setup(r => r.GetActiveSubmissionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(submission);

            repoMock
                .Setup(r => r.GetValidQuestionAnswerIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HashSet<int> { 2, 3 }); // id=1 không còn valid

            repoMock.Setup(r => r.RemoveStudentAnswers(It.IsAny<List<StudentAnswer>>()));
            repoMock.Setup(r => r.AddStudentAnswers(It.IsAny<List<StudentAnswer>>()));
            repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Request: gửi id=2 (update), id=3 (add mới); bỏ id=1 (remove)
            var request = BuildRequest(submit: true, answers: new List<StudentAnswerDto>
            {
                new StudentAnswerDto { QuestionAnswerId = 2, Response = "new2" },
                new StudentAnswerDto { QuestionAnswerId = 3, Response = "new3" }
            });

            var svc = CreateService(repoMock);

            // Act
            var result = await svc.SubmitExamAsync(1, request);

            // Assert – Status nộp hẳn → 2
            Assert.Equal(2, submission.Status);
            Assert.Equal(1, result.SubmissionId);
            Assert.False(result.IsLate);

            // id=2 phải được update Response
            Assert.Equal("new2", existingAnswers.First(a => a.QuestionAnswerId == 2).Response);

            // RemoveStudentAnswers được gọi với id=1
            repoMock.Verify(r => r.RemoveStudentAnswers(
                It.Is<List<StudentAnswer>>(l => l.Any(a => a.QuestionAnswerId == 1))), Times.Once);

            // AddStudentAnswers được gọi với id=3
            repoMock.Verify(r => r.AddStudentAnswers(
                It.Is<List<StudentAnswer>>(l => l.Any(a => a.QuestionAnswerId == 3))), Times.Once);

            repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ────────────────────────────────────────────────────────────────
        //  TC07 – Submit=false (lưu nháp) → Status = 1
        // ────────────────────────────────────────────────────────────────
        [Fact]
        public async Task TC07_SaveDraft_StatusSetTo1()
        {
            // Arrange
            var submission = BuildInProgressSubmission(duration: 60);
            var repoMock = new Mock<ISubmissionRepository>();

            repoMock
                .Setup(r => r.GetActiveSubmissionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(submission);

            repoMock
                .Setup(r => r.GetValidQuestionAnswerIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HashSet<int>());

            repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var request = BuildRequest(submit: false); // lưu nháp

            var svc = CreateService(repoMock);

            // Act
            var result = await svc.SubmitExamAsync(1, request);

            // Assert
            Assert.Equal(1, submission.Status);
        }

        // ────────────────────────────────────────────────────────────────
        //  TC08 – toRemove = 0, toAdd = 0 (chỉ update existing FillInBlank)
        //         Đảm bảo Remove và Add KHÔNG được gọi
        // ────────────────────────────────────────────────────────────────
        [Fact]
        public async Task TC08_OnlyUpdate_NoAddNoRemove()
        {
            // Arrange
            var existingAnswers = new List<StudentAnswer>
            {
                new StudentAnswer { QuestionAnswerId = 5, Response = "old", SubmissionId = 1 }
            };

            var submission = BuildInProgressSubmission(duration: 60, existingAnswers: existingAnswers);
            var repoMock = new Mock<ISubmissionRepository>();

            repoMock
                .Setup(r => r.GetActiveSubmissionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(submission);

            repoMock
                .Setup(r => r.GetValidQuestionAnswerIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HashSet<int> { 5 });

            repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var request = BuildRequest(submit: true, answers: new List<StudentAnswerDto>
            {
                new StudentAnswerDto { QuestionAnswerId = 5, Response = "updated" }
            });

            var svc = CreateService(repoMock);

            // Act
            await svc.SubmitExamAsync(1, request);

            // Assert – Response được cập nhật
            Assert.Equal("updated", existingAnswers.First().Response);

            // Remove và Add không được gọi
            repoMock.Verify(r => r.RemoveStudentAnswers(It.IsAny<List<StudentAnswer>>()), Times.Never);
            repoMock.Verify(r => r.AddStudentAnswers(It.IsAny<List<StudentAnswer>>()), Times.Never);
        }

        // ────────────────────────────────────────────────────────────────
        //  TC09 – StudentAnswers rỗng: không có add/remove/update nào
        // ────────────────────────────────────────────────────────────────
        [Fact]
        public async Task TC09_EmptyStudentAnswers_NothingAddedOrRemoved()
        {
            // Arrange
            var submission = BuildInProgressSubmission(duration: 60);
            var repoMock = new Mock<ISubmissionRepository>();

            repoMock
                .Setup(r => r.GetActiveSubmissionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(submission);

            repoMock
                .Setup(r => r.GetValidQuestionAnswerIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HashSet<int>());

            repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var request = BuildRequest(submit: true, answers: new List<StudentAnswerDto>());

            var svc = CreateService(repoMock);

            // Act
            var result = await svc.SubmitExamAsync(1, request);

            // Assert
            Assert.Equal(1, result.SubmissionId);
            repoMock.Verify(r => r.RemoveStudentAnswers(It.IsAny<List<StudentAnswer>>()), Times.Never);
            repoMock.Verify(r => r.AddStudentAnswers(It.IsAny<List<StudentAnswer>>()), Times.Never);
        }

        // ────────────────────────────────────────────────────────────────
        //  TC10 – CloseAt = null (exam không có thời hạn đóng),
        //         còn trong duration  →  nộp thành công, isLate = false
        // ────────────────────────────────────────────────────────────────
        [Fact]
        public async Task TC10_NoCloseAt_WithinDuration_Success()
        {
            // Arrange
            var submission = BuildInProgressSubmission(
                duration: 60,
                closeAt: null,             // không có CloseAt
                startTime: DateTime.UtcNow.AddMinutes(-10));

            var repoMock = new Mock<ISubmissionRepository>();

            repoMock
                .Setup(r => r.GetActiveSubmissionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(submission);

            repoMock
                .Setup(r => r.GetValidQuestionAnswerIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HashSet<int>());

            repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var svc = CreateService(repoMock);

            // Act
            var result = await svc.SubmitExamAsync(1, BuildRequest());

            // Assert
            Assert.False(result.IsLate);
        }

        // ────────────────────────────────────────────────────────────────
        //  TC11 – Nhiều answers không hợp lệ: dừng ở invalid đầu tiên
        // ────────────────────────────────────────────────────────────────
        [Fact]
        public async Task TC11_MultipleAnswers_FirstInvalidThrows()
        {
            // Arrange
            var submission = BuildInProgressSubmission(duration: 60);
            var repoMock = new Mock<ISubmissionRepository>();

            repoMock
                .Setup(r => r.GetActiveSubmissionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(submission);

            // Chỉ id=100 hợp lệ
            repoMock
                .Setup(r => r.GetValidQuestionAnswerIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HashSet<int> { 100 });

            var request = BuildRequest(answers: new List<StudentAnswerDto>
            {
                new StudentAnswerDto { QuestionAnswerId = 100, Response = "ok" },
                new StudentAnswerDto { QuestionAnswerId = 200, Response = "bad" }, // invalid
                new StudentAnswerDto { QuestionAnswerId = 300, Response = "bad2" }
            });

            var svc = CreateService(repoMock);

            // Act & Assert – dừng ngay tại id=200
            await Assert.ThrowsAsync<ArgumentException>(
                () => svc.SubmitExamAsync(1, request));
        }

        // ────────────────────────────────────────────────────────────────
        //  TC12 – UpdatedAtUtc được gán đúng (≈ UtcNow)
        // ────────────────────────────────────────────────────────────────
        [Fact]
        public async Task TC12_UpdatedAtUtc_SetToNow()
        {
            // Arrange
            var submission = BuildInProgressSubmission(duration: 60);
            var repoMock = new Mock<ISubmissionRepository>();

            repoMock
                .Setup(r => r.GetActiveSubmissionAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(submission);

            repoMock
                .Setup(r => r.GetValidQuestionAnswerIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HashSet<int>());

            repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var before = DateTime.UtcNow;
            var svc = CreateService(repoMock);

            // Act
            var result = await svc.SubmitExamAsync(1, BuildRequest());
            var after = DateTime.UtcNow;

            // Assert – UpdatedAtUtc nằm trong khoảng [before, after]
            Assert.InRange(submission.UpdatedAtUtc, before, after);
            // SubmittedAt trong response cũng hợp lệ
            Assert.InRange(result.SubmittedAtUtc, before, after);
        }
    }
}