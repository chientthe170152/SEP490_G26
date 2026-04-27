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

// F53 - CreateBlueprintAsync
// Source: ExamBlueprintService.cs:79-270 — large validation block + create.
// Branches (selected):
//   1. currentUserId <= 0                            -> Unauthorized
//   2. Name empty/whitespace                         -> error
//   3. Name length > 200                             -> error
//   4. Description.Length > 1000                     -> error
//   5. SubjectId <= 0                                -> error
//   6. TargetStatus not Draft/Active                 -> error
//   7. TargetTotalQuestions < 0                      -> error
//   8. After above, errors.Count > 0                 -> throw ValidationException
//   9. SubjectExistsAsync false                      -> throw KeyNotFound
//  10. row.ChapterId <= 0                            -> error + continue
//  11. row.ChapterId not in chapter map              -> error
//  12. row.Difficulty out of [1..4]                  -> error
//  13. row.TotalQuestions < 0                        -> error
//  14. duplicate (chapterId, difficulty)             -> error
//  15. Active + rows.Count == 0                      -> error
//  16. Active + TargetTotalQuestions <= 0            -> error
//  17. Active + TargetTotalQuestions != rowTotal     -> error
//  18. Active + any row.TotalQuestions <= 0          -> error
//  19. !Active + TargetTotalQuestions != rowTotal    -> warning
//  20. row.TotalQuestions > available + Active       -> error
//  21. row.TotalQuestions > available + !Active      -> warning
//  22. errors.Count > 0 (after row checks)           -> throw
//  23. created.UpdatedAtUtc == default               -> use 'now'
public class F53_CreateBlueprintAsync_Tests
{
    private readonly Mock<IExamBlueprintRepository> _repoMock = new(MockBehavior.Strict);
    private readonly ExamBlueprintService _service;

    public F53_CreateBlueprintAsync_Tests() => _service = new ExamBlueprintService(_repoMock.Object);

    private static List<ChapterOptionDto> StandardChapters() => new()
    {
        new() { ChapterId = 10, Name = "Chương 1", AvailabilityByDifficulty = new List<ChapterAvailabilityDto>
            {
                new() { Difficulty = 1, AvailableQuestions = 5 },
                new() { Difficulty = 2, AvailableQuestions = 10 }
            }
        }
    };

    private static CreateExamBlueprintRequest ValidActive() => new()
    {
        Name = "BP1",
        Description = "Desc",
        SubjectId = 1,
        TargetTotalQuestions = 5,
        TargetStatus = ExamBlueprintStatus.Active,
        Rows = new List<CreateExamBlueprintRowDto>
        {
            new() { ChapterId = 10, Difficulty = 1, TotalQuestions = 5 }
        }
    };

