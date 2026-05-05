using System;
using System.Collections.Generic;

namespace Backend.DTOs.Promotion;

public class CreatePromotionRequest
{
    public int SourcePersonalBankId { get; set; }
    public int TargetSharedBankId { get; set; }
    public List<int> QuestionIds { get; set; } = new();
    public string? Note { get; set; }
}

public class FinalizePromotionRequest
{
    public string ConcurrencyStamp { get; set; } = "";
    public List<FinalizeItemDecision> Decisions { get; set; } = new();
}

public class FinalizeItemDecision
{
    public int QuestionId { get; set; }
    public string Decision { get; set; } = "";
    public string? RejectionReason { get; set; }
}

public class PromotionRequestListItemDto
{
    public int PromotionRequestId { get; set; }
    public int RequestedByUserId { get; set; }
    public string RequestedByName { get; set; } = "";
    public int SourcePersonalBankId { get; set; }
    public string SourceBankName { get; set; } = "";
    public int TargetSharedBankId { get; set; }
    public string TargetBankName { get; set; } = "";
    public string SubjectName { get; set; } = "";
    public byte TargetPurpose { get; set; }
    public int Status { get; set; }
    public int TotalItems { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}

public class PromotionRequestDetailDto : PromotionRequestListItemDto
{
    public string? Note { get; set; }
    public string ConcurrencyStamp { get; set; } = "";
    public List<PromotionItemDto> Items { get; set; } = new();
}

public class PromotionRequestPage
{
    public List<PromotionRequestListItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class PromotionItemDto
{
    public int QuestionId { get; set; }
    public string QuestionContent { get; set; } = "";
    public string QuestionType { get; set; } = "";
    public int Difficulty { get; set; }
    public int ChapterId { get; set; }
    public string ChapterName { get; set; } = "";
    public int AuthorUserId { get; set; }
    public string AuthorName { get; set; } = "";
    public int Status { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public int? ResolvedByUserId { get; set; }
    public string? ResolvedByName { get; set; }
    public string ConcurrencyStamp { get; set; } = "";
    public List<AnswerPreviewDto> Answers { get; set; } = new();
}

public class AnswerPreviewDto
{
    public int? AnswerId { get; set; }
    public string? Content { get; set; }
    public string? CorrectAnswer { get; set; }
    public bool? IsCorrect { get; set; }
    public int? Point { get; set; }
}
