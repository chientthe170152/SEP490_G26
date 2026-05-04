using System;

namespace Backend.DTOs.StudentExam;

public class SubmissionHistoryRaw
{
    public int SubmissionId { get; set; }
    public bool IsExam { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ClassName { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public int TotalQuestions { get; set; }
    public int Status { get; set; }
    public decimal? TotalPoints { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public int? ExamId { get; set; }
}