    private void SetupSubjectAndChapters(int subjectId = 1, List<ChapterOptionDto>? chapters = null)
    {
        _repoMock.Setup(r => r.SubjectExistsAsync(subjectId)).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetChaptersBySubjectAsync(subjectId)).ReturnsAsync(chapters ?? StandardChapters());
    }

    [Fact(DisplayName = "CreateBlueprintAsync - UTCID01 - Active hợp lệ + đủ ngân hàng -> tạo thành công")]
    [TestType("N")]
    public async Task CreateBlueprintAsync_UTCID01_ActiveValid_ShouldCreate()
    {
        SetupSubjectAndChapters();
        _repoMock.Setup(r => r.CreateBlueprintAsync(It.IsAny<ExamBlueprint>(), It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                 .ReturnsAsync((ExamBlueprint bp, IEnumerable<ExamBlueprintChapter> _) => { bp.ExamBlueprintId = 100; return bp; });

        var result = await _service.CreateBlueprintAsync(100, ValidActive());

        Assert.Equal(100, result.ExamBlueprintId);
        Assert.Equal(ExamBlueprintStatus.Active, result.Status);
        Assert.Empty(result.Warnings);
    }

    [Fact(DisplayName = "CreateBlueprintAsync - UTCID02 - Draft với rowTotal != target -> tạo + warning")]
    [TestType("N")]
    public async Task CreateBlueprintAsync_UTCID02_DraftMismatch_ShouldWarn()
    {
        SetupSubjectAndChapters();
        var req = ValidActive();
        req.TargetStatus = ExamBlueprintStatus.Draft;
        req.TargetTotalQuestions = 7; // rowTotal=5
        _repoMock.Setup(r => r.CreateBlueprintAsync(It.IsAny<ExamBlueprint>(), It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                 .ReturnsAsync((ExamBlueprint bp, IEnumerable<ExamBlueprintChapter> _) => { bp.ExamBlueprintId = 101; return bp; });

        var result = await _service.CreateBlueprintAsync(100, req);

        Assert.Contains(result.Warnings, w => w.Code == "TARGET_TOTAL_MISMATCH");
    }

    [Fact(DisplayName = "CreateBlueprintAsync - UTCID03 - Draft + insufficient bank -> warning INSUFFICIENT_QUESTION_BANK")]
    [TestType("B")]
    public async Task CreateBlueprintAsync_UTCID03_DraftInsufficientBank_ShouldWarn()
    {
        SetupSubjectAndChapters();
        var req = ValidActive();
        req.TargetStatus = ExamBlueprintStatus.Draft;
        req.Rows[0].TotalQuestions = 100; // available=5
        req.TargetTotalQuestions = 100;
        _repoMock.Setup(r => r.CreateBlueprintAsync(It.IsAny<ExamBlueprint>(), It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                 .ReturnsAsync((ExamBlueprint bp, IEnumerable<ExamBlueprintChapter> _) => { bp.ExamBlueprintId = 102; return bp; });

        var result = await _service.CreateBlueprintAsync(100, req);

        Assert.Contains(result.Warnings, w => w.Code == "INSUFFICIENT_QUESTION_BANK");
    }

    [Fact(DisplayName = "CreateBlueprintAsync - UTCID04 - currentUserId = 0 -> Unauthorized")]
    [TestType("B")]
    public async Task CreateBlueprintAsync_UTCID04_ZeroUser_ShouldThrow()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.CreateBlueprintAsync(0, ValidActive()));
    }

    [Fact(DisplayName = "CreateBlueprintAsync - UTCID05 - Multiple field errors (name empty + subject<=0 + targetStatus invalid + total<0) -> ValidationException")]
    [TestType("A")]
    public async Task CreateBlueprintAsync_UTCID05_MultipleFieldErrors_ShouldThrow()
    {
        var req = new CreateExamBlueprintRequest
        {
            Name = "  ",
            SubjectId = 0,
            TargetStatus = 99,
            TargetTotalQuestions = -5
        };

        var ex = await Assert.ThrowsAsync<ExamBlueprintValidationException>(
            () => _service.CreateBlueprintAsync(100, req));
        Assert.Contains(ex.Errors, e => e.Contains("Tên ma trận đề là bắt buộc"));
        Assert.Contains(ex.Errors, e => e.Contains("Môn học không hợp lệ"));
        Assert.Contains(ex.Errors, e => e.Contains("Trạng thái mục tiêu không hợp lệ"));
        Assert.Contains(ex.Errors, e => e.Contains("Tổng số câu mục tiêu không được âm"));
    }

    [Fact(DisplayName = "CreateBlueprintAsync - UTCID06 - Name dài + Description dài -> ValidationException (boundary)")]
    [TestType("B")]
    public async Task CreateBlueprintAsync_UTCID06_LongFields_ShouldThrow()
    {
        var req = ValidActive();
        req.Name = new string('A', 201);
        req.Description = new string('B', 1001);

        var ex = await Assert.ThrowsAsync<ExamBlueprintValidationException>(
            () => _service.CreateBlueprintAsync(100, req));
        Assert.Contains(ex.Errors, e => e.Contains("vượt quá 200"));
        Assert.Contains(ex.Errors, e => e.Contains("vượt quá 1000"));
    }

    [Fact(DisplayName = "CreateBlueprintAsync - UTCID07 - Subject không tồn tại -> KeyNotFoundException")]
    [TestType("A")]
    public async Task CreateBlueprintAsync_UTCID07_SubjectNotFound_ShouldThrow()
    {
        _repoMock.Setup(r => r.SubjectExistsAsync(1)).ReturnsAsync(false);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.CreateBlueprintAsync(100, ValidActive()));
    }

    [Fact(DisplayName = "CreateBlueprintAsync - UTCID08 - row errors: ChapterId<=0 + difficulty out of range + negative total + duplicate -> nhiều lỗi gộp")]
    [TestType("A")]
    public async Task CreateBlueprintAsync_UTCID08_RowErrors_ShouldCollect()
    {
        SetupSubjectAndChapters();
        var req = ValidActive();
        req.Rows = new List<CreateExamBlueprintRowDto>
        {
            new() { ChapterId = 0, Difficulty = 1, TotalQuestions = 5 },          // bad chapter (<=0)
            new() { ChapterId = 999, Difficulty = 9, TotalQuestions = -1 },       // not in subject + bad difficulty + negative total
            new() { ChapterId = 10, Difficulty = 1, TotalQuestions = 1 },
            new() { ChapterId = 10, Difficulty = 1, TotalQuestions = 1 }          // duplicate
        };
        req.TargetTotalQuestions = 7;

        var ex = await Assert.ThrowsAsync<ExamBlueprintValidationException>(
            () => _service.CreateBlueprintAsync(100, req));
        Assert.Contains(ex.Errors, e => e.Contains("phải có chương hợp lệ"));
        Assert.Contains(ex.Errors, e => e.Contains("Chương 999 không thuộc"));
        Assert.Contains(ex.Errors, e => e.Contains("Mức độ 9 không hợp lệ"));
        Assert.Contains(ex.Errors, e => e.Contains("không được âm"));
        Assert.Contains(ex.Errors, e => e.Contains("Trùng dòng"));
    }

    [Fact(DisplayName = "CreateBlueprintAsync - UTCID09 - Active + rows rỗng + target<=0 -> errors")]
    [TestType("A")]
    public async Task CreateBlueprintAsync_UTCID09_ActiveNoRows_ShouldThrow()
    {
        SetupSubjectAndChapters();
        var req = ValidActive();
        req.Rows = new List<CreateExamBlueprintRowDto>();
        req.TargetTotalQuestions = 0;

        var ex = await Assert.ThrowsAsync<ExamBlueprintValidationException>(
            () => _service.CreateBlueprintAsync(100, req));
        Assert.Contains(ex.Errors, e => e.Contains("ít nhất một dòng"));
        Assert.Contains(ex.Errors, e => e.Contains("lớn hơn 0"));
    }

    [Fact(DisplayName = "CreateBlueprintAsync - UTCID10 - Active + Target != rowTotal + có row total<=0 -> errors")]
    [TestType("A")]
    public async Task CreateBlueprintAsync_UTCID10_ActiveSumMismatch_ShouldThrow()
    {
        SetupSubjectAndChapters();
        var req = ValidActive();
        req.Rows[0].TotalQuestions = 0; // active requires >0 per row
        req.TargetTotalQuestions = 7;   // != rowTotal

        var ex = await Assert.ThrowsAsync<ExamBlueprintValidationException>(
            () => _service.CreateBlueprintAsync(100, req));
        Assert.Contains(ex.Errors, e => e.Contains("phải bằng tổng số câu của các dòng"));
        Assert.Contains(ex.Errors, e => e.Contains("mỗi dòng ma trận có số câu lớn hơn 0"));
    }

    [Fact(DisplayName = "CreateBlueprintAsync - UTCID11 - Active + insufficient bank -> error")]
    [TestType("A")]
    public async Task CreateBlueprintAsync_UTCID11_ActiveInsufficientBank_ShouldThrow()
    {
        SetupSubjectAndChapters();
        var req = ValidActive();
        req.Rows[0].TotalQuestions = 50;
        req.TargetTotalQuestions = 50;

        var ex = await Assert.ThrowsAsync<ExamBlueprintValidationException>(
            () => _service.CreateBlueprintAsync(100, req));
        Assert.Contains(ex.Errors, e => e.Contains("vượt ngân hàng câu hỏi"));
    }

    [Fact(DisplayName = "CreateBlueprintAsync - UTCID13 - request.Name = null -> normalized to empty -> error 'tên ma trận đề' (boundary cho ??)")]
    [TestType("B")]
    public async Task CreateBlueprintAsync_UTCID13_NullName_ShouldThrow()
    {
        var req = ValidActive();
        req.Name = null!;
        req.Rows = new List<CreateExamBlueprintRowDto>();
        req.TargetTotalQuestions = 0;
        req.SubjectId = 1;
        req.TargetStatus = ExamBlueprintStatus.Active;

        var ex = await Assert.ThrowsAsync<ExamBlueprintValidationException>(
            () => _service.CreateBlueprintAsync(100, req));
        Assert.Contains(ex.Errors, e => e.Contains("Tên ma trận đề là bắt buộc"));
    }

    [Fact(DisplayName = "CreateBlueprintAsync - UTCID14 - request.Rows = null + Active -> 'ít nhất một dòng' (boundary cho ??)")]
    [TestType("B")]
    public async Task CreateBlueprintAsync_UTCID14_NullRows_ShouldThrow()
    {
        SetupSubjectAndChapters();
        var req = ValidActive();
        req.Rows = null!;
        req.TargetTotalQuestions = 1; // active requires > 0

        var ex = await Assert.ThrowsAsync<ExamBlueprintValidationException>(
            () => _service.CreateBlueprintAsync(100, req));
        Assert.Contains(ex.Errors, e => e.Contains("ít nhất một dòng"));
    }

    [Fact(DisplayName = "CreateBlueprintAsync - UTCID15 - row.Difficulty = 0 -> error mức độ (boundary < 1)")]
    [TestType("B")]
    public async Task CreateBlueprintAsync_UTCID15_DifficultyZero_ShouldThrow()
    {
        SetupSubjectAndChapters();
        var req = ValidActive();
        req.Rows[0].Difficulty = 0; // <1

        var ex = await Assert.ThrowsAsync<ExamBlueprintValidationException>(
            () => _service.CreateBlueprintAsync(100, req));
        Assert.Contains(ex.Errors, e => e.Contains("Mức độ 0 không hợp lệ"));
    }

    [Fact(DisplayName = "CreateBlueprintAsync - UTCID16 - row có (chapterId, difficulty) không có trong availabilityMap -> available = 0 (boundary cho TryGetValue)")]
    [TestType("B")]
    public async Task CreateBlueprintAsync_UTCID16_AvailabilityMissing_ShouldStillCheck()
    {
        // Chapter 10 has availability for diff 1 + 2 only. Use diff = 3 → TryGetValue false → available = 0 → error vì target > 0.
        SetupSubjectAndChapters();
        var req = ValidActive();
        req.Rows = new List<CreateExamBlueprintRowDto>
        {
            new() { ChapterId = 10, Difficulty = 3, TotalQuestions = 5 }
        };
        req.TargetTotalQuestions = 5;

        var ex = await Assert.ThrowsAsync<ExamBlueprintValidationException>(
            () => _service.CreateBlueprintAsync(100, req));
        Assert.Contains(ex.Errors, e => e.Contains("hiện có 0"));
    }

    [Fact(DisplayName = "CreateBlueprintAsync - UTCID12 - created.UpdatedAtUtc = default -> use 'now' (boundary)")]
    [TestType("B")]
    public async Task CreateBlueprintAsync_UTCID12_DefaultUpdatedAt_ShouldUseNow()
    {
        SetupSubjectAndChapters();
        _repoMock.Setup(r => r.CreateBlueprintAsync(It.IsAny<ExamBlueprint>(), It.IsAny<IEnumerable<ExamBlueprintChapter>>()))
                 .ReturnsAsync((ExamBlueprint bp, IEnumerable<ExamBlueprintChapter> _) =>
                 {
                     bp.ExamBlueprintId = 200;
                     bp.UpdatedAtUtc = default; // simulate repo not setting timestamp
                     return bp;
                 });

        var result = await _service.CreateBlueprintAsync(100, ValidActive());

        Assert.NotEqual(default, result.UpdatedAtUtc);
    }
}
