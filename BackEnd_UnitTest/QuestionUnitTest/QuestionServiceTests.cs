using Backend.Common;
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
    public class QuestionServiceTests
    {
        private readonly Mock<IQuestionRepository> _mockRepo;
        private readonly Mock<ILogger<QuestionService>> _mockLogger;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly QuestionService _service;

        public QuestionServiceTests()
        {
            _mockRepo = new Mock<IQuestionRepository>(MockBehavior.Strict);
            _mockLogger = new Mock<ILogger<QuestionService>>();
            _mockCurrentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
            _service = new QuestionService(_mockRepo.Object, _mockLogger.Object, _mockCurrentUser.Object, TimeProvider.System);
        }

        [Fact(DisplayName = "CreateQuestionsAsync - UTCID01 - Danh sách rỗng → EmptyList")]
        public async Task CreateQuestionsAsync_UTCID01_EmptyList_ShouldReturnEmptyListError()
        {
            var request = new List<QuestionDto>();
            _mockCurrentUser.Setup(u => u.UserId).Returns(1);

            var result = await _service.CreateQuestionsAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal(QuestionErrors.EmptyList.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateQuestionsAsync - UTCID02 - Tạo câu hỏi thành công")]
        public async Task CreateQuestionsAsync_UTCID02_ValidQuestions_ShouldSuccessAndCreateQuestions()
        {
            var userId = 1;
            var request = new List<QuestionDto>
            {
                new QuestionDto { QuestionType = "MCQ", ChapterId = 1, Difficulty = 1, Stem = "Q1" }
            };
            var created = new List<Question>
            {
                new Question { QuestionId = 1, QuestionType = "MCQ", CreatedByUserId = userId }
            };

            _mockCurrentUser.Setup(u => u.UserId).Returns(userId);
            _mockRepo.Setup(r => r.CreateQuestionsAsync(It.IsAny<List<Question>>())).ReturnsAsync(created);
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.CreateQuestionsAsync(request);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact(DisplayName = "UpdateQuestionAsync - UTCID01 - Câu hỏi không tồn tại → NotFound")]
        public async Task UpdateQuestionAsync_UTCID01_QuestionNotFound_ShouldReturnNotFound()
        {
            var userId = 1;
            var request = new QuestionDto { Stem = "Updated" };
            _mockCurrentUser.Setup(u => u.UserId).Returns(userId);
            _mockRepo.Setup(r => r.GetQuestionWithAnswersAsync(1)).ReturnsAsync((Question?)null);

            var result = await _service.UpdateQuestionAsync(1, request);

            Assert.True(result.IsFailure);
            Assert.Equal(QuestionErrors.NotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "GetQuestionByIdAsync - UTCID01 - Câu hỏi không tồn tại → NotFound")]
        public async Task GetQuestionByIdAsync_UTCID01_QuestionNotFound_ShouldReturnNotFound()
        {
            var userId = 1;
            _mockCurrentUser.Setup(u => u.UserId).Returns(userId);
            _mockRepo.Setup(r => r.GetQuestionWithAnswersAsync(999)).ReturnsAsync((Question?)null);

            var result = await _service.GetQuestionByIdAsync(999);

            Assert.True(result.IsFailure);
            Assert.Equal(QuestionErrors.NotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "UpdateQuestionStatusAsync - UTCID01 - Cập nhật trạng thái")]
        public async Task UpdateQuestionStatusAsync_UTCID01_ValidUpdate_ShouldReturnUpdatedCount()
        {
            var userId = 1;
            var questions = new List<Question>
            {
                new Question { QuestionId = 1, CreatedByUserId = userId, Status = "Draft" },
                new Question { QuestionId = 2, CreatedByUserId = userId, Status = "Draft" }
            };
            _mockCurrentUser.Setup(u => u.UserId).Returns(userId);
            _mockRepo.Setup(r => r.GetQuestionsByIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(questions);
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.UpdateQuestionStatusAsync(new List<int> { 1, 2 }, "Active");

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value);
        }

        [Fact(DisplayName = "DeleteQuestionAsync - UTCID01 - Câu hỏi không tồn tại → NotFound")]
        public async Task DeleteQuestionAsync_UTCID01_QuestionNotFound_ShouldReturnNotFound()
        {
            var userId = 1;
            _mockCurrentUser.Setup(u => u.UserId).Returns(userId);
            _mockRepo.Setup(r => r.GetQuestionWithAnswersAsync(999)).ReturnsAsync((Question?)null);

            var result = await _service.DeleteQuestionAsync(999);

            Assert.True(result.IsFailure);
            Assert.Equal(QuestionErrors.NotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "DeleteQuestionAsync - UTCID02 - Xóa câu hỏi thành công")]
        public async Task DeleteQuestionAsync_UTCID02_ValidDelete_ShouldSuccessAndDelete()
        {
            var userId = 1;
            var question = new Question { QuestionId = 1, CreatedByUserId = userId, Status = "Draft" };
            _mockCurrentUser.Setup(u => u.UserId).Returns(userId);
            _mockRepo.Setup(r => r.GetQuestionWithAnswersAsync(1)).ReturnsAsync(question);
            _mockRepo.Setup(r => r.IsQuestionUsedAsync(1)).ReturnsAsync(false);
            _mockRepo.Setup(r => r.DeleteQuestionAsync(question)).Returns(Task.CompletedTask);
            _mockRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.DeleteQuestionAsync(1);

            Assert.True(result.IsSuccess);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact(DisplayName = "GetQuestionMetadataAsync - UTCID01 - Lấy metadata")]
        public async Task GetQuestionMetadataAsync_UTCID01_ValidRequest_ShouldReturnMetadata()
        {
            var inputTypes = new List<InputType> { new InputType { InputTypeId = 1, Name = "Text" } };
            var subjects = new List<Subject> { new Subject { SubjectId = 1, Name = "Math", Code = "M" } };

            _mockRepo.Setup(r => r.GetInputTypesAsync()).ReturnsAsync(inputTypes);
            _mockRepo.Setup(r => r.GetSubjectsWithChaptersAsync()).ReturnsAsync(subjects);

            var result = await _service.GetQuestionMetadataAsync();

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value.InputTypes);
            Assert.Single(result.Value.Subjects);
        }
    }
}
