using System;
using System.Collections.Generic;

namespace Backend.Models;

/// <summary>
/// Header của Promotion Request — giáo viên gửi batch câu hỏi xin duyệt lên Shared Bank.
/// Status chỉ có 3 trạng thái: Pending(1) / Resolved(3) / Withdrawn(4).
/// KHÔNG có PartiallyResolved — admin batch-finalize 1 lần khi nhấn "Xác nhận hoàn tất yêu cầu".
/// </summary>
public partial class QuestionPromotionRequest
{
    public int       PromotionRequestId   { get; set; }

    /// <summary>Giáo viên gửi request.</summary>
    public int       RequestedByUserId    { get; set; }

    /// <summary>Bank cá nhân nguồn của batch này.</summary>
    public int       SourcePersonalBankId { get; set; }

    /// <summary>Kho chung đích (cùng SubjectId — service validate).</summary>
    public int       TargetSharedBankId   { get; set; }

    /// <summary>1=Pending, 3=Resolved, 4=Withdrawn. Xem <see cref="Backend.Constants.PromotionRequestStatus"/>.</summary>
    public int       Status               { get; set; }

    /// <summary>Ghi chú của giáo viên khi submit (optional).</summary>
    public string?   Note                 { get; set; }

    public DateTime  CreatedAtUtc         { get; set; }

    /// <summary>Thời điểm header chuyển sang Resolved/Withdrawn.</summary>
    public DateTime? ResolvedAtUtc        { get; set; }

    /// <summary>Admin xử lý cuối (NULL nếu Withdrawn).</summary>
    public int?      ResolvedByUserId     { get; set; }
    public byte[]    ConcurrencyStamp     { get; set; } = null!;

    // ── Navigations ────────────────────────────────────────
    public virtual User          RequestedByUser    { get; set; } = null!;
    public virtual User?         ResolvedByUser     { get; set; }
    public virtual QuestionBank  SourcePersonalBank { get; set; } = null!;
    public virtual QuestionBank  TargetSharedBank   { get; set; } = null!;

    public virtual ICollection<QuestionPromotionRequestItem> Items { get; set; } = new List<QuestionPromotionRequestItem>();
}
