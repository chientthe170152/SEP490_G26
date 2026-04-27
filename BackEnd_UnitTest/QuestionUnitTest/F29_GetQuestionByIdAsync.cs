using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using BackEnd_UnitTest._Shared;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.QuestionUnitTest;

// F29 - GetQuestionByIdAsync
// Source: QuestionService.cs:98-125
// Branches:
//   1. q == null                                  -> throw KeyNotFoundException
//   2. q.CreatedByUserId != userId                -> throw KeyNotFoundException
//   3. q.QuestionType == FillBlank                -> populate BlankGroups
//   4. else (MCQ)                                 -> skip BlankGroups
//   5. ParseContent: empty/null json              -> return (null, null)
//   6. ParseContent: try succeeds                 -> return parsed stem/frame
//   7. ParseContent: try throws (invalid JSON)    -> catch -> (json, null)
public class F29_GetQuestionByIdAsync_Tests
{
    private readonly Mock<IQuestionRepository> _repoMock = new(MockBehavior.Strict);
    private readonly QuestionService _service;

    public F29_GetQuestionByIdAsync_Tests()
    {
        _service = new QuestionService(_repoMock.Object, new Mock<ILogger<QuestionService>>().Object);
    }

    private static Question MakeQ(string type = QuestionType.Mcq, string content = "{\"stem\":\"What is 1+1?\",\"frame\":null}")
        => new()
        {
            QuestionId = 5,
            CreatedByUserId = 100,
            QuestionType = type,
            QuestionContent = content,
            ChapterId = 1,
            Difficulty = 2,
            UpdatedAtUtc = DateTime.UtcNow,
            Status = QuestionStatus.Active,
            ConcurrencyStamp = Array.Empty<byte>(),
            QuestionAnswers = new List<QuestionAnswer>()
        };

    [Fact(DisplayName = "GetQuestionByIdAsync - UTCID01 - MCQ valid + JSON content -> trả DTO")]
    [TestType("N")]
    public async Task GetQuestionByIdAsync_UTCID01_McqValid_ShouldReturnDto()
    {
        var q = MakeQ();
        q.QuestionAnswers.Add(new QuestionAnswer { QuestionAnswerId = 50, Content = "Option A", CorrectAnswer = "A", IsCorrect = true, Point = 50, ConcurrencyStamp = Array.Empty<byte>() });
        q.QuestionAnswers.Add(new QuestionAnswer { QuestionAnswerId = 51, Content = "Option B", CorrectAnswer = "B", IsCorrect = false, Point = 50, ConcurrencyStamp = Array.Empty<byte>() });
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(5)).ReturnsAsync(q);

        var dto = await _service.GetQuestionByIdAsync(5, 100);

        Assert.Equal(QuestionType.Mcq, dto.QuestionType);
        Assert.Equal("What is 1+1?", dto.Stem);
        Assert.Null(dto.Frame);
        Assert.Equal(2, dto.Answers.Count);
        Assert.Null(dto.BlankGroups);
    }

    [Fact(DisplayName = "GetQuestionByIdAsync - UTCID02 - FillBlank với BlankGroups -> populate BlankGroups")]
    [TestType("N")]
    public async Task GetQuestionByIdAsync_UTCID02_FillBlankValid_ShouldReturnGroups()
    {
        var group = new GroupAnswer { GroupAnswerId = 7, Name = "G1", DependsOnGroupId = null };
        var q = MakeQ(type: QuestionType.FillBlank, content: "{\"stem\":\"X is __\",\"frame\":\"X is placeholder[1]\"}");
        var ans = new QuestionAnswer { QuestionAnswerId = 60, Content = "placeholder[1]{type=text}", CorrectAnswer = "answer", IsCorrect = true, Point = 100, ConcurrencyStamp = Array.Empty<byte>(), GroupAnswerId = 7, GroupAnswer = group };
        q.QuestionAnswers.Add(ans);
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(5)).ReturnsAsync(q);

        var dto = await _service.GetQuestionByIdAsync(5, 100);

        Assert.NotNull(dto.BlankGroups);
        Assert.Single(dto.BlankGroups);
        Assert.Equal(7, dto.BlankGroups![0].GroupAnswerId);
        Assert.Contains(1, dto.BlankGroups[0].BlankIndices);
    }

    [Fact(DisplayName = "GetQuestionByIdAsync - UTCID03 - q == null -> KeyNotFoundException")]
    [TestType("A")]
    public async Task GetQuestionByIdAsync_UTCID03_QuestionNotFound_ShouldThrow()
    {
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(99)).ReturnsAsync((Question?)null);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetQuestionByIdAsync(99, 100));
        Assert.Equal("Không tìm thấy câu hỏi hoặc bạn không có quyền xem.", ex.Message);
    }

    [Fact(DisplayName = "GetQuestionByIdAsync - UTCID04 - userId không phải owner -> KeyNotFoundException")]
    [TestType("A")]
    public async Task GetQuestionByIdAsync_UTCID04_NotOwner_ShouldThrow()
    {
        var q = MakeQ();
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(5)).ReturnsAsync(q);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetQuestionByIdAsync(5, 999));
        Assert.Equal("Không tìm thấy câu hỏi hoặc bạn không có quyền xem.", ex.Message);
    }

    [Fact(DisplayName = "GetQuestionByIdAsync - UTCID05 - QuestionContent rỗng -> stem/frame null (boundary)")]
    [TestType("B")]
    public async Task GetQuestionByIdAsync_UTCID05_EmptyContent_ShouldFallback()
    {
        var q = MakeQ(content: string.Empty);
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(5)).ReturnsAsync(q);

        var dto = await _service.GetQuestionByIdAsync(5, 100);

        Assert.Equal(string.Empty, dto.Stem);
        Assert.Null(dto.Frame);
    }

    [Fact(DisplayName = "GetQuestionByIdAsync - UTCID06 - QuestionContent invalid JSON -> catch fallback")]
    [TestType("A")]
    public async Task GetQuestionByIdAsync_UTCID06_InvalidJson_ShouldFallback()
    {
        var q = MakeQ(content: "this is not json");
        _repoMock.Setup(r => r.GetQuestionWithAnswersAsync(5)).ReturnsAsync(q);

        var dto = await _service.GetQuestionByIdAsync(5, 100);

        Assert.Equal("this is not json", dto.Stem);
        Assert.Null(dto.Frame);
    }
}
