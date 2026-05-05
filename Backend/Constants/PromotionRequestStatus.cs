namespace Backend.Constants;

/// <summary>
/// Trạng thái của QuestionPromotionRequest (header).
/// KHÔNG có PartiallyResolved — admin batch-finalize 1 lần duy nhất.
/// </summary>
public static class PromotionRequestStatus
{
    public const int Pending   = 1;
    // 2 reserved (không dùng — tránh re-use value sau này khi thêm state)
    public const int Resolved  = 3;
    public const int Withdrawn = 4;
}
