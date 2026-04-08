using Backend.DTOs.ExamBlueprint;
using Backend.UnitTest;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackEnd_UnitTest.CourseUnitTest
{
    public class GetSubjectAsyncTest : CourseTestBase
    {
        [Fact]
        public async Task GetSubjects_ShouldReturnList_WhenDataExists()
        {
            // 1. Arrange: Phải Mock đúng hàm mà GetSubjectsAsync gọi (trong ảnh là GetSubjectsAsync của repo)
            var mockData = new List<SubjectOptionDto>
        {
            new SubjectOptionDto { SubjectId = 1, Name = "Math" },
            new SubjectOptionDto { SubjectId = 2, Name = "Physics" }
        };

            _mockRepo.Setup(r => r.GetSubjectsAsync())
             .Returns(Task.FromResult(mockData));

            // 2. Act: Gọi ĐÚNG hàm cần đo coverage
            var result = await _courseService.GetSubjectsAsync();

            // 3. Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            _mockRepo.Verify(r => r.GetSubjectsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetSubjects_ShouldThrow_WhenRepoFails()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetSubjectsAsync()).ThrowsAsync(new Exception("DB Error"));

            // Act & Assert: Gọi ĐÚNG hàm GetSubjectsAsync
            await Assert.ThrowsAsync<Exception>(() => _courseService.GetSubjectsAsync());
        }
    }
}
