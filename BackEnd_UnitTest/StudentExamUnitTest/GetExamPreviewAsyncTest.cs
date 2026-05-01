using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Common;
using Backend.Common.Errors;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Moq;
using Xunit;

namespace Backend_UnitTest.StudentExamTests;

public class GetExamPreviewAsync_UTCID_Tests
{
    private readonly Mock<IStudentExamRepository> _repoMock;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly StudentExamService _service;

    public GetExamPreviewAsync_UTCID_Tests()
    {
        _repoMock = new Mock<IStudentExamRepository>(MockBehavior.Strict);
        _currentUserMock = new Mock<ICurrentUserService>();
        _service = new StudentExamService(_repoMock.Object, _currentUserMock.Object, TimeProvider.System);
    }

    [Fact(DisplayName = "GetExamPreviewAsync - UTCID01 - Student authorized, status=1 -> public")]
    public async Task GetExamPreviewAsync_UTCID01_StatusPublic_ShouldReturnPreview()
    {
        const int userId = 1001;
        const int examId = 1;

        _currentUserMock.Setup(u => u.UserId).Returns(userId);
        _currentUserMock.Setup(u => u.Role).Returns(RoleIds.Student);
        _repoMock.Setup(r => r.CanStudentTakeExamAsync(userId, examId)).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetExamPreviewAsync(examId)).ReturnsAsync(BuildExamPreviewData(examId, 1));

        var result = await _service.GetExamPreviewAsync(examId);

        Assert.True(result.IsSuccess);
        Assert.Equal("public", result.Value.Status);
        Assert.Equal(2, result.Value.BlueprintMatrix.Count);
        Assert.Equal(3, result.Value.BlueprintMatrix.Single(x => x.ChapterName == "Chương 1").Total);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "GetExamPreviewAsync - UTCID02 - Student authorized, status=2 -> private")]
    public async Task GetExamPreviewAsync_UTCID02_StatusPrivate_ShouldReturnPreview()
    {
        const int userId = 1001;
        const int examId = 1;

        _currentUserMock.Setup(u => u.UserId).Returns(userId);
        _currentUserMock.Setup(u => u.Role).Returns(RoleIds.Student);
        _repoMock.Setup(r => r.CanStudentTakeExamAsync(userId, examId)).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetExamPreviewAsync(examId)).ReturnsAsync(BuildExamPreviewData(examId, 2));

        var result = await _service.GetExamPreviewAsync(examId);

        Assert.True(result.IsSuccess);
        Assert.Equal("private", result.Value.Status);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "GetExamPreviewAsync - UTCID03 - Student authorized, status=3 -> closed")]
    public async Task GetExamPreviewAsync_UTCID03_StatusClosed_ShouldReturnPreview()
    {
        const int userId = 1001;
        const int examId = 1;

        _currentUserMock.Setup(u => u.UserId).Returns(userId);
        _currentUserMock.Setup(u => u.Role).Returns(RoleIds.Student);
        _repoMock.Setup(r => r.CanStudentTakeExamAsync(userId, examId)).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetExamPreviewAsync(examId)).ReturnsAsync(BuildExamPreviewData(examId, 3));

        var result = await _service.GetExamPreviewAsync(examId);

        Assert.True(result.IsSuccess);
        Assert.Equal("closed", result.Value.Status);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "GetExamPreviewAsync - UTCID04 - Student authorized, status khác -> unknown")]
    public async Task GetExamPreviewAsync_UTCID04_StatusUnknown_ShouldReturnPreview()
    {
        const int userId = 1001;
        const int examId = 1;

        _currentUserMock.Setup(u => u.UserId).Returns(userId);
        _currentUserMock.Setup(u => u.Role).Returns(RoleIds.Student);
        _repoMock.Setup(r => r.CanStudentTakeExamAsync(userId, examId)).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetExamPreviewAsync(examId)).ReturnsAsync(BuildExamPreviewData(examId, 99));

        var result = await _service.GetExamPreviewAsync(examId);

        Assert.True(result.IsSuccess);
        Assert.Equal("unknown", result.Value.Status);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "GetExamPreviewAsync - UTCID05 - Teacher bypass permission check")]
    public async Task GetExamPreviewAsync_UTCID05_Teacher_ShouldBypassPermission()
    {
        const int examId = 1;

        _currentUserMock.Setup(u => u.Role).Returns(RoleIds.Teacher);
        _repoMock.Setup(r => r.GetExamPreviewAsync(examId)).ReturnsAsync(BuildExamPreviewData(examId, 1));

        var result = await _service.GetExamPreviewAsync(examId);

        Assert.True(result.IsSuccess);
        Assert.Equal("public", result.Value.Status);
        _repoMock.Verify(r => r.CanStudentTakeExamAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "GetExamPreviewAsync - UTCID06 - Student không được phép -> NotAllowed error")]
    public async Task GetExamPreviewAsync_UTCID06_UnauthorizedStudent_ShouldReturnNotAllowedError()
    {
        const int userId = 1001;
        const int examId = 1;

        _currentUserMock.Setup(u => u.UserId).Returns(userId);
        _currentUserMock.Setup(u => u.Role).Returns(RoleIds.Student);
        _repoMock.Setup(r => r.CanStudentTakeExamAsync(userId, examId)).ReturnsAsync(false);

        var result = await _service.GetExamPreviewAsync(examId);

        Assert.True(result.IsFailure);
        Assert.Equal(StudentExamErrors.NotAllowed.Code, result.Error.Code);
        _repoMock.Verify(r => r.GetExamPreviewAsync(It.IsAny<int>()), Times.Never);
        _repoMock.VerifyAll();
    }

    [Fact(DisplayName = "GetExamPreviewAsync - UTCID07 - Repo trả null -> NotFound error")]
    public async Task GetExamPreviewAsync_UTCID07_NullPreview_ShouldReturnNotFoundError()
    {
        const int examId = 1;

        _currentUserMock.Setup(u => u.Role).Returns(RoleIds.Teacher);
        _repoMock.Setup(r => r.GetExamPreviewAsync(examId)).ReturnsAsync((ExamPreviewData?)null);

        var result = await _service.GetExamPreviewAsync(examId);

        Assert.True(result.IsFailure);
        Assert.Equal(StudentExamErrors.NotFound.Code, result.Error.Code);
        _repoMock.VerifyAll();
    }

    private static ExamPreviewData BuildExamPreviewData(int examId, int status)
        => new()
        {
            ExamId = examId,
            Title = "Exam Preview",
            Description = "Description",
            Duration = 60,
            Status = status,
            OpenAt = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc),
            CloseAt = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2026, 4, 1, 7, 0, 0, DateTimeKind.Utc),
            SubjectCode = "MAE",
            SubjectName = "Math",
            TeacherName = "Teacher A",
            TotalQuestions = 10,
            MaxAttempts = 3,
            PaperCount = 2,
            ShowScore = 1,
            ShowAnswer = 1,
            AnswerTimingMode = 0,
            BlueprintChapters = new List<BlueprintChapterRaw>
            {
                new() { ChapterName = "Chương 1", Difficulty = 1, TotalOfQuestions = 2 },
                new() { ChapterName = "Chương 1", Difficulty = 2, TotalOfQuestions = 1 },
                new() { ChapterName = "Chương 2", Difficulty = 3, TotalOfQuestions = 3 },
                new() { ChapterName = "Chương 2", Difficulty = 4, TotalOfQuestions = 4 }
            }
        };
}
