namespace Backend.DTOs.QuestionBank;

public class QuestionBankDetailDto : QuestionBankListItemDto
{
    public string   CreatedByName    { get; set; } = "";
    public DateTime CreatedAtUtc     { get; set; }
    public string   UpdatedByName    { get; set; } = "";
    /// <summary>Base-64 encoded ROWVERSION — sent back on PUT for optimistic concurrency.</summary>
    public string   ConcurrencyStamp { get; set; } = "";
}
