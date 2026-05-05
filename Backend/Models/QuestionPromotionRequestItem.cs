using System;

namespace Backend.Models;

/// <summary>
/// Line item của Promotion Request — 1 câu hỏi trong 1 batch.
/// PK composite: (PromotionRequestId, QuestionId).
/// RejectionReason bắt buộc khi Status=3 (Rejected) — enforce cả DB CHECK và service.
/// </summary>
public partial class QuestionPromotionRequestItem
{
    public int       PromotionRequestId { get; set; }
    public int       QuestionId         { get; set; }

    /// <summary>1=Pending, 2=Approved, 3=Rejected, 4=Cancelled. Xem <see cref="Backend.Constants.PromotionItemStatus"/>.</summary>
    public int       Status             { get; set; }

    /// <summary>BẮT BUỘC khi Status=3 (Rejected). NULL khi Approved/Pending/Cancelled.</summary>
    public string?   RejectionReason    { get; set; }

    public DateTime? ResolvedAtUtc      { get; set; }

    /// <summary>Admin xử lý item này (có thể khác admin xử lý header nếu nhiều round).</summary>
    public int?      ResolvedByUserId   { get; set; }

    /// <summary>Chống race khi 2 admin cùng resolve item — EF throw DbUpdateConcurrencyException.</summary>
    public byte[]    ConcurrencyStamp   { get; set; } = null!;

    // ── Navigations ────────────────────────────────────────
    public virtual QuestionPromotionRequest Request        { get; set; } = null!;
    public virtual Question                 Question       { get; set; } = null!;
    public virtual User?                    ResolvedByUser { get; set; }
}
