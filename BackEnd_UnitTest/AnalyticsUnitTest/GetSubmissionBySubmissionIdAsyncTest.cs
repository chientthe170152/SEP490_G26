using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Common.Errors;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Moq;
using Xunit;

namespace Backend_UnitTest.AnalyticsTests
{
    public class GetSubmissionBySubmissionIdAsync_UTCID_Tests
    {
        private readonly Mock<IAnalyticsRepository> _analyticsRepoMock;
        private readonly Mock<IStudentExamRepository> _studentExamRepoMock;
        private readonly AnalyticsService _service;

        public GetSubmissionBySubmissionIdAsync_UTCID_Tests()
        {
            _analyticsRepoMock = new Mock<IAnalyticsRepository>(MockBehavior.Strict);
            _studentExamRepoMock = new Mock<IStudentExamRepository>(MockBehavior.Strict);
            _service = new AnalyticsService(_analyticsRepoMock.Object, _studentExamRepoMock.Object, TimeProvider.System);
        }

        [Fact(DisplayName = "GetSubmissionBySubmissionIdAsync - UTCID01 - Submission hợp lệ -> trả DTO với showScore=1, showAnswer=2")]
        public async Task GetSubmissionBySubmissionIdAsync_UTCID01_ValidSubmission_ShouldReturnDto()
        {
            int submissionId = 1;
            var exam = BuildExamForTeacherSubmissionView(examId: 1, submissionId: submissionId);

            var submissionLookup = new Submission
            {
                SubmissionId = submissionId,
                StudentId = 1,
                PaperId = 10,
                Paper = new Paper { PaperId = 10, ExamId = 1, Code = 1 }
            };

            _analyticsRepoMock.Setup(r => r.GetSubmissionByIdWithPaperAsync(submissionId))
                .ReturnsAsync(submissionLookup);
            _analyticsRepoMock.Setup(r => r.GetExamWithFullGraphAsync(1))
                .ReturnsAsync(exam);

            var result = await _service.GetSubmissionBySubmissionIdAsync(submissionId);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value.ShowScore);
            Assert.Equal(2, result.Value.ShowAnswer);
            Assert.Equal(submissionId, result.Value.SubmissionId);
            Assert.NotEmpty(result.Value.AnswerReview);
            Assert.NotNull(result.Value.TotalPoints);

