using Backend.Common;
using Backend.Common.Errors;
using Backend.Constants;
using Backend.DTOs.ExamBlueprint;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Backend.Services.Interfaces;
using Moq;
using Xunit;

namespace Backend_UnitTest.ExamBlueprintUnitTest
{
    public class ExamBlueprintServiceTests
    {
        private readonly Mock<IExamBlueprintRepository> _mockRepo;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly ExamBlueprintService _service;

        public ExamBlueprintServiceTests()
        {
            _mockRepo = new Mock<IExamBlueprintRepository>(MockBehavior.Strict);
            _mockCurrentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
            _service = new ExamBlueprintService(_mockRepo.Object, _mockCurrentUser.Object, TimeProvider.System);
        }

        [Fact(DisplayName = "GetSubjectsAsync - UTCID01 - Lấy danh sách môn")]
        public async Task GetSubjectsAsync_UTCID01_ValidRequest_ShouldReturnSubjects()
        {
            var subjects = new List<SubjectOptionDto> { new SubjectOptionDto { SubjectId = 1, Name = "Math" } };
            _mockRepo.Setup(r => r.GetSubjectsAsync()).ReturnsAsync(subjects);

            var result = await _service.GetSubjectsAsync();

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "GetChaptersBySubjectAsync - UTCID01 - Môn không tồn tại → SubjectNotFound")]
        public async Task GetChaptersBySubjectAsync_UTCID01_SubjectNotFound_ShouldReturnSubjectNotFound()
        {
            _mockRepo.Setup(r => r.SubjectExistsAsync(999)).ReturnsAsync(false);

            var result = await _service.GetChaptersBySubjectAsync(999);

            Assert.True(result.IsFailure);
            Assert.Equal(ExamBlueprintErrors.SubjectNotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "GetChaptersBySubjectAsync - UTCID02 - Lấy chương theo môn")]
        public async Task GetChaptersBySubjectAsync_UTCID02_ValidSubject_ShouldReturnChapters()
        {
            var chapters = new List<ChapterOptionDto> { new ChapterOptionDto { ChapterId = 1, Name = "Chapter 1" } };
            _mockRepo.Setup(r => r.SubjectExistsAsync(1)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(1)).ReturnsAsync(chapters);

            var result = await _service.GetChaptersBySubjectAsync(1);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "GetBlueprintsAsync - UTCID01 - Lấy danh sách ma trận")]
        public async Task GetBlueprintsAsync_UTCID01_ValidQuery_ShouldReturnBlueprintList()
        {
            var userId = 1;
            var query = new BlueprintListQueryDto { Page = 1, PageSize = 10 };
            var items = new List<BlueprintListItemDto> { new BlueprintListItemDto { ExamBlueprintId = 1 } };
            _mockCurrentUser.Setup(u => u.UserId).Returns(userId);
            _mockRepo.Setup(r => r.GetBlueprintsAsync(It.IsAny<BlueprintListQueryDto>(), userId)).ReturnsAsync((items, 1));

            var result = await _service.GetBlueprintsAsync(query);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value.Items);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "GetBlueprintDetailAsync - UTCID01 - Ma trận không tồn tại → NotFound")]
        public async Task GetBlueprintDetailAsync_UTCID01_BlueprintNotFound_ShouldReturnNotFound()
        {
            var userId = 1;
            _mockCurrentUser.Setup(u => u.UserId).Returns(userId);
            _mockRepo.Setup(r => r.GetBlueprintDetailAsync(999, userId)).ReturnsAsync((BlueprintDetailDto?)null);

            var result = await _service.GetBlueprintDetailAsync(999);

            Assert.True(result.IsFailure);
            Assert.Equal(ExamBlueprintErrors.NotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "GetBlueprintDetailAsync - UTCID02 - Lấy chi tiết ma trận")]
        public async Task GetBlueprintDetailAsync_UTCID02_ValidBlueprint_ShouldReturnDetail()
        {
            var userId = 1;
            var detail = new BlueprintDetailDto { ExamBlueprintId = 1, Name = "Blueprint 1" };
            _mockCurrentUser.Setup(u => u.UserId).Returns(userId);
            _mockRepo.Setup(r => r.GetBlueprintDetailAsync(1, userId)).ReturnsAsync(detail);

            var result = await _service.GetBlueprintDetailAsync(1);

            Assert.True(result.IsSuccess);
            Assert.Equal("Blueprint 1", result.Value.Name);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "CreateBlueprintAsync - UTCID01 - Môn không tồn tại → SubjectNotFound")]
        public async Task CreateBlueprintAsync_UTCID01_SubjectNotFound_ShouldReturnSubjectNotFound()
        {
            var userId = 1;
            var request = new CreateExamBlueprintRequest { SubjectId = 999, Name = "Test" };
            _mockCurrentUser.Setup(u => u.UserId).Returns(userId);
            _mockRepo.Setup(r => r.SubjectExistsAsync(999)).ReturnsAsync(false);

            var result = await _service.CreateBlueprintAsync(request);

            Assert.True(result.IsFailure);
            Assert.Equal(ExamBlueprintErrors.SubjectNotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "CreateBlueprintAsync - UTCID02 - Tạo ma trận thành công")]
        public async Task CreateBlueprintAsync_UTCID02_ValidRequest_ShouldSuccessAndCreateBlueprint()
        {
            var userId = 1;
            var request = new CreateExamBlueprintRequest
            {
                SubjectId = 1,
                Name = "Blueprint 1",
                TargetStatus = ExamBlueprintStatus.Draft,
                TargetTotalQuestions = 0,
                Rows = new List<CreateExamBlueprintRowDto>()
            };
            var created = new ExamBlueprint { ExamBlueprintId = 1, Status = ExamBlueprintStatus.Draft };
            _mockCurrentUser.Setup(u => u.UserId).Returns(userId);
            _mockRepo.Setup(r => r.SubjectExistsAsync(1)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(1)).ReturnsAsync(new List<ChapterOptionDto>());
            _mockRepo.Setup(r => r.CreateBlueprintAsync(It.IsAny<ExamBlueprint>(), It.IsAny<List<ExamBlueprintChapter>>())).ReturnsAsync(created);

            var result = await _service.CreateBlueprintAsync(request);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value.ExamBlueprintId);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "UpdateBlueprintAsync - UTCID01 - Ma trận không tồn tại → NotFound")]
        public async Task UpdateBlueprintAsync_UTCID01_BlueprintNotFound_ShouldReturnNotFound()
        {
            var userId = 1;
            var request = new CreateExamBlueprintRequest { SubjectId = 1, Name = "Updated" };
            _mockCurrentUser.Setup(u => u.UserId).Returns(userId);
            _mockRepo.Setup(r => r.SubjectExistsAsync(1)).ReturnsAsync(true);
            _mockRepo.Setup(r => r.GetChaptersBySubjectAsync(1)).ReturnsAsync(new List<ChapterOptionDto>());
            _mockRepo.Setup(r => r.GetBlueprintDetailAsync(999, userId)).ReturnsAsync((BlueprintDetailDto?)null);

            var result = await _service.UpdateBlueprintAsync(999, request);

            Assert.True(result.IsFailure);
            Assert.Equal(ExamBlueprintErrors.NotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "UpdateBlueprintStatusAsync - UTCID01 - Status không phải Archived → InvalidUpdateStatus")]
        public async Task UpdateBlueprintStatusAsync_UTCID01_InvalidStatus_ShouldReturnInvalidUpdateStatus()
        {
            var userId = 1;
            _mockCurrentUser.Setup(u => u.UserId).Returns(userId);

            var result = await _service.UpdateBlueprintStatusAsync(new List<int> { 1 }, ExamBlueprintStatus.Active);

            Assert.True(result.IsFailure);
            Assert.Equal(ExamBlueprintErrors.InvalidUpdateStatus.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "UpdateBlueprintStatusAsync - UTCID02 - Cập nhật thành Archived")]
        public async Task UpdateBlueprintStatusAsync_UTCID02_ValidUpdate_ShouldReturnCount()
        {
            var userId = 1;
            _mockCurrentUser.Setup(u => u.UserId).Returns(userId);
            _mockRepo.Setup(r => r.UpdateBlueprintStatusAsync(new List<int> { 1 }, userId, ExamBlueprintStatus.Archived)).ReturnsAsync(1);

            var result = await _service.UpdateBlueprintStatusAsync(new List<int> { 1 }, ExamBlueprintStatus.Archived);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "DeleteBlueprintAsync - UTCID01 - Ma trận không tồn tại → NotFound")]
        public async Task DeleteBlueprintAsync_UTCID01_BlueprintNotFound_ShouldReturnNotFound()
        {
            var userId = 1;
            _mockCurrentUser.Setup(u => u.UserId).Returns(userId);
            _mockRepo.Setup(r => r.GetBlueprintDetailAsync(999, userId)).ReturnsAsync((BlueprintDetailDto?)null);

            var result = await _service.DeleteBlueprintAsync(999);

            Assert.True(result.IsFailure);
            Assert.Equal(ExamBlueprintErrors.NotFound.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "DeleteBlueprintAsync - UTCID02 - Status không phải Draft/Active → CannotDelete")]
        public async Task DeleteBlueprintAsync_UTCID02_InvalidStatus_ShouldReturnCannotDelete()
        {
            var userId = 1;
            var detail = new BlueprintDetailDto { ExamBlueprintId = 1, Status = ExamBlueprintStatus.Archived };
            _mockCurrentUser.Setup(u => u.UserId).Returns(userId);
            _mockRepo.Setup(r => r.GetBlueprintDetailAsync(1, userId)).ReturnsAsync(detail);

            var result = await _service.DeleteBlueprintAsync(1);

            Assert.True(result.IsFailure);
            Assert.Equal(ExamBlueprintErrors.CannotDelete.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "DeleteBlueprintAsync - UTCID03 - Active + đang được sử dụng → InUse")]
        public async Task DeleteBlueprintAsync_UTCID03_ActiveAndInUse_ShouldReturnInUse()
        {
            var userId = 1;
            var detail = new BlueprintDetailDto { ExamBlueprintId = 1, Status = ExamBlueprintStatus.Active };
            _mockCurrentUser.Setup(u => u.UserId).Returns(userId);
            _mockRepo.Setup(r => r.GetBlueprintDetailAsync(1, userId)).ReturnsAsync(detail);
            _mockRepo.Setup(r => r.IsBlueprintUsedAsync(1)).ReturnsAsync(true);

            var result = await _service.DeleteBlueprintAsync(1);

            Assert.True(result.IsFailure);
            Assert.Equal(ExamBlueprintErrors.InUse.Code, result.Error.Code);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "DeleteBlueprintAsync - UTCID04 - Xóa ma trận thành công")]
        public async Task DeleteBlueprintAsync_UTCID04_ValidDelete_ShouldSuccessAndDelete()
        {
            var userId = 1;
            var detail = new BlueprintDetailDto { ExamBlueprintId = 1, Status = ExamBlueprintStatus.Draft };
            _mockCurrentUser.Setup(u => u.UserId).Returns(userId);
            _mockRepo.Setup(r => r.GetBlueprintDetailAsync(1, userId)).ReturnsAsync(detail);
            _mockRepo.Setup(r => r.DeleteBlueprintAsync(1, userId)).Returns(Task.CompletedTask);

            var result = await _service.DeleteBlueprintAsync(1);

            Assert.True(result.IsSuccess);
            _mockRepo.Verify(r => r.DeleteBlueprintAsync(1, userId), Times.Once);
            _mockRepo.VerifyAll();
        }
    }
}
