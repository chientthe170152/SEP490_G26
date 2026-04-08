using Backend.Constants;
using Backend.DTOs.Question;
using Backend.Exceptions;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.QuestionUnitTest;

public class UpdateQuestionAsyncTest
{
    private readonly Mock<IQuestionRepository> _repoMock = new();
    private readonly Mock<ILogger<QuestionService>> _loggerMock = new();

    private QuestionService CreateService() => new(_repoMock.Object, _loggerMock.Object);

    private void SetupValidation(bool chapterExists = true)
    {
        _repoMock.Setup(r => r.ChapterExistsAsync(It.IsAny<int>())).ReturnsAsync(chapterExists);
    }

    private void SetupSave()
    {
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
    }

    private void SetupCreate(Question? returnEntity = null)
    {
        _repoMock
            .Setup(r => r.CreateQuestionsAsync(It.IsAny<List<Question>>()))
            .ReturnsAsync((List<Question> l) => returnEntity != null ? new List<Question> { returnEntity } : l);
    }

    // ── TC01: Câu hỏi không tồn tại → KeyNotFoundException ──────────────
    [Fact]
    public async Task TC01_QuestionNotFound_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(It.IsAny<int>()))
                 .ReturnsAsync((Question?)null);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => CreateService().UpdateQuestionAsync(1, 1, TestHelpers.ValidMcqDto()));

        Assert.Contains("Không tìm thấy câu hỏi", ex.Message);
    }

    // ── TC02: Câu hỏi tồn tại nhưng userId khác → KeyNotFoundException ──
    [Fact]
    public async Task TC02_WrongOwner_ThrowsKeyNotFoundException()
    {
        var entity = TestHelpers.ValidQuestionEntity(createdBy: 99);
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(It.IsAny<int>())).ReturnsAsync(entity);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => CreateService().UpdateQuestionAsync(1, userId: 1, TestHelpers.ValidMcqDto()));
    }

    // ── TC03: Status = Archive → QuestionValidationException ─────────────
    [Fact]
    public async Task TC03_ArchiveStatus_ThrowsValidationException()
    {
        var entity = TestHelpers.ValidQuestionEntity(status: QuestionStatus.Archive);
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(It.IsAny<int>())).ReturnsAsync(entity);

        var ex = await Assert.ThrowsAsync<QuestionValidationException>(
            () => CreateService().UpdateQuestionAsync(1, 1, TestHelpers.ValidMcqDto()));

        Assert.Contains(ex.Errors, e => e.Contains("Không thể sửa câu hỏi đã lưu trữ"));
    }

    // ── TC04: Request không hợp lệ → QuestionValidationException ─────────
    [Fact]
    public async Task TC04_InvalidRequest_ThrowsValidationException()
    {
        var entity = TestHelpers.ValidQuestionEntity();
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(It.IsAny<int>())).ReturnsAsync(entity);
        _repoMock.Setup(r => r.IsQuestionUsedAsync(It.IsAny<int>())).ReturnsAsync(false);
        SetupValidation(chapterExists: true);

        var dto = TestHelpers.ValidMcqDto();
        dto.Stem = ""; // invalid

        await Assert.ThrowsAsync<QuestionValidationException>(
            () => CreateService().UpdateQuestionAsync(1, 1, dto));
    }

    // ── TC05: Status = Inprogress → clone, existing → Archive ────────────
    [Fact]
    public async Task TC05_StatusInprogress_ClonesQuestion()
    {
        var entity = TestHelpers.ValidQuestionEntity(status: QuestionStatus.Inprogress);
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(It.IsAny<int>())).ReturnsAsync(entity);
        _repoMock.Setup(r => r.IsQuestionUsedAsync(It.IsAny<int>())).ReturnsAsync(false);
        SetupValidation();
        SetupCreate();
        SetupSave();

        var dto = TestHelpers.ValidMcqDto();
        await CreateService().UpdateQuestionAsync(1, 1, dto);

        Assert.Equal(QuestionStatus.Archive, entity.Status);
        _repoMock.Verify(r => r.CreateQuestionsAsync(It.IsAny<List<Question>>()), Times.Once);
    }

    // ── TC06: isUsed = true → clone, existing → Archive ──────────────────
    [Fact]
    public async Task TC06_IsUsed_ClonesQuestion()
    {
        var entity = TestHelpers.ValidQuestionEntity(status: QuestionStatus.Active);
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(It.IsAny<int>())).ReturnsAsync(entity);
        _repoMock.Setup(r => r.IsQuestionUsedAsync(It.IsAny<int>())).ReturnsAsync(true);
        SetupValidation();
        SetupCreate();
        SetupSave();

        await CreateService().UpdateQuestionAsync(1, 1, TestHelpers.ValidMcqDto());

        Assert.Equal(QuestionStatus.Archive, entity.Status);
        _repoMock.Verify(r => r.CreateQuestionsAsync(It.IsAny<List<Question>>()), Times.Once);
    }

    // ── TC07: Inprogress + request.Status = Inprogress → ép thành Active ─
    [Fact]
    public async Task TC07_CloneWithInprogressRequestStatus_ForcedToActive()
    {
        var entity = TestHelpers.ValidQuestionEntity(status: QuestionStatus.Inprogress);
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(It.IsAny<int>())).ReturnsAsync(entity);
        _repoMock.Setup(r => r.IsQuestionUsedAsync(It.IsAny<int>())).ReturnsAsync(false);
        SetupValidation();

        // Capture dto sau khi bị mutate
        QuestionDto? capturedDto = null;
        _repoMock
            .Setup(r => r.CreateQuestionsAsync(It.IsAny<List<Question>>()))
            .Callback<List<Question>>(l => { /* entity đã map */ })
            .ReturnsAsync((List<Question> l) => l);

        SetupSave();

        var dto = TestHelpers.ValidMcqDto(status: QuestionStatus.Inprogress);
        await CreateService().UpdateQuestionAsync(1, 1, dto);

        // dto.Status phải bị ép thành Active trước khi map
        Assert.Equal(QuestionStatus.Active, dto.Status);
    }

    // ── TC08: isUsed=false, Status=Draft → update trực tiếp, không clone ─
    [Fact]
    public async Task TC08_NotUsedDraft_UpdatesInPlace()
    {
        var entity = TestHelpers.ValidQuestionEntity(status: QuestionStatus.Draft);
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(It.IsAny<int>())).ReturnsAsync(entity);
        _repoMock.Setup(r => r.IsQuestionUsedAsync(It.IsAny<int>())).ReturnsAsync(false);
        SetupValidation();
        SetupSave();

        await CreateService().UpdateQuestionAsync(1, 1, TestHelpers.ValidMcqDto());

        _repoMock.Verify(r => r.CreateQuestionsAsync(It.IsAny<List<Question>>()), Times.Never);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // ── TC09: Update trực tiếp, answer bị bỏ → DeleteQuestionAnswersAsync
    [Fact]
    public async Task TC09_UpdateInPlace_RemovesDroppedAnswers()
    {
        var entity = TestHelpers.ValidQuestionEntity(status: QuestionStatus.Draft);
        // entity có answerId=10 và 11; request chỉ gửi 10 → 11 bị remove
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(It.IsAny<int>())).ReturnsAsync(entity);
        _repoMock.Setup(r => r.IsQuestionUsedAsync(It.IsAny<int>())).ReturnsAsync(false);
        _repoMock.Setup(r => r.DeleteQuestionAnswersAsync(It.IsAny<List<QuestionAnswer>>())).Returns(Task.CompletedTask);
        SetupValidation();
        SetupSave();

        var dto = TestHelpers.ValidMcqDto();
        dto.Answers[0].AnswerId = 10; // giữ 10
        // answer 11 không có trong request → toRemove

        await CreateService().UpdateQuestionAsync(1, 1, dto);

        _repoMock.Verify(r => r.DeleteQuestionAnswersAsync(
            It.Is<List<QuestionAnswer>>(l => l.Any(a => a.QuestionAnswerId == 11))), Times.Once);
    }

    // ── TC10: Update trực tiếp, tất cả answers hợp lệ → không xoá gì ────
    [Fact]
    public async Task TC10_UpdateInPlace_NoAnswersRemoved()
    {
        var entity = TestHelpers.ValidQuestionEntity(status: QuestionStatus.Draft);
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(It.IsAny<int>())).ReturnsAsync(entity);
        _repoMock.Setup(r => r.IsQuestionUsedAsync(It.IsAny<int>())).ReturnsAsync(false);
        SetupValidation();
        SetupSave();

        var dto = TestHelpers.ValidMcqDto();
        dto.Answers[0].AnswerId = 10;
        dto.Answers[1].AnswerId = 11; // giữ cả 2

        await CreateService().UpdateQuestionAsync(1, 1, dto);

        _repoMock.Verify(r => r.DeleteQuestionAnswersAsync(It.IsAny<List<QuestionAnswer>>()), Times.Never);
    }

    // ── TC11: SaveChanges được gọi đúng 1 lần ở cả hai nhánh ─────────────
    [Fact]
    public async Task TC11_AlwaysSavesOnce()
    {
        var entity = TestHelpers.ValidQuestionEntity(status: QuestionStatus.Draft);
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(It.IsAny<int>())).ReturnsAsync(entity);
        _repoMock.Setup(r => r.IsQuestionUsedAsync(It.IsAny<int>())).ReturnsAsync(false);
        SetupValidation();
        SetupSave();

        await CreateService().UpdateQuestionAsync(1, 1, TestHelpers.ValidMcqDto());

        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
    [Fact]
    public async Task UpdateQuestion_FillBlank_ComplexGroups_ShouldCoverAllBranches()
    {
        // Arrange: Tạo câu hỏi FillBlank hiện tại có 2 nhóm
        var existingQuestion = TestHelpers.ValidQuestionEntity(status: QuestionStatus.Draft);
        existingQuestion.QuestionType = QuestionType.FillBlank;

        var group1 = new GroupAnswer { GroupAnswerId = 10, Name = "Group 1" };
        var group2 = new GroupAnswer { GroupAnswerId = 11, Name = "Group 2" };

        // Gán group cho các câu trả lời hiện tại
        existingQuestion.QuestionAnswers = new List<QuestionAnswer>
    {
        new() { QuestionAnswerId = 1, Content = "placeholder[0]({10})", GroupAnswer = group1, GroupAnswerId = 10 },
        new() { QuestionAnswerId = 2, Content = "placeholder[1]", GroupAnswer = group2, GroupAnswerId = 11 }
    };

        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(It.IsAny<int>())).ReturnsAsync(existingQuestion);
        _repoMock.Setup(r => r.IsQuestionUsedAsync(It.IsAny<int>())).ReturnsAsync(false);
        _repoMock.Setup(r => r.ChapterExistsAsync(It.IsAny<int>())).ReturnsAsync(true);

        // DTO yêu cầu: Xóa group 11, Cập nhật group 10, Thêm group mới 12 phụ thuộc vào 10
        var dto = TestHelpers.ValidFillBlankDto();
        dto.QuestionType = QuestionType.FillBlank;
        dto.BlankGroups = new List<GroupAnswerDto>
    {
        new() {
            GroupAnswerId = 10,
            Name = "Updated Group 10",
            BlankIndices = new List<int> { 0 }
        },
        new() {
            Name = "New Group 12",
            BlankIndices = new List<int> { 1 },
            DependsOnGroupIndex = 0 // Phụ thuộc vào group ở vị trí index 0 (Group 10)
        }
    };

        // Act
        await CreateService().UpdateQuestionAsync(1, 1, dto);

        // Assert: Kiểm tra repo đã gọi xóa group không còn tồn tại (Group 11)
        _repoMock.Verify(r => r.DeleteGroupAnswersAsync(It.Is<List<GroupAnswer>>(l => l.Any(g => g.GroupAnswerId == 11))), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
    [Fact]
    public async Task UpdateQuestion_Draft_RemoveSomeAnswers_ShouldDeleteAnswers()
    {
        // Arrange: Câu hỏi Draft có 3 answers
        var entity = TestHelpers.ValidQuestionEntity(status: QuestionStatus.Draft);
        entity.QuestionAnswers = new List<QuestionAnswer>
    {
        new() { QuestionAnswerId = 100 },
        new() { QuestionAnswerId = 101 },
        new() { QuestionAnswerId = 102 }
    };

        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(It.IsAny<int>())).ReturnsAsync(entity);
        _repoMock.Setup(r => r.IsQuestionUsedAsync(It.IsAny<int>())).ReturnsAsync(false);
        _repoMock.Setup(r => r.ChapterExistsAsync(It.IsAny<int>())).ReturnsAsync(true);

        // DTO chỉ gửi về 2 answers (giữ 100, 101 -> xóa 102)
        var dto = TestHelpers.ValidMcqDto();
        dto.Answers = new List<AnswerDto>
    {
        new() { AnswerId = 100, Content = "A", IsCorrect = true, Point = 50 },
        new() { AnswerId = 101, Content = "B", IsCorrect = false, Point = 50 }
    };

        // Act
        await CreateService().UpdateQuestionAsync(1, 1, dto);

        // Assert
        _repoMock.Verify(r => r.DeleteQuestionAnswersAsync(It.Is<List<QuestionAnswer>>(l => l.Count == 1)), Times.Once);
    }
}
