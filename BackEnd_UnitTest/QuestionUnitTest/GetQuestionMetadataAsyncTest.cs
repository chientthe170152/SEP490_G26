using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.QuestionUnitTest;

public class GetQuestionMetadataAsyncTest
{
    private readonly Mock<IQuestionRepository> _repoMock = new();
    private readonly Mock<ILogger<QuestionService>> _loggerMock = new();

    private QuestionService CreateService() => new(_repoMock.Object, _loggerMock.Object);

    // ── TC01: Trả về đúng InputTypes từ repo ─────────────────────────────
    [Fact]
    public async Task TC01_ReturnsInputTypes_MappedCorrectly()
    {
        _repoMock.Setup(r => r.GetInputTypesAsync())
            .ReturnsAsync(new List<InputType>
            {
                new() { InputTypeId = 1, Name = "Số nguyên", GroupType = "number" },
                new() { InputTypeId = 2, Name = "Văn bản",   GroupType = "text"   }
            });

        _repoMock.Setup(r => r.GetSubjectsWithChaptersAsync())
            .ReturnsAsync(new List<Subject>());

        var result = await CreateService().GetQuestionMetadataAsync();

        Assert.Equal(2, result.InputTypes.Count);
        Assert.Equal("Số nguyên", result.InputTypes[0].Name);
        Assert.Equal("number", result.InputTypes[0].GroupType);
    }

    // ── TC02: Trả về đúng Subjects + Chapters ────────────────────────────
    [Fact]
    public async Task TC02_ReturnsSubjectsWithChapters_MappedCorrectly()
    {
        _repoMock.Setup(r => r.GetInputTypesAsync()).ReturnsAsync(new List<InputType>());

        _repoMock.Setup(r => r.GetSubjectsWithChaptersAsync())
            .ReturnsAsync(new List<Subject>
            {
                new()
                {
                    SubjectId = 10,
                    Name = "Toán",
                    Code = "MATH",
                    Chapters = new List<Chapter>
                    {
                        new() { ChapterId = 1, Name = "Đại số" },
                        new() { ChapterId = 2, Name = "Hình học" }
                    }
                }
            });

        var result = await CreateService().GetQuestionMetadataAsync();

        Assert.Single(result.Subjects);
        Assert.Equal("Toán", result.Subjects[0].Name);
        Assert.Equal("MATH", result.Subjects[0].Code);
        Assert.Equal(2, result.Subjects[0].Chapters.Count);
        Assert.Equal("Đại số", result.Subjects[0].Chapters[0].Name);
    }

    // ── TC03: Repo trả về empty → DTO có list rỗng, không throw ─────────
    [Fact]
    public async Task TC03_EmptyData_ReturnsEmptyLists()
    {
        _repoMock.Setup(r => r.GetInputTypesAsync()).ReturnsAsync(new List<InputType>());
        _repoMock.Setup(r => r.GetSubjectsWithChaptersAsync()).ReturnsAsync(new List<Subject>());

        var result = await CreateService().GetQuestionMetadataAsync();

        Assert.Empty(result.InputTypes);
        Assert.Empty(result.Subjects);
    }
}
