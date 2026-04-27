using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using BackEnd_UnitTest._Shared;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.QuestionUnitTest;

// F31 - GetQuestionMetadataAsync
// Source: QuestionService.cs:317-342 — pure projection delegation. No conditional branches.
public class F31_GetQuestionMetadataAsync_Tests
{
    private readonly Mock<IQuestionRepository> _repoMock = new(MockBehavior.Strict);
    private readonly QuestionService _service;

    public F31_GetQuestionMetadataAsync_Tests()
    {
        _service = new QuestionService(_repoMock.Object, new Mock<ILogger<QuestionService>>().Object);
    }

    [Fact(DisplayName = "GetQuestionMetadataAsync - UTCID01 - Có inputTypes + subjects -> trả metadata")]
    [TestType("N")]
    public async Task GetQuestionMetadataAsync_UTCID01_HasData_ShouldProjectAll()
    {
        var inputTypes = new List<InputType>
        {
            new() { InputTypeId = 1, Name = "Text", Regex = ".*", GroupType = "G1" }
        };
        var subjects = new List<Subject>
        {
            new()
            {
                SubjectId = 1, Name = "Toán", Code = "MAE",
                Chapters = new List<Chapter> { new() { ChapterId = 10, SubjectId = 1, Name = "Chương 1" } }
            }
        };
        _repoMock.Setup(r => r.GetInputTypesAsync()).ReturnsAsync(inputTypes);
        _repoMock.Setup(r => r.GetSubjectsWithChaptersAsync()).ReturnsAsync(subjects);

        var result = await _service.GetQuestionMetadataAsync();

        Assert.Single(result.InputTypes);
        Assert.Equal("Text", result.InputTypes[0].Name);
        Assert.Single(result.Subjects);
        Assert.Equal("MAE", result.Subjects[0].Code);
        Assert.Single(result.Subjects[0].Chapters);
    }

    [Fact(DisplayName = "GetQuestionMetadataAsync - UTCID02 - DB rỗng -> trả metadata rỗng")]
    [TestType("B")]
    public async Task GetQuestionMetadataAsync_UTCID02_NoData_ShouldReturnEmpty()
    {
        _repoMock.Setup(r => r.GetInputTypesAsync()).ReturnsAsync(new List<InputType>());
        _repoMock.Setup(r => r.GetSubjectsWithChaptersAsync()).ReturnsAsync(new List<Subject>());

        var result = await _service.GetQuestionMetadataAsync();

        Assert.Empty(result.InputTypes);
        Assert.Empty(result.Subjects);
    }
}
