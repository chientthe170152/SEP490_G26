namespace Backend.DTOs.QuestionBank;

public class QuestionBankListItemDto
{
    public int      QuestionBankId { get; set; }
    public int      SubjectId      { get; set; }
    public string   SubjectName    { get; set; } = "";
    public string   SubjectCode    { get; set; } = "";
    public string   Name           { get; set; } = "";
    public string?  Description    { get; set; }
    public byte     Purpose        { get; set; }
    public byte     OwnerType      { get; set; }
    public int?     OwnerUserId    { get; set; }
    public string?  OwnerName      { get; set; }
    public int      Status         { get; set; }
    public int      QuestionCount  { get; set; }
    public DateTime UpdatedAtUtc   { get; set; }
}
