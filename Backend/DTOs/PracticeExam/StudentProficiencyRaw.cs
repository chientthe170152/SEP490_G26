namespace Backend.DTOs.PracticeExam;

public class StudentProficiencyRaw
{
    public int ChapterId { get; set; }
    public string ChapterName { get; set; } = string.Empty;
    public int Difficulty { get; set; }
    public int TotalAttempted { get; set; }
    public int CorrectCount { get; set; }
}
