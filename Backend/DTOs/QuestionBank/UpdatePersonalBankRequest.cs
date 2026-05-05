namespace Backend.DTOs.QuestionBank;

public class UpdatePersonalBankRequest
{
    public string  Name             { get; set; } = "";
    public string? Description      { get; set; }
    /// <summary>Base-64 encoded ROWVERSION from the last GET — for optimistic concurrency.</summary>
    public string  ConcurrencyStamp { get; set; } = "";
}
