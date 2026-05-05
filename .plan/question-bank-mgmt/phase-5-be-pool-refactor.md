# Phase 5 — BE Refactor Pool Query (Practice + Assign Exam)

> **Trạng thái**: ✅ Đã hoàn thành (commit `ec1c54d` 2026-05-05).
> **Phụ thuộc**: P3 (Question refactor) đã đóng.
> **Tham chiếu**: [`05-create-exam-flow-options.md`](05-create-exam-flow-options.md), [`06-edge-cases-and-risks.md` §7](06-edge-cases-and-risks.md#7), [`MASTER.md`](MASTER.md) §3 Q6.

## Mục tiêu

1. Khắc phục [bug âm thầm](06-edge-cases-and-risks.md#7) — pool tạo đề kiểm tra hiện không filter teacher.
2. Refactor `AssignExamRepository` + `PracticeExamRepository` query pool qua `QuestionBank` thay vì `Question.QuestionPurpose`.
3. **Mode `blueprint`**: thêm `SourceBankIds` (multi-select) vào DTO. Default = mọi Personal Bank của tôi cùng SubjectId+Purpose ∪ Shared Bank.
4. **Mode `manual`**: GIỮ NGUYÊN — vẫn chọn `QuestionIds[]` (Q6 user chốt). Validate mỗi câu thuộc bank giáo viên dùng được + cùng Purpose với bài kiểm tra (Manual mode tạo bài kiểm tra → chỉ chọn câu từ bank `Purpose=Exam`).
5. Endpoint mới: `preview-pool` (đếm câu) + `usable-banks` (list bank dùng được).
6. **Practice Pool (PR-B)**: học sinh luyện tập nhận câu từ Personal Banks của teacher-of-class ∪ Shared Practice Bank của subject.

> **★ Pending Promotion semantic**: pool query lấy câu theo `Question.QuestionBankId` (FK). Câu đang Pending Promotion (item.Status=Pending, request chưa Approve) vẫn có FK = Personal bank gốc → **vẫn xuất hiện trong pool của giáo viên**, dùng được cho Practice + Assign Exam. Chỉ khi admin Approve item, FK đổi sang Shared bank → câu rời pool Personal. Logic này tự động đúng nhờ schema (không cần code đặc biệt).

## File sửa / tạo

### Sửa (10 file)

```
Backend/Repositories/Implements/AssignExamRepository.cs    (3 method query đổi: GetQuestions, GetAllForBlueprint, Alternative)
Backend/Repositories/Interfaces/IAssignExamRepository.cs   (signature đổi)
Backend/Services/Implements/AssignExamService.cs           (validate SourceBankIds; mode manual giữ)
Backend/Controllers/AssignExamController.cs                (thêm action preview-pool, usable-banks)
Backend/DTOs/AssignExam/CreateAssignExamRequest.cs         (thêm SourceBankIds: List<int>?)
Backend/DTOs/AssignExam/PreviewPoolRequest.cs              (mới)
Backend/DTOs/AssignExam/PreviewPoolResponse.cs             (mới)
Backend/DTOs/AssignExam/UsableBankDto.cs                   (mới)

Backend/Repositories/Implements/PracticeExamRepository.cs  (3 method query đổi: GetAllPracticeQuestionIdsAsync, CountPracticeQuestionsAsync, GetPracticeQuestionCountsAsync — method thứ 3 phục vụ analytics, dùng QuestionPurpose ở [:166](../../Backend/Repositories/Implements/PracticeExamRepository.cs#L166))
Backend/Services/Implements/PracticeExamService.cs         (count + pool đổi)
```

### Sửa test (4 file)

```
BackEnd_UnitTest/AssignExamUnitTest/CreateAssignExamAsync.cs
BackEnd_UnitTest/AssignExamUnitTest/GetAlternativeQuestionsAsync.cs
BackEnd_UnitTest/PracticeExamUnitTest/CreatePracticeExamAsync.cs
BackEnd_UnitTest/PracticeExamUnitTest/GetChaptersForPracticeAsync.cs
```

### Tạo test (3 file)

```
BackEnd_UnitTest/AssignExamUnitTest/PreviewPoolAsync.cs
BackEnd_UnitTest/AssignExamUnitTest/UsableBanksAsync.cs
BackEnd_UnitTest/AssignExamUnitTest/CreateAssignExam_BankFiltering.cs
```

## Endpoint hợp đồng

### Mới

```
[Authorize(Roles = RoleIds.Teacher)]
POST   /api/assign-exam/preview-pool         body: PreviewPoolRequest    → PreviewPoolResponse
GET    /api/assign-exam/usable-banks         ?subjectId=&bankKind=        → List<UsableBankDto>
```

### Sửa

```
POST   /api/assign-exam                      body: CreateAssignExamRequest    (thêm SourceBankIds optional)
```

## DTOs

```csharp
public class CreateAssignExamRequest {
    // ... fields cũ ...
    public List<int>? SourceBankIds { get; set; }   // ← MỚI; chỉ áp dụng cho mode "blueprint"
                                                     //  null/empty = backfill = mọi bank tôi dùng được
}

public class PreviewPoolRequest {
    public int        SubjectId    { get; set; }
    public byte       Purpose     { get; set; }    // 1=Exam, 2=Practice
    public List<int>? BankIds      { get; set; }    // null = mọi bank tôi dùng được
    public List<int>? ChapterIds   { get; set; }    // optional filter
}

public class PreviewPoolResponse {
    public int TotalQuestions { get; set; }
    public List<ChapterBucket> ByChapter { get; set; } = new();
    public List<BankContribution> BankBreakdown { get; set; } = new();

    public class ChapterBucket {
        public int ChapterId    { get; set; }
        public string ChapterName { get; set; } = "";
        public List<DifficultyBucket> ByDifficulty { get; set; } = new();
    }
    public class DifficultyBucket { public int Difficulty { get; set; } public int Count { get; set; } }
    public class BankContribution { public int BankId { get; set; } public string BankName { get; set; } = ""; public int Contribution { get; set; } }
}

public class UsableBankDto {
    public int     BankId      { get; set; }
    public string  BankName    { get; set; } = "";
    public byte    OwnerType   { get; set; }
    public byte    Purpose    { get; set; }
    public int     QuestionCount { get; set; }   // active questions
}
```

## Quy tắc nghiệp vụ

### usable-banks (helper endpoint)

Trả pool bank giáo viên dùng được cho 1 (Subject, Purpose):

```csharp
public async Task<List<UsableBankDto>> GetUsableBanksAsync(int subjectId, byte bankKind)
{
    return await _db.QuestionBanks
        .Where(b => b.SubjectId == subjectId
                 && b.Purpose == bankKind
                 && b.Status == BankStatus.Active
                 && (
                     (b.OwnerType == BankOwnerType.Personal && b.OwnerUserId == userId)
                     || b.OwnerType == BankOwnerType.Shared
                    ))
        .Select(b => new UsableBankDto {
            BankId = b.QuestionBankId,
            BankName = b.Name,
            OwnerType = b.OwnerType,
            Purpose = b.Purpose,
            QuestionCount = b.Questions.Count(q => q.Status == "Active" || q.Status == "Inprogress"),
        })
        .ToListAsync();
}
```

### preview-pool

```csharp
public async Task<PreviewPoolResponse> PreviewPoolAsync(PreviewPoolRequest req)
{
    var bankIds = req.BankIds?.Any() == true
        ? req.BankIds
        : (await GetUsableBanksAsync(req.SubjectId, req.Purpose)).Select(b => b.BankId).ToList();

    // Validate quyền dùng từng bank — nếu user truyền ID không thuộc usable list → return empty
    var allowed = await _bankRepo.GetUsableBankIdsAsync(userId, req.SubjectId, req.Purpose);
    bankIds = bankIds.Intersect(allowed).ToList();

    var questions = await _db.Questions
        .Where(q => bankIds.Contains(q.QuestionBankId)
                 && (q.Status == "Active" || q.Status == "Inprogress"))
        .Where(q => req.ChapterIds == null || req.ChapterIds.Contains(q.ChapterId))
        .Select(q => new { q.QuestionBankId, q.ChapterId, q.Difficulty })
        .ToListAsync();

    // Aggregate
    var byChapter = questions.GroupBy(q => q.ChapterId)
        .Select(g => new ChapterBucket {
            ChapterId = g.Key,
            ByDifficulty = g.GroupBy(q => q.Difficulty).Select(d => new DifficultyBucket { ... }).ToList()
        }).ToList();

    var byBank = questions.GroupBy(q => q.QuestionBankId)
        .Select(g => new BankContribution { BankId = g.Key, Contribution = g.Count() }).ToList();

    // Lookup tên bank, chương từ DB
    return new PreviewPoolResponse { TotalQuestions = questions.Count, ByChapter = byChapter, BankBreakdown = byBank };
}
```

### CreateAssignExamAsync — refactor

#### Mode `blueprint`

```csharp
// Sau khi resolve blueprint (như cũ — lấy ChapterId × Difficulty × Count quotas)
// thay đổi pool query:

var bankIds = r.SourceBankIds?.Any() == true
    ? r.SourceBankIds
    : await _bankRepo.GetUsableBankIdsAsync(teacherId, bp.SubjectId, BankPurpose.Exam);

// Validate quyền
var allowed = await _bankRepo.GetUsableBankIdsAsync(teacherId, bp.SubjectId, BankPurpose.Exam);
if (bankIds.Except(allowed).Any())
    return AssignExamErrors.BankNotAccessible;   // mới

foreach (var row in bp.ExamBlueprintChapters) {
    var bank = await _repo.GetAllQuestionIdsForBlueprintRowAsync(row.ChapterId, row.Difficulty, ActiveStatus, bankIds, ct);
    // ↑ signature mới: thêm bankIds
    // ...
}
```

#### Mode `manual` (Q6 chốt — giữ chọn tay)

`r.QuestionIds[]` validate qua `BuildFromManualAsync` đã có. Bổ sung:

```csharp
private async Task<Result<...>> BuildFromManualAsync(int? sid, IReadOnlyCollection<int> ids, int teacherId)
{
    // ... check non-empty, check status active
    var sel = await _repo.GetQuestionsWithBankAndSubjectByIdsAsync(ids, ActiveStatus, ct);

    // ★ MỚI: validate mỗi câu thuộc bank teacher dùng được
    foreach (var q in sel) {
        if (q.Bank.OwnerType == BankOwnerType.Personal && q.Bank.OwnerUserId != teacherId)
            return AssignExamErrors.QuestionNotAccessible;
        if (q.Bank.OwnerType == BankOwnerType.Shared) /* always OK */;
    }

    // Cùng subject như cũ
    var subjectIds = sel.Select(x => x.SubjectId).Distinct().ToList();
    if (subjectIds.Count != 1) return AssignExamErrors.MultipleSubjects;
    // ...
}
```

→ KHÔNG nhận `SourceBankIds` ở mode manual — FE chỉ gửi field này khi mode = blueprint.

### `AssignExamRepository.GetAllQuestionIdsForBlueprintRowAsync` — signature mới

```csharp
public async Task<List<int>> GetAllQuestionIdsForBlueprintRowAsync(
    int chapterId, int difficulty, string[] activeStatus, List<int> bankIds, CancellationToken ct)
{
    return await _db.Questions
        .Where(q => bankIds.Contains(q.QuestionBankId)
                 && activeStatus.Contains(q.Status)
                 && q.ChapterId == chapterId && q.Difficulty == difficulty)
        .Select(q => q.QuestionId)
        .ToListAsync(ct);
}
```

→ KHÔNG còn `q.QuestionPurpose == QuestionBankPurpose.Exam` (phần Purpose đã được include trong filter `bankIds` — bank đã có Purpose=Exam).

### `AssignExamRepository.GetAlternativeQuestionsAsync`

Đổi tương tự — query có thêm `q.QuestionBank.Purpose == originalKind` (`Exam`) và bank thuộc usable list của teacher (truyền vào).

### Practice Pool — `PracticeExamRepository.GetAllPracticeQuestionIdsAsync`

```csharp
public async Task<List<int>> GetAllPracticeQuestionIdsAsync(
    List<int> chapterIds, int teacherIdOfClass, int subjectId, List<int>? difficultyLevels = null)
{
    var query = _context.Questions
        .Where(q => chapterIds.Contains(q.ChapterId)
                 && q.QuestionBank.SubjectId == subjectId
                 && q.QuestionBank.Purpose == BankPurpose.Practice
                 && q.QuestionBank.Status == BankStatus.Active
                 && q.Status == QuestionStatus.Active
                 && (
                     (q.QuestionBank.OwnerType == BankOwnerType.Personal && q.QuestionBank.OwnerUserId == teacherIdOfClass)
                     || q.QuestionBank.OwnerType == BankOwnerType.Shared
                    ));
    if (difficultyLevels != null && difficultyLevels.Count > 0)
        query = query.Where(q => difficultyLevels.Contains(q.Difficulty));
    return await query.Select(q => q.QuestionId).ToListAsync();
}
```

→ Đổi signature thêm `subjectId` để query gọn (đã có ở caller — class.SubjectId).

→ `CountPracticeQuestionsAsync` đổi tương tự.

→ `GetPracticeQuestionCountsAsync` (analytics — group by ChapterId + Difficulty) đổi tương tự: thay `q.QuestionPurpose == QuestionBankPurpose.Practice && q.CreatedByUserId == teacherId` bằng filter qua Bank (Personal-of-teacher ∪ Shared Practice cùng SubjectId).

### `PracticeExamService.GetChaptersForPracticeAsync` (Q8 thêm context)

Khi học sinh xem trang luyện tập, hiển thị thêm: số câu **đến từ Shared Bank** vs **đến từ Personal Bank của teacher**. Optional ở v1 — đề xuất chỉ hiển thị tổng. Phase 7 (FE) có thể thêm badge nguồn.

→ Service chỉ trả `AvailableQuestions = totalCountAcrossPools`.

## Test (3 mới + 4 sửa)

| File | Scenarios |
|---|---|
| `PreviewPoolAsync.cs` (mới) | (1) bankIds null → backfill default; (2) bankIds với bank không thuộc tôi → bị lọc bỏ; (3) byChapter / byDifficulty đếm đúng; (4) bankBreakdown contribution đúng. |
| `UsableBanksAsync.cs` (mới) | (1) trả Personal-of-mine + Shared cùng SubjectId+Kind; (2) không trả Personal của user khác; (3) không trả Shared khác SubjectId. |
| `CreateAssignExam_BankFiltering.cs` (mới) | (1) blueprint mode default sourceBankIds = mọi usable; (2) sourceBankIds chỉ Shared → pool chỉ Shared; (3) sourceBankIds có bank không thuộc tôi → BankNotAccessible; (4) manual mode bỏ qua SourceBankIds; (5) manual với question từ bank user khác → QuestionNotAccessible. |
| `CreateAssignExamAsync.cs` (sửa) | Update test cũ để dùng query qua Bank thay vì QuestionPurpose. |
| `GetAlternativeQuestionsAsync.cs` (sửa) | Tương tự. |
| `CreatePracticeExamAsync.cs` (sửa) | Pool join Bank; verify câu của teacher khác (cùng subject) KHÔNG xuất hiện trừ khi Shared. |
| `GetChaptersForPracticeAsync.cs` (sửa) | Count theo pool mới. |

## Smoke test

1. T1 tạo 5 câu kiểm tra Toán 12 (Personal Bank A); T2 tạo 5 câu kiểm tra Toán 12 (Personal Bank B của T2).
2. T1 tạo blueprint Toán 12 yêu cầu 3 câu C1/M2 → assignExam mode blueprint không truyền SourceBankIds → pool có những câu nào? (Default = Personal-of-T1 + Shared) → KHÔNG bao gồm câu của T2.
3. **Verify bug fix**: trước phase này, pool sẽ bao gồm cả câu T2 — sau phase này không.
4. T1 truyền SourceBankIds = [B của T2] → 403 BankNotAccessible.
5. T1 truyền SourceBankIds = [SharedExamBank] → pool chỉ Shared.
6. `POST /api/assign-exam/preview-pool` body `{ subjectId, bankKind=1 }` → response totalQuestions = 5 (5 câu của T1) + N (Shared).
7. Mode manual: T1 chọn `questionIds = [Q1 của T1, Q1 của T2]` → 403 QuestionNotAccessible (vì câu T2 không thuộc bank T1 dùng được).
8. Mode manual: T1 chọn `questionIds = [Q1 của T1, Qx Shared]` → success.
9. Học sinh S1 trong lớp T1 dạy: `POST /api/practice-exam/create` → pool gồm câu Practice của T1 + Shared Practice. KHÔNG có câu của T2 (T2 không phải teacher lớp).
10. Verify SQL stack trace không còn reference `QuestionPurpose` (search `Backend/` — phải sạch hoàn toàn ngoại trừ file `Constants/QuestionPurpose.cs` đang `[Obsolete]`).

## Checklist đóng phase

- [x] 10 file sửa + 3 file test mới + 4 file test sửa.
- [x] Build BE 0 errors, 0 warning `Question.QuestionPurpose` (drop column từ P1; xác nhận field không còn ai đọc).
- [x] Smoke test 1–10 pass.
- [x] Update `MASTER.md` đánh dấu P5 ✅ Done.
- [x] Phase 9 sẽ remove file `Backend/Constants/QuestionPurpose.cs` hoàn toàn.
