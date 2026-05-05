using Backend.Common.Errors;
using Backend.Common.Models;
using Backend.Constants;
using Backend.DTOs.Promotion;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend.Services.Implements;

public class PromotionService : IPromotionService
{
    private readonly IPromotionRepository _promotionRepo;
    private readonly IQuestionBankRepository _bankRepo;
    private readonly IQuestionRepository _questionRepo;
    private readonly ICurrentUserService _currentUserService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PromotionService> _logger;

    public PromotionService(
        IPromotionRepository promotionRepo,
        IQuestionBankRepository bankRepo,
        IQuestionRepository questionRepo,
        ICurrentUserService currentUserService,
        TimeProvider timeProvider,
        ILogger<PromotionService> logger)
    {
        _promotionRepo = promotionRepo;
        _bankRepo = bankRepo;
        _questionRepo = questionRepo;
        _currentUserService = currentUserService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Result<PromotionRequestDetailDto>> CreateAsync(CreatePromotionRequest req)
    {
        var userId = _currentUserService.UserId;
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (req.QuestionIds.Count == 0) return PromotionErrors.EmptyQuestions;
        if (req.QuestionIds.Count > 50) return PromotionErrors.TooManyQuestions;

        var source = await _bankRepo.GetByIdAsync(req.SourcePersonalBankId);
        if (source == null || source.OwnerType != BankOwnerType.Personal || source.OwnerUserId != userId)
            return PromotionErrors.SourceNotOwned;
        if (source.Status != BankStatus.Active) return QuestionBankErrors.AlreadyArchived;

        var target = await _bankRepo.GetByIdAsync(req.TargetSharedBankId);
        if (target == null || target.OwnerType != BankOwnerType.Shared) return PromotionErrors.TargetNotShared;
        if (target.Status != BankStatus.Active) return QuestionBankErrors.AlreadyArchived;

        if (target.SubjectId != source.SubjectId) return PromotionErrors.SubjectMismatch;

        var distinctQids = req.QuestionIds.Distinct().ToList();
        var questions = await _questionRepo.GetByIdsAsync(distinctQids);
        
        if (questions.Count != distinctQids.Count) return PromotionErrors.QuestionNotInSource;
        if (questions.Any(q => q.QuestionBankId != source.QuestionBankId))
            return PromotionErrors.QuestionNotInSource;

        foreach (var q in questions)
        {
            if (await _promotionRepo.HasPendingItemAsync(q.QuestionId))
                return PromotionErrors.QuestionAlreadyPending;
        }

        using var tx = await _promotionRepo.BeginTransactionAsync();
        var request = new QuestionPromotionRequest
        {
            RequestedByUserId = userId,
            SourcePersonalBankId = source.QuestionBankId,
            TargetSharedBankId = target.QuestionBankId,
            Status = PromotionRequestStatus.Pending,
            Note = req.Note?.Trim(),
            CreatedAtUtc = now,
        };
        await _promotionRepo.InsertRequestAsync(request);
        
        foreach (var qid in distinctQids)
        {
            await _promotionRepo.InsertItemAsync(new QuestionPromotionRequestItem
            {
                PromotionRequestId = request.PromotionRequestId,
                QuestionId = qid,
                Status = PromotionItemStatus.Pending,
                Request = request // Needed for EF Core cascade logic before saving
            });
        }
        
        await _promotionRepo.SaveChangesAsync();
        await tx.CommitAsync();

        // Load detail to return full DTO
        var detail = await _promotionRepo.GetRequestDetailAsync(request.PromotionRequestId);
        return MapToDetail(detail!);
    }

    public async Task<Result> FinalizeAsync(int requestId, FinalizePromotionRequest req)
    {
        var adminId = _currentUserService.UserId;
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var request = await _promotionRepo.GetRequestWithItemsForUpdateAsync(requestId);
        if (request == null) return PromotionErrors.RequestNotFound;
        if (request.Status == PromotionRequestStatus.Withdrawn) return PromotionErrors.AlreadyWithdrawn;
        if (request.Status == PromotionRequestStatus.Resolved) return PromotionErrors.AlreadyResolved;
        
        var currentStamp = Convert.ToBase64String(request.ConcurrencyStamp);
        if (currentStamp != req.ConcurrencyStamp)
            return PromotionErrors.ConcurrentUpdate;

        if (req.Decisions == null || req.Decisions.Count == 0)
            return PromotionErrors.FinalizeIncomplete;

        var decisionQids = req.Decisions.Select(d => d.QuestionId).ToList();
        if (decisionQids.Distinct().Count() != req.Decisions.Count)
            return PromotionErrors.FinalizeDuplicateItem;

        var itemQids = request.Items.Select(i => i.QuestionId).ToHashSet();
        if (req.Decisions.Any(d => !itemQids.Contains(d.QuestionId)))
            return PromotionErrors.FinalizeUnknownItem;

        var decidedQids = req.Decisions.Select(d => d.QuestionId).ToHashSet();
        if (!itemQids.IsSubsetOf(decidedQids))
            return PromotionErrors.FinalizeIncomplete;

        var approveQids = req.Decisions.Where(d => d.Decision == "approve").Select(d => d.QuestionId).ToList();
        var questions = await _questionRepo.GetByIdsAsync(approveQids);

        using var tx = await _promotionRepo.BeginTransactionAsync();
        try
        {
            var decisionMap = req.Decisions.ToDictionary(d => d.QuestionId);
            foreach (var item in request.Items)
            {
                var d = decisionMap[item.QuestionId];
                if (d.Decision == "approve")
                {
                    var q = questions.First(x => x.QuestionId == item.QuestionId);
                    if (q.QuestionBankId != request.SourcePersonalBankId)
                        return PromotionErrors.QuestionNotInSource;
                    
                    q.QuestionBankId = request.TargetSharedBankId;
                    q.UpdatedAtUtc = now;
                    item.Status = PromotionItemStatus.Approved;
                }
                else
                {
                    item.Status = PromotionItemStatus.Rejected;
                    item.RejectionReason = d.RejectionReason!.Trim();
                }
                item.ResolvedAtUtc = now;
                item.ResolvedByUserId = adminId;
            }

            request.Status = PromotionRequestStatus.Resolved;
            request.ResolvedAtUtc = now;
            request.ResolvedByUserId = adminId;

            await _promotionRepo.SaveChangesAsync();
            await tx.CommitAsync();
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync();
            return PromotionErrors.ConcurrentUpdate;
        }
    }

    public async Task<Result> WithdrawAsync(int requestId)
    {
        var userId = _currentUserService.UserId;
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var request = await _promotionRepo.GetRequestWithItemsForUpdateAsync(requestId);
        if (request == null || request.RequestedByUserId != userId)
            return PromotionErrors.RequestNotFound;
            
        if (request.Status == PromotionRequestStatus.Withdrawn) return PromotionErrors.AlreadyWithdrawn;
        if (request.Status == PromotionRequestStatus.Resolved) return PromotionErrors.AlreadyResolved;

        try
        {
            request.Status = PromotionRequestStatus.Withdrawn;
            request.ResolvedAtUtc = now;
            request.ResolvedByUserId = null;

            foreach (var item in request.Items.Where(i => i.Status == PromotionItemStatus.Pending))
            {
                item.Status = PromotionItemStatus.Cancelled;
                item.ResolvedAtUtc = now;
                item.ResolvedByUserId = null;
            }

            await _promotionRepo.SaveChangesAsync();
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return PromotionErrors.ConcurrentUpdate;
        }
    }

    public async Task<Result<PromotionRequestPage>> ListMineAsync(int? status, int page, int pageSize)
    {
        var userId = _currentUserService.UserId;
        var (items, total) = await _promotionRepo.GetRequestsAsync(userId, status, null, null, page, pageSize);
        
        return new PromotionRequestPage
        {
            Items = items.Select(MapToListItem).ToList(),
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Result<PromotionRequestPage>> ListAdminAsync(int? status, int? subjectId, int? teacherId, int page, int pageSize)
    {
        var (items, total) = await _promotionRepo.GetRequestsAsync(null, status, subjectId, teacherId, page, pageSize);
        
        return new PromotionRequestPage
        {
            Items = items.Select(MapToListItem).ToList(),
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Result<PromotionRequestDetailDto>> GetDetailAsync(int requestId, bool isAdmin)
    {
        var userId = _currentUserService.UserId;
        var request = await _promotionRepo.GetRequestDetailAsync(requestId);
        
        if (request == null) return PromotionErrors.RequestNotFound;
        if (!isAdmin && request.RequestedByUserId != userId) return PromotionErrors.RequestNotFound;

        return MapToDetail(request);
    }

    private PromotionRequestListItemDto MapToListItem(QuestionPromotionRequest req)
    {
        return new PromotionRequestListItemDto
        {
            PromotionRequestId = req.PromotionRequestId,
            RequestedByUserId = req.RequestedByUserId,
            RequestedByName = req.RequestedByUser?.FullName ?? "",
            SourcePersonalBankId = req.SourcePersonalBankId,
            SourceBankName = req.SourcePersonalBank?.Name ?? "",
            TargetSharedBankId = req.TargetSharedBankId,
            TargetBankName = req.TargetSharedBank?.Name ?? "",
            SubjectName = req.TargetSharedBank?.Subject?.Name ?? "",
            TargetPurpose = req.TargetSharedBank?.Purpose ?? 0,
            Status = req.Status,
            TotalItems = req.Items.Count,
            PendingCount = req.Items.Count(i => i.Status == PromotionItemStatus.Pending),
            ApprovedCount = req.Items.Count(i => i.Status == PromotionItemStatus.Approved),
            RejectedCount = req.Items.Count(i => i.Status == PromotionItemStatus.Rejected),
            CreatedAtUtc = req.CreatedAtUtc,
            ResolvedAtUtc = req.ResolvedAtUtc
        };
    }

    private PromotionRequestDetailDto MapToDetail(QuestionPromotionRequest req)
    {
        var dto = new PromotionRequestDetailDto
        {
            PromotionRequestId = req.PromotionRequestId,
            RequestedByUserId = req.RequestedByUserId,
            RequestedByName = req.RequestedByUser?.FullName ?? "",
            SourcePersonalBankId = req.SourcePersonalBankId,
            SourceBankName = req.SourcePersonalBank?.Name ?? "",
            TargetSharedBankId = req.TargetSharedBankId,
            TargetBankName = req.TargetSharedBank?.Name ?? "",
            SubjectName = req.TargetSharedBank?.Subject?.Name ?? "",
            TargetPurpose = req.TargetSharedBank?.Purpose ?? 0,
            Status = req.Status,
            TotalItems = req.Items.Count,
            PendingCount = req.Items.Count(i => i.Status == PromotionItemStatus.Pending),
            ApprovedCount = req.Items.Count(i => i.Status == PromotionItemStatus.Approved),
            RejectedCount = req.Items.Count(i => i.Status == PromotionItemStatus.Rejected),
            CreatedAtUtc = req.CreatedAtUtc,
            ResolvedAtUtc = req.ResolvedAtUtc,
            Note = req.Note,
            ConcurrencyStamp = Convert.ToBase64String(req.ConcurrencyStamp),
            Items = req.Items.Select(MapToItemDto).ToList()
        };
        return dto;
    }

    private PromotionItemDto MapToItemDto(QuestionPromotionRequestItem item)
    {
        return new PromotionItemDto
        {
            QuestionId = item.QuestionId,
            QuestionContent = item.Question?.QuestionContent ?? "",
            QuestionType = item.Question?.QuestionType ?? "",
            Difficulty = item.Question?.Difficulty ?? 0,
            ChapterId = item.Question?.ChapterId ?? 0,
            ChapterName = item.Question?.Chapter?.Name ?? "",
            AuthorUserId = item.Question?.CreatedByUserId ?? 0,
            AuthorName = "", // Author name might need extra join or mapping if required, but currently missing from Item.Question navigation
            Status = item.Status,
            RejectionReason = item.RejectionReason,
            ResolvedAtUtc = item.ResolvedAtUtc,
            ResolvedByUserId = item.ResolvedByUserId,
            ResolvedByName = item.ResolvedByUser?.FullName,
            ConcurrencyStamp = Convert.ToBase64String(item.ConcurrencyStamp),
            Answers = item.Question?.QuestionAnswers.Select(a => new AnswerPreviewDto
            {
                AnswerId = a.QuestionAnswerId,
                Content = a.Content,
                CorrectAnswer = a.CorrectAnswer,
                IsCorrect = a.IsCorrect,
                Point = a.Point
            }).ToList() ?? new List<AnswerPreviewDto>()
        };
    }
}