            _analyticsRepoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetSubmissionBySubmissionIdAsync - UTCID02 - Submission không tồn tại -> SubmissionNotFound error")]
        public async Task GetSubmissionBySubmissionIdAsync_UTCID02_SubmissionNotFound_ShouldReturnSubmissionNotFoundError()
        {
            int submissionId = 999;

            _analyticsRepoMock.Setup(r => r.GetSubmissionByIdWithPaperAsync(submissionId))
                .ReturnsAsync((Submission?)null);

            var result = await _service.GetSubmissionBySubmissionIdAsync(submissionId);

            Assert.True(result.IsFailure);
            Assert.Equal(AnalyticsErrors.SubmissionNotFound.Code, result.Error.Code);
            _analyticsRepoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetSubmissionBySubmissionIdAsync - UTCID03 - Exam không tồn tại -> ExamNotFound error")]
        public async Task GetSubmissionBySubmissionIdAsync_UTCID03_ExamNotFound_ShouldReturnExamNotFoundError()
        {
            int submissionId = 1;

            var submissionLookup = new Submission
            {
                SubmissionId = submissionId,
                StudentId = 1,
                PaperId = 10,
                Paper = new Paper { PaperId = 10, ExamId = 1, Code = 1 }
            };

            _analyticsRepoMock.Setup(r => r.GetSubmissionByIdWithPaperAsync(submissionId))
                .ReturnsAsync(submissionLookup);
            _analyticsRepoMock.Setup(r => r.GetExamWithFullGraphAsync(1))
                .ReturnsAsync((Exam?)null);

            var result = await _service.GetSubmissionBySubmissionIdAsync(submissionId);

            Assert.True(result.IsFailure);
            Assert.Equal(AnalyticsErrors.ExamNotFound.Code, result.Error.Code);
            _analyticsRepoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetSubmissionBySubmissionIdAsync - UTCID04 - Submission không nằm trong graph exam -> SubmissionNotFound error")]
        public async Task GetSubmissionBySubmissionIdAsync_UTCID04_SubmissionMissingInExamGraph_ShouldReturnSubmissionNotFoundError()
        {
            int submissionId = 1;

            var submissionLookup = new Submission
            {
                SubmissionId = submissionId,
                StudentId = 1,
                PaperId = 10,
                Paper = new Paper { PaperId = 10, ExamId = 1, Code = 1 }
            };

            var exam = new Exam
            {
                ExamId = 1,
                Title = "Exam",
                ShowScore = 0,
                ShowAnswer = 0,
                Duration = 60,
                MaxAttempts = 1,
                AnswerTimingMode = 0,
                Status = 0,
                UpdatedAtUtc = DateTime.UtcNow,
                ConcurrencyStamp = Array.Empty<byte>(),
                Papers = new List<Paper>
                {
                    new Paper
                    {
                        PaperId = 10,
                        ExamId = 1,
                        Code = 1,
                        Questions = new List<Question>(),
                        Submissions = new List<Submission>() // missing target submission
                    }
                }
            };

            _analyticsRepoMock.Setup(r => r.GetSubmissionByIdWithPaperAsync(submissionId))
                .ReturnsAsync(submissionLookup);
            _analyticsRepoMock.Setup(r => r.GetExamWithFullGraphAsync(1))
                .ReturnsAsync(exam);

            var result = await _service.GetSubmissionBySubmissionIdAsync(submissionId);

            Assert.True(result.IsFailure);
            Assert.Equal(AnalyticsErrors.SubmissionNotFound.Code, result.Error.Code);
            _analyticsRepoMock.VerifyAll();
        }

        [Fact(DisplayName = "GetSubmissionBySubmissionIdAsync - UTCID05 - Submission có PaperId không khớp paper trong exam -> review rỗng")]
        public async Task GetSubmissionBySubmissionIdAsync_UTCID05_MissingPaperForSubmission_ShouldReturnEmptyReview()
        {
            int submissionId = 5;

            var submissionLookup = new Submission
            {
                SubmissionId = submissionId,
                StudentId = 1,
                PaperId = 999,
                Paper = new Paper { PaperId = 999, ExamId = 1, Code = 99 }
            };

            var targetSubmission = new Submission
            {
                SubmissionId = submissionId,
                StudentId = 1,
                PaperId = 999,
                CreatedAtUtc = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc),
                UpdatedAtUtc = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc),
                TotalPoints = 5m,
                Status = 2,
                ConcurrencyStamp = Array.Empty<byte>(),
                Student = new User
                {
                    UserId = 1,
                    Email = "s1@x.com",
                    FullName = "Student 1",
                    ConcurrencyStamp = Array.Empty<byte>()
                },
                StudentAnswers = new List<StudentAnswer>()
            };

            var exam = new Exam
            {
                ExamId = 1,
                Title = "Missing Paper Match",
                ShowScore = 0,
                ShowAnswer = 0,
                Duration = 60,
                MaxAttempts = 1,
                AnswerTimingMode = 0,
                Status = 0,
                UpdatedAtUtc = DateTime.UtcNow,
                ConcurrencyStamp = Array.Empty<byte>(),
                Papers = new List<Paper>
                {
                    new Paper
                    {
                        PaperId = 10,
                        ExamId = 1,
                        Code = 1,
                        Questions = new List<Question>(),
                        Submissions = new List<Submission> { targetSubmission }
                    }
                }
            };

            _analyticsRepoMock.Setup(r => r.GetSubmissionByIdWithPaperAsync(submissionId))
                .ReturnsAsync(submissionLookup);
            _analyticsRepoMock.Setup(r => r.GetExamWithFullGraphAsync(1))
                .ReturnsAsync(exam);

            var result = await _service.GetSubmissionBySubmissionIdAsync(submissionId);

            Assert.True(result.IsSuccess);
            Assert.Equal(submissionId, result.Value.SubmissionId);
            Assert.Equal(0, result.Value.TotalQuestions);
            Assert.Empty(result.Value.AnswerReview);
            Assert.Equal(0, result.Value.CorrectCount);
            Assert.Equal(0, result.Value.WrongCount);

            _analyticsRepoMock.VerifyAll();
        }

