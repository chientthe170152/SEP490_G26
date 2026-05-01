using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Common.Errors;
using Backend.Constants;
using Backend.DTOs.Question;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Backend_UnitTest.QuestionUnitTest
{
    public class QuestionServiceMissingCoverageTests
    {
        private readonly Mock<IQuestionRepository> _repo = new();
        private readonly Mock<ILogger<QuestionService>> _logger = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly QuestionService _service;

        public QuestionServiceMissingCoverageTests()
        {
            _service = new QuestionService(_repo.Object, _logger.Object, _currentUser.Object, TimeProvider.System);
        }

        [Fact(DisplayName = "GetQuestionMetadataAsync - UTCID-WithChapters - Subject có chapters -> map ChapterDto")]
        public async Task GetMetadata_WithChapters_ShouldMapChapters()
        {
            _repo.Setup(r => r.GetInputTypesAsync()).ReturnsAsync(new List<InputType>());
            _repo.Setup(r => r.GetSubjectsWithChaptersAsync()).ReturnsAsync(new List<Subject>
            {
                new()
                {
                    SubjectId = 1, Name = "Toán", Code = "MATH",
                    Chapters = new List<Chapter>
                    {
                        new() { ChapterId = 11, Name = "Đại số" },
                        new() { ChapterId = 12, Name = "Hình học" }
                    }
                }
            });

            var result = await _service.GetQuestionMetadataAsync();

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value.Subjects);
            Assert.Equal(2, result.Value.Subjects[0].Chapters.Count);
            Assert.Equal(11, result.Value.Subjects[0].Chapters[0].ChapterId);
            Assert.Equal("Đại số", result.Value.Subjects[0].Chapters[0].Name);
        }

        // ---------- GetQuestionsAsync ----------

        [Fact(DisplayName = "GetQuestionsAsync - UTCID01 - Trả về kết quả + clamp pagesize")]
        public async Task GetQuestions_ShouldClampPageSize()
        {
            var query = new QuestionListQueryDto { Page = 0, PageSize = 100 };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetQuestionsAsync(query, 1)).ReturnsAsync((new List<QuestionSummaryDto>(), 0));

            var result = await _service.GetQuestionsAsync(query);

            Assert.True(result.IsSuccess);
            Assert.Equal(50, result.Value.PageSize);
            Assert.Equal(1, result.Value.CurrentPage);
        }

        // ---------- CreateQuestionsAsync ----------

        [Fact(DisplayName = "CreateQuestionsAsync - UTCID-Null - request null -> EmptyList")]
        public async Task Create_NullRequest_ShouldReturnEmptyList()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);

            var result = await _service.CreateQuestionsAsync(null!);

            Assert.True(result.IsFailure);
        }

        [Fact(DisplayName = "CreateQuestionsAsync - UTCID-FillBlank - tạo câu fill-in-blank với group answer")]
        public async Task Create_FillBlank_WithGroups()
        {
            var request = new List<QuestionDto>
            {
                new()
                {
                    QuestionType = QuestionType.FillBlank,
                    ChapterId = 1,
                    Difficulty = 1,
                    Stem = "_____",
                    QuestionPurpose = 1,
                    Status = QuestionStatus.Draft,
                    Answers = new List<AnswerDto>
                    {
                        new() { Content = "placeholder[1]", CorrectAnswer = "abc", Point = 1, InputTypeId = 5 }
                    },
                    BlankGroups = new List<GroupAnswerDto>
                    {
                        new() { Name = "Group1", BlankIndices = new List<int> { 1 } }
                    }
                }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            var created = new List<Question>
            {
                new()
                {
                    QuestionId = 1, QuestionType = QuestionType.FillBlank, CreatedByUserId = 1,
                    QuestionAnswers = new List<QuestionAnswer>
                    {
                        new() { QuestionAnswerId = 10, Content = "placeholder[1]", BlankInputs = new List<BlankInput>() }
                    }
                }
            };
            _repo.Setup(r => r.CreateQuestionsAsync(It.IsAny<List<Question>>())).ReturnsAsync(created);
            _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.CreateQuestionsAsync(request);

            Assert.True(result.IsSuccess);
        }

        // ---------- UpdateQuestionAsync ----------

        [Fact(DisplayName = "UpdateQuestionAsync - UTCID-WrongOwner - Đúng câu hỏi, sai owner -> NotFound")]
        public async Task Update_WrongOwner_ShouldReturnNotFound()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetQuestionWithAnswersAsync(1))
                .ReturnsAsync(new Question { QuestionId = 1, CreatedByUserId = 2, Status = QuestionStatus.Draft });

            var result = await _service.UpdateQuestionAsync(1, new QuestionDto());

            Assert.Equal(QuestionErrors.NotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "UpdateQuestionAsync - UTCID-Used - Status Inprogress / used -> Clone bản mới")]
        public async Task Update_Used_ShouldArchiveAndClone()
        {
            var existing = new Question
            {
                QuestionId = 1, CreatedByUserId = 1, Status = QuestionStatus.Inprogress,
                QuestionAnswers = new List<QuestionAnswer>(), QuestionContent = "{\"stem\":\"x\"}"
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetQuestionWithAnswersAsync(1)).ReturnsAsync(existing);
            _repo.Setup(r => r.IsQuestionUsedAsync(1)).ReturnsAsync(true);
            _repo.Setup(r => r.CreateQuestionsAsync(It.IsAny<List<Question>>()))
                .ReturnsAsync((List<Question> qs) =>
                {
                    qs[0].QuestionId = 99;
                    return qs;
                });
            _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var dto = new QuestionDto
            {
                QuestionType = QuestionType.Mcq, ChapterId = 1, Difficulty = 2,
                Stem = "Q",
                Status = QuestionStatus.Inprogress, // should be normalized to Active
                Answers = new List<AnswerDto>
                {
                    new() { Content = "a", IsCorrect = true, Point = 1 }
                }
            };

            var result = await _service.UpdateQuestionAsync(1, dto);

            Assert.True(result.IsSuccess);
            Assert.Equal(QuestionStatus.Archive, existing.Status);
            Assert.Equal(99, result.Value.QuestionId);
        }

        [Fact(DisplayName = "UpdateQuestionAsync - UTCID-Archive - Status Archive cũng được clone")]
        public async Task Update_StatusArchive_ShouldArchiveAndClone()
        {
            var existing = new Question
            {
                QuestionId = 1, CreatedByUserId = 1, Status = QuestionStatus.Archive,
                QuestionAnswers = new List<QuestionAnswer>(), QuestionContent = ""
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetQuestionWithAnswersAsync(1)).ReturnsAsync(existing);
            _repo.Setup(r => r.IsQuestionUsedAsync(1)).ReturnsAsync(false);
            _repo.Setup(r => r.CreateQuestionsAsync(It.IsAny<List<Question>>()))
                .ReturnsAsync((List<Question> qs) => { qs[0].QuestionId = 100; return qs; });
            _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.UpdateQuestionAsync(1, new QuestionDto
            {
                QuestionType = QuestionType.Mcq, Status = QuestionStatus.Archive,
                Answers = new List<AnswerDto> { new() { Content = "x" } }
            });

            Assert.True(result.IsSuccess);
        }

        [Fact(DisplayName = "UpdateQuestionAsync - UTCID-InPlaceUpdate - Draft + chưa dùng -> update tại chỗ + xóa answer cũ")]
        public async Task Update_InPlace_ShouldRemoveStaleAnswers()
        {
            var existing = new Question
            {
                QuestionId = 1, CreatedByUserId = 1, Status = QuestionStatus.Draft,
                QuestionType = QuestionType.Mcq,
                QuestionAnswers = new List<QuestionAnswer>
                {
                    new() { QuestionAnswerId = 10, Content = "old A" },
                    new() { QuestionAnswerId = 11, Content = "old B" }
                }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetQuestionWithAnswersAsync(1)).ReturnsAsync(existing);
            _repo.Setup(r => r.IsQuestionUsedAsync(1)).ReturnsAsync(false);
            _repo.Setup(r => r.DeleteQuestionAnswersAsync(It.IsAny<IEnumerable<QuestionAnswer>>())).Returns(Task.CompletedTask);
            _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var dto = new QuestionDto
            {
                QuestionType = QuestionType.Mcq, ChapterId = 1, Difficulty = 1,
                Status = QuestionStatus.Draft,
                Answers = new List<AnswerDto>
                {
                    new() { AnswerId = 10, Content = "Updated A", IsCorrect = true, Point = 1 }
                }
            };

            var result = await _service.UpdateQuestionAsync(1, dto);

            Assert.True(result.IsSuccess);
            _repo.Verify(r => r.DeleteQuestionAnswersAsync(It.IsAny<IEnumerable<QuestionAnswer>>()), Times.Once);
        }

        [Fact(DisplayName = "UpdateQuestionAsync - UTCID-FillBlankUpdate - FillBlank + match by index")]
        public async Task Update_FillBlank_MatchByIndex()
        {
            var existing = new Question
            {
                QuestionId = 1, CreatedByUserId = 1, Status = QuestionStatus.Draft,
                QuestionType = QuestionType.FillBlank,
                QuestionAnswers = new List<QuestionAnswer>
                {
                    new() { QuestionAnswerId = 10, Content = "placeholder[1]", BlankInputs = new List<BlankInput>() }
                }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetQuestionWithAnswersAsync(1)).ReturnsAsync(existing);
            _repo.Setup(r => r.IsQuestionUsedAsync(1)).ReturnsAsync(false);
            _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _repo.Setup(r => r.DeleteGroupAnswersAsync(It.IsAny<IEnumerable<GroupAnswer>>())).Returns(Task.CompletedTask);

            var dto = new QuestionDto
            {
                QuestionType = QuestionType.FillBlank, ChapterId = 1, Difficulty = 1,
                Status = QuestionStatus.Draft,
                Answers = new List<AnswerDto>
                {
                    new() { Content = "placeholder[1]", BlankIndex = 1, CorrectAnswer = "answer", InputTypeId = 5 }
                },
                BlankGroups = new List<GroupAnswerDto>
                {
                    new() { Name = "G", BlankIndices = new List<int> { 1 } }
                }
            };

            var result = await _service.UpdateQuestionAsync(1, dto);

            Assert.True(result.IsSuccess);
        }

        // ---------- GetQuestionByIdAsync ----------

        [Fact(DisplayName = "GetQuestionByIdAsync - UTCID-WrongOwner - Sai owner -> NotFound")]
        public async Task GetQuestion_WrongOwner_ShouldReturnNotFound()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetQuestionWithAnswersAsync(1))
                .ReturnsAsync(new Question { QuestionId = 1, CreatedByUserId = 2 });

            var result = await _service.GetQuestionByIdAsync(1);

            Assert.Equal(QuestionErrors.NotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "GetQuestionByIdAsync - UTCID-MCQ - Trả về QuestionDto cho MCQ")]
        public async Task GetQuestion_MCQ_ShouldReturnDto()
        {
            var q = new Question
            {
                QuestionId = 1, CreatedByUserId = 1,
                QuestionType = QuestionType.Mcq,
                ChapterId = 5,
                Difficulty = 2,
                Status = QuestionStatus.Draft,
                QuestionPurpose = 1,
                QuestionContent = "{\"stem\":\"What?\",\"frame\":\"Frame\"}",
                QuestionAnswers = new List<QuestionAnswer>
                {
                    new() { QuestionAnswerId = 10, Content = "A", IsCorrect = true, Point = 1, BlankInputs = new List<BlankInput>() }
                }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetQuestionWithAnswersAsync(1)).ReturnsAsync(q);

            var result = await _service.GetQuestionByIdAsync(1);

            Assert.True(result.IsSuccess);
            Assert.Equal("What?", result.Value.Stem);
            Assert.Equal("Frame", result.Value.Frame);
            Assert.Single(result.Value.Answers!);
        }

        [Fact(DisplayName = "GetQuestionByIdAsync - UTCID-FillBlank - Trả về với BlankGroups")]
        public async Task GetQuestion_FillBlank_ShouldIncludeGroups()
        {
            var group = new GroupAnswer { GroupAnswerId = 100, Name = "G1", DependsOnGroupId = null };
            var q = new Question
            {
                QuestionId = 1, CreatedByUserId = 1,
                QuestionType = QuestionType.FillBlank,
                QuestionContent = "{\"stem\":\"x\"}",
                QuestionAnswers = new List<QuestionAnswer>
                {
                    new()
                    {
                        QuestionAnswerId = 10,
                        Content = "placeholder[1]",
                        GroupAnswerId = 100, GroupAnswer = group,
                        BlankInputs = new List<BlankInput>()
                    }
                }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetQuestionWithAnswersAsync(1)).ReturnsAsync(q);

            var result = await _service.GetQuestionByIdAsync(1);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value.BlankGroups);
            Assert.Single(result.Value.BlankGroups!);
            Assert.Equal(100, result.Value.BlankGroups![0].GroupAnswerId);
        }

        [Fact(DisplayName = "GetQuestionByIdAsync - UTCID-RawContent - Content không phải JSON -> trả về raw")]
        public async Task GetQuestion_NonJson_ShouldReturnRaw()
        {
            var q = new Question
            {
                QuestionId = 1, CreatedByUserId = 1,
                QuestionType = QuestionType.Mcq,
                QuestionContent = "raw text not json",
                QuestionAnswers = new List<QuestionAnswer>()
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetQuestionWithAnswersAsync(1)).ReturnsAsync(q);

            var result = await _service.GetQuestionByIdAsync(1);

            Assert.True(result.IsSuccess);
            Assert.Equal("raw text not json", result.Value.Stem);
        }

        [Fact(DisplayName = "GetQuestionByIdAsync - UTCID-EmptyContent - Content rỗng -> Stem = empty")]
        public async Task GetQuestion_EmptyContent_ShouldReturnEmptyStem()
        {
            var q = new Question
            {
                QuestionId = 1, CreatedByUserId = 1,
                QuestionType = QuestionType.Mcq,
                QuestionContent = "",
                QuestionAnswers = new List<QuestionAnswer>()
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetQuestionWithAnswersAsync(1)).ReturnsAsync(q);

            var result = await _service.GetQuestionByIdAsync(1);

            Assert.True(result.IsSuccess);
            Assert.Equal(string.Empty, result.Value.Stem);
        }

        [Fact(DisplayName = "GetQuestionByIdAsync - UTCID-FillBlankWithBlankIndices - GroupAnswer with multiple blanks")]
        public async Task GetQuestion_FillBlank_WithBlankIndices()
        {
            var group = new GroupAnswer { GroupAnswerId = 100, Name = "G1", DependsOnGroupId = 50 };
            var q = new Question
            {
                QuestionId = 1, CreatedByUserId = 1,
                QuestionType = QuestionType.FillBlank,
                QuestionContent = "{\"stem\":\"x\"}",
                QuestionAnswers = new List<QuestionAnswer>
                {
                    new() { QuestionAnswerId = 10, Content = "placeholder[1]{a}", GroupAnswerId = 100, GroupAnswer = group, BlankInputs = new List<BlankInput>() },
                    new() { QuestionAnswerId = 11, Content = "placeholder[2]", GroupAnswerId = 100, GroupAnswer = group, BlankInputs = new List<BlankInput>() }
                }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetQuestionWithAnswersAsync(1)).ReturnsAsync(q);

            var result = await _service.GetQuestionByIdAsync(1);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value.BlankGroups!);
            Assert.Equal(50, result.Value.BlankGroups![0].DependsOnGroupId);
            Assert.Equal(2, result.Value.BlankGroups![0].BlankIndices!.Count);
        }

