using System.Collections.Generic;
using System.Threading.Tasks;
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
    public class ExamBlueprintServiceMissingCoverageTests
    {
        private readonly Mock<IExamBlueprintRepository> _repo = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly ExamBlueprintService _service;

        public ExamBlueprintServiceMissingCoverageTests()
        {
            _service = new ExamBlueprintService(_repo.Object, _currentUser.Object, TimeProvider.System);
        }

        private List<ChapterOptionDto> BuildChapters(params (int id, int diff, int available)[] entries)
        {
            var grouped = new Dictionary<int, ChapterOptionDto>();
            foreach (var (id, diff, available) in entries)
            {
                if (!grouped.TryGetValue(id, out var chapter))
                {
                    chapter = new ChapterOptionDto { ChapterId = id, Name = $"Chap{id}" };
                    grouped[id] = chapter;
                }
                chapter.AvailabilityByDifficulty.Add(new ChapterAvailabilityDto { Difficulty = diff, AvailableQuestions = available });
            }
            return grouped.Values.ToList();
        }

        // ----- Validate paths -----

        [Fact(DisplayName = "CreateBlueprintAsync - UTCID-Validation - Active + Empty rows -> EmptyRows")]
        public async Task Create_Active_EmptyRows_ShouldReturnEmptyRows()
        {
            var request = new CreateExamBlueprintRequest
            {
                SubjectId = 1, Name = "X",
                TargetStatus = ExamBlueprintStatus.Active,
                TargetTotalQuestions = 0,
                Rows = new List<CreateExamBlueprintRowDto>()
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.SubjectExistsAsync(1)).ReturnsAsync(true);
            _repo.Setup(r => r.GetChaptersBySubjectAsync(1)).ReturnsAsync(new List<ChapterOptionDto>());

            var result = await _service.CreateBlueprintAsync(request);

            Assert.Equal(ExamBlueprintErrors.EmptyRows.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateBlueprintAsync - UTCID-Mismatch - Active + Tổng số khác -> TargetTotalMismatch")]
        public async Task Create_Active_TotalMismatch_ShouldReturnMismatch()
        {
            var request = new CreateExamBlueprintRequest
            {
                SubjectId = 1, Name = "X",
                TargetStatus = ExamBlueprintStatus.Active,
                TargetTotalQuestions = 5,
                Rows = new List<CreateExamBlueprintRowDto>
                {
                    new() { ChapterId = 1, Difficulty = 1, TotalQuestions = 3 }
                }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.SubjectExistsAsync(1)).ReturnsAsync(true);
            _repo.Setup(r => r.GetChaptersBySubjectAsync(1)).ReturnsAsync(BuildChapters((1, 1, 100)));

            var result = await _service.CreateBlueprintAsync(request);

            Assert.Equal(ExamBlueprintErrors.TargetTotalMismatch.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateBlueprintAsync - UTCID-Insufficient - Active + vượt ngân hàng câu hỏi -> InsufficientQuestionBank")]
        public async Task Create_Active_OverBank_ShouldReturnInsufficient()
        {
            var request = new CreateExamBlueprintRequest
            {
                SubjectId = 1, Name = "X",
                TargetStatus = ExamBlueprintStatus.Active,
                TargetTotalQuestions = 10,
                Rows = new List<CreateExamBlueprintRowDto>
                {
                    new() { ChapterId = 1, Difficulty = 1, TotalQuestions = 10 }
                }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.SubjectExistsAsync(1)).ReturnsAsync(true);
            _repo.Setup(r => r.GetChaptersBySubjectAsync(1)).ReturnsAsync(BuildChapters((1, 1, 5)));

            var result = await _service.CreateBlueprintAsync(request);

            Assert.Equal(ExamBlueprintErrors.InsufficientQuestionBank.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateBlueprintAsync - UTCID-Duplicate - Hai dòng cùng Chapter+Difficulty -> DuplicateRow")]
        public async Task Create_DuplicateRow_ShouldReturnDuplicate()
        {
            var request = new CreateExamBlueprintRequest
            {
                SubjectId = 1, Name = "X",
                TargetStatus = ExamBlueprintStatus.Draft,
                TargetTotalQuestions = 4,
                Rows = new List<CreateExamBlueprintRowDto>
                {
                    new() { ChapterId = 1, Difficulty = 1, TotalQuestions = 2 },
                    new() { ChapterId = 1, Difficulty = 1, TotalQuestions = 2 }
                }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.SubjectExistsAsync(1)).ReturnsAsync(true);
            _repo.Setup(r => r.GetChaptersBySubjectAsync(1)).ReturnsAsync(BuildChapters((1, 1, 100)));

            var result = await _service.CreateBlueprintAsync(request);

            Assert.Equal(ExamBlueprintErrors.DuplicateRow.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateBlueprintAsync - UTCID-UnknownChapter - Chapter không thuộc Subject -> ValidationFailed")]
        public async Task Create_UnknownChapter_ShouldReturnValidationFailed()
        {
            var request = new CreateExamBlueprintRequest
            {
                SubjectId = 1, Name = "X",
                TargetStatus = ExamBlueprintStatus.Draft,
                TargetTotalQuestions = 1,
                Rows = new List<CreateExamBlueprintRowDto>
                {
                    new() { ChapterId = 999, Difficulty = 1, TotalQuestions = 1 }
                }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.SubjectExistsAsync(1)).ReturnsAsync(true);
            _repo.Setup(r => r.GetChaptersBySubjectAsync(1)).ReturnsAsync(BuildChapters((1, 1, 5)));

            var result = await _service.CreateBlueprintAsync(request);

            Assert.Equal(ExamBlueprintErrors.ValidationFailed.Code, result.Error.Code);
        }

        [Fact(DisplayName = "CreateBlueprintAsync - UTCID-DraftWarn - Draft + tổng không khớp + vượt bank -> success với warnings")]
        public async Task Create_Draft_WithWarnings()
        {
            var request = new CreateExamBlueprintRequest
            {
                SubjectId = 1, Name = "  Test  ", Description = "  desc  ",
                TargetStatus = ExamBlueprintStatus.Draft,
                TargetTotalQuestions = 99,
                Rows = new List<CreateExamBlueprintRowDto>
                {
                    new() { ChapterId = 1, Difficulty = 1, TotalQuestions = 10 }
                }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.SubjectExistsAsync(1)).ReturnsAsync(true);
            _repo.Setup(r => r.GetChaptersBySubjectAsync(1)).ReturnsAsync(BuildChapters((1, 1, 5)));
            _repo.Setup(r => r.CreateBlueprintAsync(It.IsAny<ExamBlueprint>(), It.IsAny<List<ExamBlueprintChapter>>()))
                .ReturnsAsync(new ExamBlueprint { ExamBlueprintId = 100, Status = ExamBlueprintStatus.Draft });

            var result = await _service.CreateBlueprintAsync(request);

            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.Value.Warnings);
            Assert.Contains(result.Value.Warnings, w => w.Code == "TARGET_TOTAL_MISMATCH");
            Assert.Contains(result.Value.Warnings, w => w.Code == "INSUFFICIENT_QUESTION_BANK");
        }

        [Fact(DisplayName = "CreateBlueprintAsync - UTCID-AllDifficulties - All difficulty labels mapped")]
        public async Task Create_AllDifficultyLabels_ShouldNotThrow()
        {
            var request = new CreateExamBlueprintRequest
            {
                SubjectId = 1, Name = "X",
                TargetStatus = ExamBlueprintStatus.Draft,
                TargetTotalQuestions = 12,
                Rows = new List<CreateExamBlueprintRowDto>
                {
                    new() { ChapterId = 1, Difficulty = 1, TotalQuestions = 99 },
                    new() { ChapterId = 1, Difficulty = 2, TotalQuestions = 99 },
                    new() { ChapterId = 1, Difficulty = 3, TotalQuestions = 99 },
                    new() { ChapterId = 1, Difficulty = 4, TotalQuestions = 99 },
                    new() { ChapterId = 1, Difficulty = 9, TotalQuestions = 99 }
                }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.SubjectExistsAsync(1)).ReturnsAsync(true);
            _repo.Setup(r => r.GetChaptersBySubjectAsync(1))
                .ReturnsAsync(BuildChapters((1, 1, 1), (1, 2, 1), (1, 3, 1), (1, 4, 1), (1, 9, 1)));
            _repo.Setup(r => r.CreateBlueprintAsync(It.IsAny<ExamBlueprint>(), It.IsAny<List<ExamBlueprintChapter>>()))
                .ReturnsAsync(new ExamBlueprint { ExamBlueprintId = 100, Status = ExamBlueprintStatus.Draft });

            var result = await _service.CreateBlueprintAsync(request);

            Assert.True(result.IsSuccess);
        }

        // ----- UpdateBlueprintAsync paths -----

        [Fact(DisplayName = "UpdateBlueprintAsync - UTCID-CreateNewWhenInUse - Used -> Archive cũ + tạo bản mới")]
        public async Task Update_BlueprintUsed_ShouldArchiveAndCreate()
        {
            var request = new CreateExamBlueprintRequest
            {
                SubjectId = 1, Name = "Updated",
                TargetStatus = ExamBlueprintStatus.Active,
                TargetTotalQuestions = 1,
                Rows = new List<CreateExamBlueprintRowDto>
                {
                    new() { ChapterId = 1, Difficulty = 1, TotalQuestions = 1 }
                }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.SubjectExistsAsync(1)).ReturnsAsync(true);
            _repo.Setup(r => r.GetChaptersBySubjectAsync(1)).ReturnsAsync(BuildChapters((1, 1, 5)));
            _repo.Setup(r => r.GetBlueprintDetailAsync(10, 1))
                .ReturnsAsync(new BlueprintDetailDto { ExamBlueprintId = 10, Status = ExamBlueprintStatus.Active });
            _repo.Setup(r => r.IsBlueprintUsedAsync(10)).ReturnsAsync(true);
            _repo.Setup(r => r.UpdateBlueprintStatusAsync(It.IsAny<List<int>>(), 1, ExamBlueprintStatus.Archived)).ReturnsAsync(1);
            _repo.Setup(r => r.CreateBlueprintAsync(It.IsAny<ExamBlueprint>(), It.IsAny<List<ExamBlueprintChapter>>()))
                .ReturnsAsync(new ExamBlueprint { ExamBlueprintId = 11, Status = ExamBlueprintStatus.Active });

            var result = await _service.UpdateBlueprintAsync(10, request);

            Assert.True(result.IsSuccess);
            Assert.Equal(11, result.Value.ExamBlueprintId);
            _repo.Verify(r => r.UpdateBlueprintStatusAsync(It.IsAny<List<int>>(), 1, ExamBlueprintStatus.Archived), Times.Once);
        }

        [Fact(DisplayName = "UpdateBlueprintAsync - UTCID-Inprogress - Inprogress -> Archive cũ + tạo bản mới")]
        public async Task Update_Inprogress_ShouldArchiveAndCreate()
        {
            var request = new CreateExamBlueprintRequest
            {
                SubjectId = 1, Name = "X",
                TargetStatus = ExamBlueprintStatus.Draft,
                TargetTotalQuestions = 1,
                Rows = new List<CreateExamBlueprintRowDto>
                {
                    new() { ChapterId = 1, Difficulty = 1, TotalQuestions = 1 }
                }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.SubjectExistsAsync(1)).ReturnsAsync(true);
            _repo.Setup(r => r.GetChaptersBySubjectAsync(1)).ReturnsAsync(BuildChapters((1, 1, 5)));
            _repo.Setup(r => r.GetBlueprintDetailAsync(10, 1))
                .ReturnsAsync(new BlueprintDetailDto { ExamBlueprintId = 10, Status = ExamBlueprintStatus.Inprogress });
            _repo.Setup(r => r.IsBlueprintUsedAsync(10)).ReturnsAsync(false);
            _repo.Setup(r => r.UpdateBlueprintStatusAsync(It.IsAny<List<int>>(), 1, ExamBlueprintStatus.Archived)).ReturnsAsync(1);
            _repo.Setup(r => r.CreateBlueprintAsync(It.IsAny<ExamBlueprint>(), It.IsAny<List<ExamBlueprintChapter>>()))
                .ReturnsAsync(new ExamBlueprint { ExamBlueprintId = 11, Status = ExamBlueprintStatus.Draft });

            var result = await _service.UpdateBlueprintAsync(10, request);

            Assert.True(result.IsSuccess);
        }

        [Fact(DisplayName = "UpdateBlueprintAsync - UTCID-DraftUpdate - Draft + chưa dùng -> Update tại chỗ")]
        public async Task Update_Draft_NotUsed_ShouldCallUpdate()
        {
            var request = new CreateExamBlueprintRequest
            {
                SubjectId = 1, Name = "X", Description = "  d  ",
                TargetStatus = ExamBlueprintStatus.Draft,
                TargetTotalQuestions = 1,
                Rows = new List<CreateExamBlueprintRowDto>
                {
                    new() { ChapterId = 1, Difficulty = 1, TotalQuestions = 1 }
                }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.SubjectExistsAsync(1)).ReturnsAsync(true);
            _repo.Setup(r => r.GetChaptersBySubjectAsync(1)).ReturnsAsync(BuildChapters((1, 1, 5)));
            _repo.Setup(r => r.GetBlueprintDetailAsync(10, 1))
                .ReturnsAsync(new BlueprintDetailDto { ExamBlueprintId = 10, Status = ExamBlueprintStatus.Draft });
            _repo.Setup(r => r.IsBlueprintUsedAsync(10)).ReturnsAsync(false);
            _repo.Setup(r => r.UpdateBlueprintAsync(10, 1, It.IsAny<ExamBlueprint>(), It.IsAny<List<ExamBlueprintChapter>>()))
                .ReturnsAsync(new ExamBlueprint { ExamBlueprintId = 10, Status = ExamBlueprintStatus.Draft });

            var result = await _service.UpdateBlueprintAsync(10, request);

            Assert.True(result.IsSuccess);
            _repo.Verify(r => r.UpdateBlueprintAsync(10, 1, It.IsAny<ExamBlueprint>(), It.IsAny<List<ExamBlueprintChapter>>()), Times.Once);
        }

        [Fact(DisplayName = "UpdateBlueprintAsync - UTCID-UpdateRetNull - Update tại chỗ trả null -> NotFound")]
        public async Task Update_RepoReturnsNull_ShouldReturnNotFound()
        {
            var request = new CreateExamBlueprintRequest
            {
                SubjectId = 1, Name = "X",
                TargetStatus = ExamBlueprintStatus.Draft,
                TargetTotalQuestions = 1,
                Rows = new List<CreateExamBlueprintRowDto>
                {
                    new() { ChapterId = 1, Difficulty = 1, TotalQuestions = 1 }
                }
            };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.SubjectExistsAsync(1)).ReturnsAsync(true);
            _repo.Setup(r => r.GetChaptersBySubjectAsync(1)).ReturnsAsync(BuildChapters((1, 1, 5)));
            _repo.Setup(r => r.GetBlueprintDetailAsync(10, 1))
                .ReturnsAsync(new BlueprintDetailDto { ExamBlueprintId = 10, Status = ExamBlueprintStatus.Draft });
            _repo.Setup(r => r.IsBlueprintUsedAsync(10)).ReturnsAsync(false);
            _repo.Setup(r => r.UpdateBlueprintAsync(10, 1, It.IsAny<ExamBlueprint>(), It.IsAny<List<ExamBlueprintChapter>>()))
                .ReturnsAsync((ExamBlueprint?)null!);

            var result = await _service.UpdateBlueprintAsync(10, request);

            Assert.True(result.IsFailure);
            Assert.Equal(ExamBlueprintErrors.NotFound.Code, result.Error.Code);
        }

        // ----- Get paginated blueprints + paging clamp -----

        [Fact(DisplayName = "GetBlueprintsAsync - UTCID-PagingClamp - Page<1, PageSize<=0 -> default")]
        public async Task GetBlueprints_PagingClamp_ShouldClamp()
        {
            var query = new BlueprintListQueryDto { Page = 0, PageSize = 0 };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetBlueprintsAsync(query, 1)).ReturnsAsync((new List<BlueprintListItemDto>(), 0));

            var result = await _service.GetBlueprintsAsync(query);

            Assert.Equal(1, result.Value.Page);
            Assert.Equal(10, result.Value.PageSize);
        }

        [Fact(DisplayName = "GetBlueprintsAsync - UTCID-PagingHigh - PageSize>100 -> clamp 100")]
        public async Task GetBlueprints_PageSizeOver100_ShouldClamp()
        {
            var query = new BlueprintListQueryDto { Page = 1, PageSize = 500 };
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetBlueprintsAsync(query, 1)).ReturnsAsync((new List<BlueprintListItemDto>(), 25));

            var result = await _service.GetBlueprintsAsync(query);

            Assert.Equal(100, result.Value.PageSize);
            Assert.Equal(1, result.Value.TotalPages);
        }

        // ----- DeleteBlueprintAsync Active not used path -----

        [Fact(DisplayName = "DeleteBlueprintAsync - UTCID-ActiveNotUsed - Active + chưa dùng -> success")]
        public async Task Delete_Active_NotUsed_ShouldDelete()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.GetBlueprintDetailAsync(1, 1))
                .ReturnsAsync(new BlueprintDetailDto { Status = ExamBlueprintStatus.Active });
            _repo.Setup(r => r.IsBlueprintUsedAsync(1)).ReturnsAsync(false);
            _repo.Setup(r => r.DeleteBlueprintAsync(1, 1)).Returns(Task.CompletedTask);

            var result = await _service.DeleteBlueprintAsync(1);

            Assert.True(result.IsSuccess);
        }

        // ----- UpdateBlueprintStatusAsync corner cases -----

        [Fact(DisplayName = "UpdateBlueprintAsync - UTCID-ValidationFail - Subject không tồn tại -> SubjectNotFound (qua validate)")]
        public async Task Update_ValidationFails_SubjectNotFound()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);
            _repo.Setup(r => r.SubjectExistsAsync(999)).ReturnsAsync(false);

            var result = await _service.UpdateBlueprintAsync(10, new CreateExamBlueprintRequest
            {
                SubjectId = 999, Name = "X",
                TargetStatus = ExamBlueprintStatus.Draft,
                TargetTotalQuestions = 0,
                Rows = new List<CreateExamBlueprintRowDto>()
            });

            Assert.True(result.IsFailure);
            Assert.Equal(ExamBlueprintErrors.SubjectNotFound.Code, result.Error.Code);
        }

        [Fact(DisplayName = "UpdateBlueprintStatusAsync - UTCID-EmptyIds - Lọc id<=0 -> 0 result")]
        public async Task UpdateStatus_EmptyAfterFilter_ReturnsZero()
        {
            _currentUser.Setup(u => u.UserId).Returns(1);

            var result = await _service.UpdateBlueprintStatusAsync(new List<int> { 0, -1 }, ExamBlueprintStatus.Archived);

            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.Value);
        }
    }
}
