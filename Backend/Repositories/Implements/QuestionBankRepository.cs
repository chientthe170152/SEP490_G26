using Backend.Constants;
using Backend.DTOs.QuestionBank;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories.Implements;

public class QuestionBankRepository(MtcaSep490G26Context db) : IQuestionBankRepository
{
    public async Task<(List<QuestionBankListItemDto> Items, int Total)> ListAsync(
        QuestionBankListQueryDto query, int currentUserId)
    {
        var q = db.QuestionBanks.AsNoTracking().AsQueryable();

        // Visibility filter
        if (query.OwnerType == BankOwnerType.Personal)
            q = q.Where(b => b.OwnerType == BankOwnerType.Personal && b.OwnerUserId == currentUserId);
        else if (query.OwnerType == BankOwnerType.Shared)
            q = q.Where(b => b.OwnerType == BankOwnerType.Shared);
        else
            q = q.Where(b =>
                (b.OwnerType == BankOwnerType.Personal && b.OwnerUserId == currentUserId) ||
                b.OwnerType == BankOwnerType.Shared);

        // Optional filters
        if (query.SubjectId.HasValue)
            q = q.Where(b => b.SubjectId == query.SubjectId.Value);

        if (query.Purpose.HasValue)
            q = q.Where(b => b.Purpose == query.Purpose.Value);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            q = q.Where(b => b.Name.Contains(kw) ||
                             (b.Description != null && b.Description.Contains(kw)));
        }

        var total = await q.CountAsync();

        var page     = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var items = await q
            .OrderByDescending(b => b.UpdatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new QuestionBankListItemDto
            {
                QuestionBankId = b.QuestionBankId,
                SubjectId      = b.SubjectId,
                SubjectName    = b.Subject.Name,
                SubjectCode    = b.Subject.Code,
                Name           = b.Name,
                Description    = b.Description,
                Purpose        = b.Purpose,
                OwnerType      = b.OwnerType,
                OwnerUserId    = b.OwnerUserId,
                OwnerName      = b.Owner != null ? b.Owner.FullName : null,
                Status         = b.Status,
                QuestionCount  = b.Questions.Count(qq =>
                    qq.Status == QuestionStatus.Active || qq.Status == QuestionStatus.Inprogress),
                UpdatedAtUtc   = b.UpdatedAtUtc,
            })
            .ToListAsync();

