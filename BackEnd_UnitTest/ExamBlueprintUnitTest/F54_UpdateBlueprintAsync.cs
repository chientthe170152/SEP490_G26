using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Constants;
using Backend.DTOs.ExamBlueprint;
using Backend.Exceptions;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Implements;
using BackEnd_UnitTest._Shared;
using Moq;
using Xunit;

namespace BackEnd_UnitTest.ExamBlueprintUnitTest;

// F54 - UpdateBlueprintAsync
// Source: ExamBlueprintService.cs:272-335 + shared ValidateAndPrepareBlueprintAsync (337-416)
// Branches:
//   1. id <= 0 OR currentUserId <= 0                  -> Unauthorized
//   2. validation errors                              -> ValidationException
//   3. existing == null                               -> KeyNotFoundException
//   4. existing.Status == Inprogress                  -> archive+create new
//   5. existing.Status == Archived                    -> archive+create new
//   6. isUsed == true                                 -> archive+create new
//   7. else (Active or Draft + not used)              -> in-place update
//   8. UpdateBlueprintAsync returns null              -> KeyNotFoundException
//   9. SubjectExistsAsync false (in shared validate)  -> KeyNotFoundException
public class F54_UpdateBlueprintAsync_Tests
{
    private readonly Mock<IExamBlueprintRepository> _repoMock = new(MockBehavior.Strict);
    private readonly ExamBlueprintService _service;

    public F54_UpdateBlueprintAsync_Tests() => _service = new ExamBlueprintService(_repoMock.Object);

    private static List<ChapterOptionDto> StandardChapters() => new()
    {
        new() { ChapterId = 10, Name = "Chương 1", AvailabilityByDifficulty = new List<ChapterAvailabilityDto>
            {
                new() { Difficulty = 1, AvailableQuestions = 5 }
            }
        }
    };

    private static CreateExamBlueprintRequest ValidActive() => new()
    {
        Name = "BP",
        SubjectId = 1,
        TargetTotalQuestions = 5,
        TargetStatus = ExamBlueprintStatus.Active,
        Rows = new List<CreateExamBlueprintRowDto>
        {
            new() { ChapterId = 10, Difficulty = 1, TotalQuestions = 5 }
        }
    };

