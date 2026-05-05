namespace Backend.DTOs.QuestionBank;

public class CreatePersonalBankRequest
{
    public int     SubjectId   { get; set; }
    public string  Name        { get; set; } = "";
    public byte    Purpose     { get; set; }
    public string? Description { get; set; }
}
