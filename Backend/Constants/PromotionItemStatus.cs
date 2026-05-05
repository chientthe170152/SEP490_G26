namespace Backend.Constants;

/// <summary>
/// Trạng thái của từng QuestionPromotionRequestItem.
/// Cancelled dùng cho flow Withdraw — tránh "ghost pending items" sau khi giáo viên rút request.
/// </summary>
public static class PromotionItemStatus
{
    public const int Pending   = 1;
    public const int Approved  = 2;
    public const int Rejected  = 3;
    public const int Cancelled = 4;
}
