using Backend.DTOs.Course;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using Moq;
using Xunit;

namespace Backend_UnitTest.ChapterUnitTest
{
    public class ChapterServiceTests
    {
        private readonly Mock<IChapterRepository> _mockRepo;
        private readonly ChapterService _service;

        public ChapterServiceTests()
        {
            _mockRepo = new Mock<IChapterRepository>(MockBehavior.Strict);
            _service = new ChapterService(_mockRepo.Object);
        }

        [Fact(DisplayName = "GetAllAsync - UTCID01 - Lấy tất cả chương")]
        public async Task GetAllAsync_UTCID01_ShouldReturnAllChapters()
        {
            var chapters = new List<ChapterDTO>
            {
                new ChapterDTO { ChapterId = 1, Name = "Chương 1", SubjectId = 1 },
                new ChapterDTO { ChapterId = 2, Name = "Chương 2", SubjectId = 1 }
            };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(chapters);

            var result = await _service.GetAllAsync();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(chapters[0].ChapterId, result[0].ChapterId);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "GetAllAsync - UTCID02 - Không có chương")]
        public async Task GetAllAsync_UTCID02_EmptyList_ShouldReturnEmptyList()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ChapterDTO>());

            var result = await _service.GetAllAsync();

            Assert.NotNull(result);
            Assert.Empty(result);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "GetByIdAsync - UTCID01 - Chương tồn tại")]
        public async Task GetByIdAsync_UTCID01_ChapterExists_ShouldReturnChapter()
        {
            var chapter = new ChapterDTO { ChapterId = 1, Name = "Chương 1", SubjectId = 1 };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(chapter);

            var result = await _service.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(chapter.ChapterId, result.ChapterId);
            Assert.Equal(chapter.Name, result.Name);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "GetByIdAsync - UTCID02 - Chương không tồn tại")]
        public async Task GetByIdAsync_UTCID02_ChapterNotFound_ShouldReturnNull()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((ChapterDTO?)null);

            var result = await _service.GetByIdAsync(999);

            Assert.Null(result);
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "GetBySubjectIdAsync - UTCID01 - Lấy chương theo môn")]
        public async Task GetBySubjectIdAsync_UTCID01_ValidSubject_ShouldReturnChapters()
        {
            int subjectId = 1;
            var chapters = new List<ChapterDTO>
            {
                new ChapterDTO { ChapterId = 1, Name = "Chương 1", SubjectId = subjectId },
                new ChapterDTO { ChapterId = 2, Name = "Chương 2", SubjectId = subjectId }
            };
            _mockRepo.Setup(r => r.GetBySubjectIdAsync(subjectId)).ReturnsAsync(chapters);

            var result = await _service.GetBySubjectIdAsync(subjectId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, ch => Assert.Equal(subjectId, ch.SubjectId));
            _mockRepo.VerifyAll();
        }

        [Fact(DisplayName = "GetBySubjectIdAsync - UTCID02 - Môn không có chương")]
        public async Task GetBySubjectIdAsync_UTCID02_NoChapters_ShouldReturnEmptyList()
        {
            _mockRepo.Setup(r => r.GetBySubjectIdAsync(999)).ReturnsAsync(new List<ChapterDTO>());

            var result = await _service.GetBySubjectIdAsync(999);

            Assert.NotNull(result);
            Assert.Empty(result);
            _mockRepo.VerifyAll();
        }
    }
}
