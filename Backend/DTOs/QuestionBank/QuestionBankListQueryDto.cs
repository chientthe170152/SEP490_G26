namespace Backend.DTOs.QuestionBank;

public class QuestionBankListQueryDto
{
    public int?    SubjectId { get; set; }
    public byte?   Purpose   { get; set; }
    /// <summary>null = all I can see; 1 = Personal only; 2 = Shared only.</summary>
    public byte?   OwnerType { get; set; }
    public string? Keyword   { get; set; }
    public int     Page      { get; set; } = 1;
    public int     PageSize  { get; set; } = 20;
}