[Fact(DisplayName = "CreateQuestionsAsync - UTCID-FillBlankNoGroups - FillBlank không có blank groups -> deletes existing groups")]
        public async Task Create_FillBlank_NoGroups_ShouldDeleteExistingGroups()
        {
            var request = new List<QuestionDto>
            {
                new()
                {
                    QuestionType = QuestionType.FillBlank, ChapterId = 1, Difficulty = 1,
                    Stem = "Q", QuestionPurpose = 1,
                    Answers = new List<AnswerDto> { new() { Content = "placeholder[1]" } },
                    BlankGroups = null
                }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            var grp = new GroupAnswer { GroupAnswerId = 50, Name = "X" };
            var qa = new QuestionAnswer { QuestionAnswerId = 10, Content = "placeholder[1]", GroupAnswerId = 50, GroupAnswer = grp, BlankInputs = new List<BlankInput>() };
            _repo.Setup(r => r.CreateQuestionsAsync(It.IsAny<List<Question>>()))
                .ReturnsAsync(new List<Question>
                {
                    new()
                    {
                        QuestionId = 1, QuestionType = QuestionType.FillBlank,
                        QuestionAnswers = new List<QuestionAnswer> { qa }
                    }
                });
            _repo.Setup(r => r.DeleteGroupAnswersAsync(It.IsAny<IEnumerable<GroupAnswer>>())).Returns(Task.CompletedTask);
            _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.CreateQuestionsAsync(request);

            Assert.True(result.IsSuccess);
            _repo.Verify(r => r.DeleteGroupAnswersAsync(It.IsAny<IEnumerable<GroupAnswer>>()), Times.Once);
        }

        [Fact(DisplayName = "UpdateQuestionAsync - UTCID-FillBlank-ExistingGroups - Group existing với DependsOnGroupId được set")]
        public async Task Update_FillBlank_ExistingGroupsAndDepends()
        {
            var existingGroup = new GroupAnswer { GroupAnswerId = 50, Name = "OldName" };
            var existingAnswer = new QuestionAnswer
            {
                QuestionAnswerId = 10, Content = "placeholder[1]",
                GroupAnswer = existingGroup, GroupAnswerId = 50,
                BlankInputs = new List<BlankInput>()
            };
            var existing = new Question
            {
                QuestionId = 1, CreatedByUserId = 1, Status = QuestionStatus.Draft,
                QuestionType = QuestionType.FillBlank,
                QuestionAnswers = new List<QuestionAnswer> { existingAnswer }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetQuestionWithAnswersAsync(1)).ReturnsAsync(existing);
            _repo.Setup(r => r.IsQuestionUsedAsync(1)).ReturnsAsync(false);
            _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
            _repo.Setup(r => r.DeleteGroupAnswersAsync(It.IsAny<IEnumerable<GroupAnswer>>())).Returns(Task.CompletedTask);

            var dto = new QuestionDto
            {
                QuestionType = QuestionType.FillBlank, ChapterId = 1, Difficulty = 1,
                Status = QuestionStatus.Draft,
                Answers = new List<AnswerDto>
                {
                    new() { AnswerId = 10, Content = "placeholder[1]", BlankIndex = 1 }
                },
                BlankGroups = new List<GroupAnswerDto>
                {
                    new() { GroupAnswerId = 50, Name = "NewName", BlankIndices = new List<int> { 1 }, DependsOnGroupId = 99 }
                }
            };

            var result = await _service.UpdateQuestionAsync(1, dto);

            Assert.True(result.IsSuccess);
            Assert.Equal("NewName", existingGroup.Name);
            Assert.Equal(99, existingGroup.DependsOnGroupId);
        }

        [Fact(DisplayName = "CreateQuestionsAsync - UTCID-FillBlankWithDependsOnIndex - DependsOnGroupIndex")]
        public async Task Create_FillBlank_DependsOnIndex()
        {
            var request = new List<QuestionDto>
            {
                new()
                {
                    QuestionType = QuestionType.FillBlank, ChapterId = 1, Difficulty = 1,
                    Stem = "Q", QuestionPurpose = 1,
                    Answers = new List<AnswerDto>
                    {
                        new() { Content = "placeholder[1]" },
                        new() { Content = "placeholder[2]" }
                    },
                    BlankGroups = new List<GroupAnswerDto>
                    {
                        new() { Name = "G1", BlankIndices = new List<int> { 1 } },
                        new() { Name = "G2", BlankIndices = new List<int> { 2 }, DependsOnGroupIndex = 0 }
                    }
                }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.CreateQuestionsAsync(It.IsAny<List<Question>>()))
                .ReturnsAsync(new List<Question>
                {
                    new()
                    {
                        QuestionId = 1, QuestionType = QuestionType.FillBlank,
                        QuestionAnswers = new List<QuestionAnswer>
                        {
                            new() { Content = "placeholder[1]", BlankInputs = new List<BlankInput>() },
                            new() { Content = "placeholder[2]", BlankInputs = new List<BlankInput>() }
                        }
                    }
                });
            _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.CreateQuestionsAsync(request);

            Assert.True(result.IsSuccess);
        }

        // ---------- UpdateQuestionStatusAsync ----------

        [Fact(DisplayName = "UpdateQuestionStatusAsync - UTCID-WrongOwner - Sai owner -> không update")]
        public async Task UpdateStatus_WrongOwner_ShouldNotUpdate()
        {
            var questions = new List<Question>
            {
                new() { QuestionId = 1, CreatedByUserId = 2, Status = QuestionStatus.Draft }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetQuestionsByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(questions);

            var result = await _service.UpdateQuestionStatusAsync(new List<int> { 1 }, QuestionStatus.Active);

            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.Value);
            _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        // ---------- DeleteQuestionAsync ----------

        [Fact(DisplayName = "DeleteQuestionAsync - UTCID-WrongOwner - Sai owner -> NotFound")]
        public async Task Delete_WrongOwner_ShouldReturnNotFound()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetQuestionWithAnswersAsync(1))
                .ReturnsAsync(new Question { QuestionId = 1, CreatedByUserId = 2 });

            var result = await _service.DeleteQuestionAsync(1);

            Assert.Equal(QuestionErrors.NotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "DeleteQuestionAsync - UTCID-InvalidStatus - Inprogress -> InvalidDeleteStatus")]
        public async Task Delete_StatusNotDraftActive_ShouldReturnInvalidDeleteStatus()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetQuestionWithAnswersAsync(1))
                .ReturnsAsync(new Question { QuestionId = 1, CreatedByUserId = 1, Status = QuestionStatus.Archive });

            var result = await _service.DeleteQuestionAsync(1);

            Assert.Equal(QuestionErrors.InvalidDeleteStatus.Code, result.Error.Code);
        }

        [Fact(DisplayName = "DeleteQuestionAsync - UTCID-Used - Đã dùng -> InUse")]
        public async Task Delete_Used_ShouldReturnInUse()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetQuestionWithAnswersAsync(1))
                .ReturnsAsync(new Question { QuestionId = 1, CreatedByUserId = 1, Status = QuestionStatus.Active });
            _repo.Setup(r => r.IsQuestionUsedAsync(1)).ReturnsAsync(true);

            var result = await _service.DeleteQuestionAsync(1);

            Assert.Equal(QuestionErrors.InUse.Code, result.Error.Code);
        }
    }
}
