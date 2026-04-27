using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.Exceptions;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using BackEnd_UnitTest._Shared;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.QuestionUnitTest;

// F30 - UpdateQuestionStatusAsync
// Source: QuestionService.cs:127-154
// Branches:
//   1. !QuestionStatus.IsValid(status)              -> throw QuestionValidationException
//   2. q.CreatedByUserId == userId                  -> set status, increment count
//   3. q.CreatedByUserId != userId                  -> skip (defensive)
//   4. updatedCount > 0                             -> SaveChangesAsync + log
//   5. updatedCount == 0                            -> no save, return 0
public class F30_UpdateQuestionStatusAsync_Tests
{
    private readonly Mock<IQuestionRepository> _repoMock = new(MockBehavior.Strict);
    private readonly QuestionService _service;

    public F30_UpdateQuestionStatusAsync_Tests()
    {
        _service = new QuestionService(_repoMock.Object, new Mock<ILogger<QuestionService>>().Object);
    }

    private static Question MakeQ(int qid, int ownerId)
        => new()
        {
            QuestionId = qid,
            CreatedByUserId = ownerId,
            QuestionType = QuestionType.Mcq,
            QuestionContent = "{}",
            ChapterId = 1,
            Difficulty = 1,
            UpdatedAtUtc = DateTime.UtcNow,
            Status = QuestionStatus.Active,
            ConcurrencyStamp = Array.Empty<byte>()
        };

    [Fact(DisplayName = "UpdateQuestionStatusAsync - UTCID01 - Status hợp lệ + tất cả là của user -> update tất cả")]
    [TestType("N")]
    public async Task UpdateQuestionStatusAsync_UTCID01_AllOwned_ShouldUpdate()
    {
        var ids = new List<int> { 1, 2 };
        var qs = new List<Question> { MakeQ(1, 100), MakeQ(2, 100) };
        _repoMock.Setup(r => r.GetQuestionsByIdsAsync(ids)).ReturnsAsync(qs);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var count = await _service.UpdateQuestionStatusAsync(ids, 100, QuestionStatus.Archive);

        Assert.Equal(2, count);
        Assert.All(qs, q => Assert.Equal(QuestionStatus.Archive, q.Status));
    }

    [Fact(DisplayName = "UpdateQuestionStatusAsync - UTCID02 - Status không hợp lệ -> throw QuestionValidationException")]
    [TestType("A")]
    public async Task UpdateQuestionStatusAsync_UTCID02_InvalidStatus_ShouldThrow()
    {
        await Assert.ThrowsAsync<QuestionValidationException>(
            () => _service.UpdateQuestionStatusAsync(new List<int> { 1 }, 100, "INVALID_STATUS"));
        _repoMock.Verify(r => r.GetQuestionsByIdsAsync(It.IsAny<IEnumerable<int>>()), Times.Never);
    }

    [Fact(DisplayName = "UpdateQuestionStatusAsync - UTCID03 - Trong list có question không thuộc user -> chỉ update của user")]
    [TestType("B")]
    public async Task UpdateQuestionStatusAsync_UTCID03_MixedOwners_ShouldUpdateOnlyOwn()
    {
        var ids = new List<int> { 1, 2, 3 };
        var qs = new List<Question> { MakeQ(1, 100), MakeQ(2, 999), MakeQ(3, 100) };
        _repoMock.Setup(r => r.GetQuestionsByIdsAsync(ids)).ReturnsAsync(qs);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var count = await _service.UpdateQuestionStatusAsync(ids, 100, QuestionStatus.Archive);

        Assert.Equal(2, count);
        Assert.Equal(QuestionStatus.Active, qs[1].Status); // Not updated (different owner)
    }

    [Fact(DisplayName = "UpdateQuestionStatusAsync - UTCID04 - Tất cả không thuộc user -> count=0, không save (boundary)")]
    [TestType("B")]
    public async Task UpdateQuestionStatusAsync_UTCID04_NoneOwned_ShouldReturnZeroNoSave()
    {
        var ids = new List<int> { 1 };
        var qs = new List<Question> { MakeQ(1, 999) };
        _repoMock.Setup(r => r.GetQuestionsByIdsAsync(ids)).ReturnsAsync(qs);

        var count = await _service.UpdateQuestionStatusAsync(ids, 100, QuestionStatus.Archive);

        Assert.Equal(0, count);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}