        private static Exam BuildExamForTeacherSubmissionView(int examId, int submissionId)
        {
            var chapter = new Chapter { ChapterId = 10, SubjectId = 1, Name = "Chương 1" };
            var q = CreateQuestion(101, "Q1", chapter, 1);

            var student = new User
            {
                UserId = 1,
                Email = "s1@x.com",
                FullName = "Student 1",
                ConcurrencyStamp = Array.Empty<byte>()
            };

            var submission = CreateSubmission(
                submissionId: submissionId,
                studentId: 1,
                paperId: 10,
                updatedAtUtc: new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc),
                totalPoints: 9m,
                student: student,
                answers: new List<StudentAnswer>
                {
                    CreateStudentAnswer(1, q.QuestionAnswers.First(), "A")
                });

            return new Exam
            {
                ExamId = examId,
                Title = "Teacher View Exam",
                ShowScore = 0,
                ShowAnswer = 0,
                Duration = 60,
                MaxAttempts = 1,
                AnswerTimingMode = 0,
                Status = 0,
                UpdatedAtUtc = DateTime.UtcNow,
                ConcurrencyStamp = Array.Empty<byte>(),
                Papers = new List<Paper>
                {
                    new Paper
                    {
                        PaperId = 10,
                        ExamId = examId,
                        Code = 1,
                        Questions = new List<Question> { q },
                        Submissions = new List<Submission> { submission }
                    }
                }
            };
        }

        private static Question CreateQuestion(int questionId, string content, Chapter chapter, int difficulty)
        {
            var q = new Question
            {
                QuestionId = questionId,
                CreatedByUserId = 99,
                QuestionType = "MCQ",
                QuestionContent = content,
                ChapterId = chapter.ChapterId,
                Difficulty = difficulty,
                UpdatedAtUtc = DateTime.UtcNow,
                Status = "Active",
                ConcurrencyStamp = Array.Empty<byte>(),
                Chapter = chapter,
                CreatedByUser = new User
                {
                    UserId = 99,
                    Email = "teacher@x.com",
                    FullName = "Teacher",
                    ConcurrencyStamp = Array.Empty<byte>()
                }
            };

            var qa = new QuestionAnswer
            {
                QuestionAnswerId = questionId * 10,
                QuestionId = questionId,
                Content = "Option A",
                CorrectAnswer = "A",
                IsCorrect = true,
                ConcurrencyStamp = Array.Empty<byte>(),
                Question = q
            };

            q.QuestionAnswers = new List<QuestionAnswer> { qa };
            return q;
        }

        private static Submission CreateSubmission(
            int submissionId,
            int studentId,
            int paperId,
            DateTime updatedAtUtc,
            decimal? totalPoints,
            User student,
            List<StudentAnswer> answers)
        {
            var submission = new Submission
            {
                SubmissionId = submissionId,
                StudentId = studentId,
                PaperId = paperId,
                CreatedAtUtc = updatedAtUtc.AddMinutes(-20),
                UpdatedAtUtc = updatedAtUtc,
                TotalPoints = totalPoints,
                Status = 2,
                ConcurrencyStamp = Array.Empty<byte>(),
                Student = student,
                StudentAnswers = answers
            };

            foreach (var answer in answers)
            {
                answer.Submission = submission;
            }

            return submission;
        }

        private static StudentAnswer CreateStudentAnswer(int studentAnswerId, QuestionAnswer qa, string response)
        {
            return new StudentAnswer
            {
                StudentAnswerId = studentAnswerId,
                SubmissionId = 0,
                QuestionAnswerId = qa.QuestionAnswerId,
                Response = response,
                ConcurrencyStamp = Array.Empty<byte>(),
                QuestionAnswer = qa
            };
        }
    }
}