    private void SetupSubjectAndChapters()
    {
        _repoMock.Setup(r => r.SubjectExistsAsync(1)).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetChaptersBySubjectAsync(1)).ReturnsAsync(StandardChapters());
    }

    [Fact(DisplayName = "UpdateBlueprintAsync - UTCID01 - Active + chưa dùng + có Description -> in-place update")]
    [TestType("N")]
    public async Task UpdateBlueprintAsync_UTCID01_InPlace_ShouldSucceed()
    {
        SetupSubjectAndChapters();
        _repoMock.Setup(r => r.GetBlueprintDetailAsync(5, 100)).ReturnsAsync(new BlueprintDetailDto { ExamBlueprintId = 5, Status = ExamBlueprintStatus.Active });
        _repoMock.Setup(r => r.IsBlueprintUsedAsync(5)).ReturnsAsync(false);
        _repoMock.Setup(r => r.UpdateBlueprintAsync(5, 100, It.IsAny<ExamBlueprint>(), It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                 .ReturnsAsync(new ExamBlueprint { ExamBlueprintId = 5, Status = ExamBlueprintStatus.Active, Name = "X", TeacherId = 100, ConcurrencyStamp = Array.Empty<byte>(), UpdatedAtUtc = DateTime.UtcNow });
        var req = ValidActive();
        req.Description = "  Mô tả chi tiết  "; // non-null/non-whitespace -> covers ternary's Trim() branch on line 293

        var result = await _service.UpdateBlueprintAsync(5, 100, req);

        Assert.Equal(5, result.ExamBlueprintId);
        Assert.Equal(ExamBlueprintStatus.Active, result.Status);
    }

    [Fact(DisplayName = "UpdateBlueprintAsync - UTCID02 - Inprogress -> archive + tạo mới (clone)")]
    [TestType("N")]
    public async Task UpdateBlueprintAsync_UTCID02_Inprogress_ShouldClone()
    {
        SetupSubjectAndChapters();
        _repoMock.Setup(r => r.GetBlueprintDetailAsync(5, 100)).ReturnsAsync(new BlueprintDetailDto { ExamBlueprintId = 5, Status = ExamBlueprintStatus.Inprogress });
        _repoMock.Setup(r => r.IsBlueprintUsedAsync(5)).ReturnsAsync(false);
        _repoMock.Setup(r => r.UpdateBlueprintStatusAsync(It.Is<List<int>>(x => x.SequenceEqual(new[] { 5 })), 100, ExamBlueprintStatus.Archived)).ReturnsAsync(1);
        _repoMock.Setup(r => r.CreateBlueprintAsync(It.IsAny<ExamBlueprint>(), It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                 .ReturnsAsync((ExamBlueprint bp, IEnumerable<ExamBlueprintChapter> _) => { bp.ExamBlueprintId = 999; return bp; });

        var result = await _service.UpdateBlueprintAsync(5, 100, ValidActive());

        Assert.Equal(999, result.ExamBlueprintId);
    }

    [Fact(DisplayName = "UpdateBlueprintAsync - UTCID03 - Archived -> archive + tạo mới")]
    [TestType("N")]
    public async Task UpdateBlueprintAsync_UTCID03_Archived_ShouldClone()
    {
        SetupSubjectAndChapters();
        _repoMock.Setup(r => r.GetBlueprintDetailAsync(5, 100)).ReturnsAsync(new BlueprintDetailDto { ExamBlueprintId = 5, Status = ExamBlueprintStatus.Archived });
        _repoMock.Setup(r => r.IsBlueprintUsedAsync(5)).ReturnsAsync(false);
        _repoMock.Setup(r => r.UpdateBlueprintStatusAsync(It.IsAny<List<int>>(), 100, ExamBlueprintStatus.Archived)).ReturnsAsync(1);
        _repoMock.Setup(r => r.CreateBlueprintAsync(It.IsAny<ExamBlueprint>(), It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                 .ReturnsAsync((ExamBlueprint bp, IEnumerable<ExamBlueprintChapter> _) => { bp.ExamBlueprintId = 1000; return bp; });

        var result = await _service.UpdateBlueprintAsync(5, 100, ValidActive());

        Assert.Equal(1000, result.ExamBlueprintId);
    }

    [Fact(DisplayName = "UpdateBlueprintAsync - UTCID04 - isUsed=true -> archive + tạo mới (boundary)")]
    [TestType("B")]
    public async Task UpdateBlueprintAsync_UTCID04_IsUsed_ShouldClone()
    {
        SetupSubjectAndChapters();
        _repoMock.Setup(r => r.GetBlueprintDetailAsync(5, 100)).ReturnsAsync(new BlueprintDetailDto { ExamBlueprintId = 5, Status = ExamBlueprintStatus.Active });
        _repoMock.Setup(r => r.IsBlueprintUsedAsync(5)).ReturnsAsync(true);
        _repoMock.Setup(r => r.UpdateBlueprintStatusAsync(It.IsAny<List<int>>(), 100, ExamBlueprintStatus.Archived)).ReturnsAsync(1);
        _repoMock.Setup(r => r.CreateBlueprintAsync(It.IsAny<ExamBlueprint>(), It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                 .ReturnsAsync((ExamBlueprint bp, IEnumerable<ExamBlueprintChapter> _) => { bp.ExamBlueprintId = 1001; return bp; });

        var result = await _service.UpdateBlueprintAsync(5, 100, ValidActive());

        Assert.Equal(1001, result.ExamBlueprintId);
    }

    [Fact(DisplayName = "UpdateBlueprintAsync - UTCID05 - id = 0 -> Unauthorized (boundary)")]
    [TestType("B")]
    public async Task UpdateBlueprintAsync_UTCID05_ZeroId_ShouldThrow()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.UpdateBlueprintAsync(0, 100, ValidActive()));
    }

    [Fact(DisplayName = "UpdateBlueprintAsync - UTCID06 - currentUserId = 0 -> Unauthorized (boundary)")]
    [TestType("B")]
    public async Task UpdateBlueprintAsync_UTCID06_ZeroUser_ShouldThrow()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.UpdateBlueprintAsync(5, 0, ValidActive()));
    }

    [Fact(DisplayName = "UpdateBlueprintAsync - UTCID07 - Validation errors (Name empty) -> ValidationException")]
    [TestType("A")]
    public async Task UpdateBlueprintAsync_UTCID07_BadPayload_ShouldThrow()
    {
        // Set up subject existence so validate-and-prepare can run far enough to collect errors before throw.
        SetupSubjectAndChapters();
        var req = ValidActive();
        req.Name = "  ";

        await Assert.ThrowsAsync<ExamBlueprintValidationException>(
            () => _service.UpdateBlueprintAsync(5, 100, req));
    }

    [Fact(DisplayName = "UpdateBlueprintAsync - UTCID08 - existing == null -> KeyNotFoundException")]
    [TestType("A")]
    public async Task UpdateBlueprintAsync_UTCID08_NotFound_ShouldThrow()
    {
        SetupSubjectAndChapters();
        _repoMock.Setup(r => r.GetBlueprintDetailAsync(5, 100)).ReturnsAsync((BlueprintDetailDto?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateBlueprintAsync(5, 100, ValidActive()));
    }

    [Fact(DisplayName = "UpdateBlueprintAsync - UTCID09 - SubjectExistsAsync false (trong validate) -> KeyNotFoundException")]
    [TestType("A")]
    public async Task UpdateBlueprintAsync_UTCID09_SubjectNotExist_ShouldThrow()
    {
        _repoMock.Setup(r => r.SubjectExistsAsync(1)).ReturnsAsync(false);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateBlueprintAsync(5, 100, ValidActive()));
    }

    [Fact(DisplayName = "UpdateBlueprintAsync - UTCID11 - Draft target + in-place -> message 'Cập nhật nháp' (cover non-Active branch của ternary message)")]
    [TestType("N")]
    public async Task UpdateBlueprintAsync_UTCID11_DraftTarget_ShouldUseDraftMessage()
    {
        SetupSubjectAndChapters();
        _repoMock.Setup(r => r.GetBlueprintDetailAsync(5, 100)).ReturnsAsync(new BlueprintDetailDto { ExamBlueprintId = 5, Status = ExamBlueprintStatus.Draft });
        _repoMock.Setup(r => r.IsBlueprintUsedAsync(5)).ReturnsAsync(false);
        _repoMock.Setup(r => r.UpdateBlueprintAsync(5, 100, It.IsAny<ExamBlueprint>(), It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                 .ReturnsAsync(new ExamBlueprint { ExamBlueprintId = 5, Status = ExamBlueprintStatus.Draft, Name = "X", TeacherId = 100, ConcurrencyStamp = Array.Empty<byte>(), UpdatedAtUtc = DateTime.UtcNow });
        var req = ValidActive();
        req.TargetStatus = ExamBlueprintStatus.Draft;
        req.TargetTotalQuestions = 5;

        var result = await _service.UpdateBlueprintAsync(5, 100, req);

        Assert.Equal(ExamBlueprintStatus.Draft, result.Status);
        Assert.Contains("nháp", result.Message);
    }

    [Fact(DisplayName = "UpdateBlueprintAsync - UTCID12 - request.Name = null + Rows null + nhiều lỗi -> ValidationException (boundary cho ??)")]
    [TestType("B")]
    public async Task UpdateBlueprintAsync_UTCID12_NullNameNullRows_ShouldThrow()
    {
        // Force the validator's `request.Name ?? ""` and `request.Rows ?? new List` null branches
        _repoMock.Setup(r => r.SubjectExistsAsync(1)).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetChaptersBySubjectAsync(1)).ReturnsAsync(StandardChapters());
        var req = ValidActive();
        req.Name = null!;
        req.Rows = null!;
        req.TargetTotalQuestions = 5; // Active requires >0

        var ex = await Assert.ThrowsAsync<ExamBlueprintValidationException>(
            () => _service.UpdateBlueprintAsync(5, 100, req));
        Assert.Contains(ex.Errors, e => e.Contains("Tên ma trận đề là bắt buộc"));
        Assert.Contains(ex.Errors, e => e.Contains("ít nhất một dòng"));
    }

    [Fact(DisplayName = "UpdateBlueprintAsync - UTCID13 - Description quá dài + duplicate row + difficulty=0 + insufficient bank -> nhiều lỗi gộp")]
    [TestType("A")]
    public async Task UpdateBlueprintAsync_UTCID13_AggregatedErrors_ShouldThrow()
    {
        _repoMock.Setup(r => r.SubjectExistsAsync(1)).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetChaptersBySubjectAsync(1)).ReturnsAsync(StandardChapters());
        var req = ValidActive();
        req.Description = new string('D', 1001);
        req.Rows = new List<CreateExamBlueprintRowDto>
        {
            new() { ChapterId = 0, Difficulty = 1, TotalQuestions = 5 },           // chapter <=0
            new() { ChapterId = 999, Difficulty = 1, TotalQuestions = 1 },         // chapter not in subject
            new() { ChapterId = 10, Difficulty = 0, TotalQuestions = -1 },         // bad difficulty + negative
            new() { ChapterId = 10, Difficulty = 1, TotalQuestions = 100 },        // insufficient bank (avail=5)
            new() { ChapterId = 10, Difficulty = 1, TotalQuestions = 1 }           // duplicate
        };
        req.TargetTotalQuestions = 7;

        var ex = await Assert.ThrowsAsync<ExamBlueprintValidationException>(
            () => _service.UpdateBlueprintAsync(5, 100, req));
        Assert.Contains(ex.Errors, e => e.Contains("vượt quá 1000"));
        Assert.Contains(ex.Errors, e => e.Contains("phải có chương hợp lệ"));
        Assert.Contains(ex.Errors, e => e.Contains("không thuộc"));
        Assert.Contains(ex.Errors, e => e.Contains("Mức độ 0"));
        Assert.Contains(ex.Errors, e => e.Contains("không được âm"));
        Assert.Contains(ex.Errors, e => e.Contains("Trùng dòng"));
        Assert.Contains(ex.Errors, e => e.Contains("vượt ngân hàng"));
    }

    [Fact(DisplayName = "UpdateBlueprintAsync - UTCID14 - Draft + insufficient bank -> warning thay vì error")]
    [TestType("N")]
    public async Task UpdateBlueprintAsync_UTCID14_DraftInsufficientBank_ShouldWarn()
    {
        SetupSubjectAndChapters();
        _repoMock.Setup(r => r.GetBlueprintDetailAsync(5, 100)).ReturnsAsync(new BlueprintDetailDto { ExamBlueprintId = 5, Status = ExamBlueprintStatus.Draft });
        _repoMock.Setup(r => r.IsBlueprintUsedAsync(5)).ReturnsAsync(false);
        _repoMock.Setup(r => r.UpdateBlueprintAsync(5, 100, It.IsAny<ExamBlueprint>(), It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                 .ReturnsAsync(new ExamBlueprint { ExamBlueprintId = 5, Status = ExamBlueprintStatus.Draft, Name = "X", TeacherId = 100, ConcurrencyStamp = Array.Empty<byte>(), UpdatedAtUtc = DateTime.UtcNow });
        var req = ValidActive();
        req.TargetStatus = ExamBlueprintStatus.Draft;
        req.Rows[0].TotalQuestions = 100; // available=5
        req.TargetTotalQuestions = 50; // mismatch -> also adds TARGET_TOTAL_MISMATCH warning

        var result = await _service.UpdateBlueprintAsync(5, 100, req);

        Assert.Contains(result.Warnings, w => w.Code == "INSUFFICIENT_QUESTION_BANK");
        Assert.Contains(result.Warnings, w => w.Code == "TARGET_TOTAL_MISMATCH");
    }

    [Fact(DisplayName = "UpdateBlueprintAsync - UTCID15 - Name dài + TargetStatus=99 + TargetTotal=-5 + SubjectId=0 -> nhiều lỗi")]
    [TestType("A")]
    public async Task UpdateBlueprintAsync_UTCID15_AggregateScalarErrors_ShouldThrow()
    {
        // Cover các nhánh path-0: name length > 200 (line 346), SubjectId<=0 (line 353),
        // TargetStatus invalid (line 356), TargetTotalQuestions < 0 (line 359).
        _repoMock.Setup(r => r.SubjectExistsAsync(0)).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetChaptersBySubjectAsync(0)).ReturnsAsync(new List<ChapterOptionDto>());
        var req = ValidActive();
        req.Name = new string('A', 201);
        req.SubjectId = 0;
        req.TargetStatus = 99;
        req.TargetTotalQuestions = -5;

        var ex = await Assert.ThrowsAsync<ExamBlueprintValidationException>(
            () => _service.UpdateBlueprintAsync(5, 100, req));
        Assert.Contains(ex.Errors, e => e.Contains("vượt quá 200 ký tự"));
        Assert.Contains(ex.Errors, e => e.Contains("Môn học không hợp lệ"));
        Assert.Contains(ex.Errors, e => e.Contains("Trạng thái mục tiêu không hợp lệ"));
        Assert.Contains(ex.Errors, e => e.Contains("không được âm"));
    }

    [Fact(DisplayName = "UpdateBlueprintAsync - UTCID16 - Active + Target=0 (boundary cho <= 0)")]
    [TestType("B")]
    public async Task UpdateBlueprintAsync_UTCID16_ActiveTargetZero_ShouldThrow()
    {
        SetupSubjectAndChapters();
        var req = ValidActive();
        req.TargetTotalQuestions = 0; // <=0
        req.Rows = new List<CreateExamBlueprintRowDto>();

        var ex = await Assert.ThrowsAsync<ExamBlueprintValidationException>(
            () => _service.UpdateBlueprintAsync(5, 100, req));
        Assert.Contains(ex.Errors, e => e.Contains("tổng số câu mục tiêu lớn hơn 0"));
    }

    [Fact(DisplayName = "UpdateBlueprintAsync - UTCID17 - row có (chapterId, difficulty) không có trong availabilityMap")]
    [TestType("B")]
    public async Task UpdateBlueprintAsync_UTCID17_AvailabilityMissing_ShouldErrorOnActive()
    {
        // Chapter 10 only has availability for diff 1 → using diff 2 hits TryGetValue false → available=0.
        SetupSubjectAndChapters();
        var req = ValidActive();
        req.Rows[0] = new CreateExamBlueprintRowDto { ChapterId = 10, Difficulty = 2, TotalQuestions = 5 };
        req.TargetTotalQuestions = 5;

        var ex = await Assert.ThrowsAsync<ExamBlueprintValidationException>(
            () => _service.UpdateBlueprintAsync(5, 100, req));
        Assert.Contains(ex.Errors, e => e.Contains("hiện có 0"));
    }

    [Fact(DisplayName = "UpdateBlueprintAsync - UTCID10 - In-place repo trả null -> KeyNotFoundException")]
    [TestType("A")]
    public async Task UpdateBlueprintAsync_UTCID10_RepoUpdateReturnsNull_ShouldThrow()
    {
        SetupSubjectAndChapters();
        _repoMock.Setup(r => r.GetBlueprintDetailAsync(5, 100)).ReturnsAsync(new BlueprintDetailDto { ExamBlueprintId = 5, Status = ExamBlueprintStatus.Draft });
        _repoMock.Setup(r => r.IsBlueprintUsedAsync(5)).ReturnsAsync(false);
        _repoMock.Setup(r => r.UpdateBlueprintAsync(5, 100, It.IsAny<ExamBlueprint>(), It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                 .ReturnsAsync((ExamBlueprint?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateBlueprintAsync(5, 100, ValidActive()));
    }
}
