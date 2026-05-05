using Backend.Constants;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Backend.Repositories.Implements;

public class PromotionRepository : IPromotionRepository
{
    private readonly MtcaSep490G26Context _context;

    public PromotionRepository(MtcaSep490G26Context context)
    {
        _context = context;
    }

    public Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return _context.Database.BeginTransactionAsync();
    }

    public async Task InsertRequestAsync(QuestionPromotionRequest request)
    {
        await _context.QuestionPromotionRequests.AddAsync(request);
    }

    public async Task InsertItemAsync(QuestionPromotionRequestItem item)
    {
        await _context.QuestionPromotionRequestItems.AddAsync(item);
    }

    public async Task<bool> HasPendingItemAsync(int questionId)
    {
        return await _context.QuestionPromotionRequestItems
            .AnyAsync(i => i.QuestionId == questionId && i.Status == PromotionItemStatus.Pending);
    }

    public async Task<QuestionPromotionRequest?> GetRequestWithItemsForUpdateAsync(int requestId)
    {
        return await _context.QuestionPromotionRequests
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.PromotionRequestId == requestId);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task<(List<QuestionPromotionRequest> items, int total)> GetRequestsAsync(
        int? requestedByUserId, 
        int? status, 
        int? subjectId, 
        int? teacherId, 
        int page, 
        int pageSize)
    {
        var query = _context.QuestionPromotionRequests
            .Include(r => r.RequestedByUser)
            .Include(r => r.SourcePersonalBank)
            .Include(r => r.TargetSharedBank)
                .ThenInclude(b => b.Subject)
            .Include(r => r.Items)
            .AsQueryable();

        if (requestedByUserId.HasValue)
            query = query.Where(r => r.RequestedByUserId == requestedByUserId.Value);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        if (subjectId.HasValue)
            query = query.Where(r => r.TargetSharedBank.SubjectId == subjectId.Value);

        if (teacherId.HasValue)
            query = query.Where(r => r.RequestedByUserId == teacherId.Value);

        int total = await query.CountAsync();

        // Sort: pending requests first, then by CreatedAtUtc DESC
        var items = await query
            .OrderBy(r => r.Status == PromotionRequestStatus.Pending ? 0 : 1)
            .ThenByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<QuestionPromotionRequest?> GetRequestDetailAsync(int requestId)
    {
        return await _context.QuestionPromotionRequests
            .Include(r => r.RequestedByUser)
            .Include(r => r.SourcePersonalBank)
            .Include(r => r.TargetSharedBank)
                .ThenInclude(b => b.Subject)
            .Include(r => r.ResolvedByUser)
            .Include(r => r.Items)
                .ThenInclude(i => i.Question)
                    .ThenInclude(q => q.Chapter)
            .Include(r => r.Items)
                .ThenInclude(i => i.Question)
                    .ThenInclude(q => q.QuestionAnswers)
            .Include(r => r.Items)
                .ThenInclude(i => i.ResolvedByUser)
            .FirstOrDefaultAsync(r => r.PromotionRequestId == requestId);
    }
}