        return (items, total);
    }

    public async Task<QuestionBankDetailDto?> GetDetailAsync(int bankId)
    {
        return await db.QuestionBanks
            .AsNoTracking()
            .Where(b => b.QuestionBankId == bankId)
            .Select(b => new QuestionBankDetailDto
            {
                QuestionBankId   = b.QuestionBankId,
                SubjectId        = b.SubjectId,
                SubjectName      = b.Subject.Name,
                SubjectCode      = b.Subject.Code,
                Name             = b.Name,
                Description      = b.Description,
                Purpose          = b.Purpose,
                OwnerType        = b.OwnerType,
                OwnerUserId      = b.OwnerUserId,
                OwnerName        = b.Owner != null ? b.Owner.FullName : null,
                Status           = b.Status,
                QuestionCount    = b.Questions.Count(qq =>
                    qq.Status == QuestionStatus.Active || qq.Status == QuestionStatus.Inprogress),
                UpdatedAtUtc     = b.UpdatedAtUtc,
                CreatedByName    = b.CreatedByUser.FullName,
                CreatedAtUtc     = b.CreatedAtUtc,
                UpdatedByName    = b.UpdatedByUser.FullName,
                ConcurrencyStamp = Convert.ToBase64String(b.ConcurrencyStamp),
            })
            .FirstOrDefaultAsync();
    }

    public Task<QuestionBank?> GetByIdAsync(int bankId)
        => db.QuestionBanks.FirstOrDefaultAsync(b => b.QuestionBankId == bankId);

    public Task AddAsync(QuestionBank bank)
        => db.QuestionBanks.AddAsync(bank).AsTask();

    public Task AddManyAsync(IEnumerable<QuestionBank> banks)
        => db.QuestionBanks.AddRangeAsync(banks);

    public Task UpdateAsync(QuestionBank bank)
    {
        db.QuestionBanks.Update(bank);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Idempotent: inserts only the Shared Banks that are missing (LEFT JOIN anti-join pattern).
    /// Returns the count of rows created.
    /// </summary>
    public async Task<int> RepairSharedBanksAsync(int adminUserId, DateTime now)
    {
        // Find active subjects missing Exam Shared Bank
        var subjectsNeedingExam = await db.Subjects
            .AsNoTracking()
            .Where(s => s.Status == SubjectStatus.Active &&
                        !db.QuestionBanks.Any(b =>
                            b.SubjectId == s.SubjectId &&
                            b.OwnerType == BankOwnerType.Shared &&
                            b.Purpose   == BankPurpose.Exam))
            .ToListAsync();

        var subjectsNeedingPractice = await db.Subjects
            .AsNoTracking()
            .Where(s => s.Status == SubjectStatus.Active &&
                        !db.QuestionBanks.Any(b =>
                            b.SubjectId == s.SubjectId &&
                            b.OwnerType == BankOwnerType.Shared &&
                            b.Purpose   == BankPurpose.Practice))
            .ToListAsync();

        var toAdd = new List<QuestionBank>();

        foreach (var s in subjectsNeedingExam)
            toAdd.Add(MakeSharedBank(s.SubjectId, s.Name, s.Code, BankPurpose.Exam, adminUserId, now));

        foreach (var s in subjectsNeedingPractice)
            toAdd.Add(MakeSharedBank(s.SubjectId, s.Name, s.Code, BankPurpose.Practice, adminUserId, now));

        if (toAdd.Count > 0)
        {
            await db.QuestionBanks.AddRangeAsync(toAdd);
            await db.SaveChangesAsync();
        }

        return toAdd.Count;
    }

    public Task SaveChangesAsync() => db.SaveChangesAsync();

    public async Task<List<Backend.DTOs.UsableBankDto>> GetUsableBanksAsync(int userId, int subjectId, byte purpose)
    {
        return await db.QuestionBanks
            .Where(b => b.SubjectId == subjectId
                     && b.Purpose == purpose
                     && b.Status == BankStatus.Active
                     && (
                         (b.OwnerType == BankOwnerType.Personal && b.OwnerUserId == userId)
                         || b.OwnerType == BankOwnerType.Shared
                        ))
            .Select(b => new Backend.DTOs.UsableBankDto {
                BankId = b.QuestionBankId,
                BankName = b.Name,
                OwnerType = b.OwnerType,
                Purpose = b.Purpose,
                QuestionCount = b.Questions.Count(q => q.Status == QuestionStatus.Active || q.Status == QuestionStatus.Inprogress),
            })
            .ToListAsync();
    }

    public async Task<List<int>> GetUsableBankIdsAsync(int userId, int subjectId, byte purpose)
    {
        return await db.QuestionBanks
            .Where(b => b.SubjectId == subjectId
                     && b.Purpose == purpose
                     && b.Status == BankStatus.Active
                     && (
                         (b.OwnerType == BankOwnerType.Personal && b.OwnerUserId == userId)
                         || b.OwnerType == BankOwnerType.Shared
                        ))
            .Select(b => b.QuestionBankId)
            .ToListAsync();
    }

    // ── helpers ──────────────────────────────────────────────
    private static QuestionBank MakeSharedBank(
        int subjectId, string subjectName, string subjectCode,
        byte purpose, int adminUserId, DateTime now)
    {
        var label = purpose == BankPurpose.Exam ? "Kiem tra" : "Luyen tap";
        return new QuestionBank
        {
            SubjectId       = subjectId,
            Name            = $"Kho chung {label} {subjectName} {subjectCode}".Trim(),
            Description     = null,
            Purpose         = purpose,
            OwnerType       = BankOwnerType.Shared,
            OwnerUserId     = null,
            Status          = BankStatus.Active,
            CreatedByUserId = adminUserId,
            CreatedAtUtc    = now,
            UpdatedByUserId = adminUserId,
            UpdatedAtUtc    = now,
        };
    }
}
