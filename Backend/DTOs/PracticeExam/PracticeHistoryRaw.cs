using System;
using System.Collections.Generic;

namespace Backend.DTOs.PracticeExam;

public class PracticeHistoryRaw
{
    public int SubmissionId { get; set; }
    public int PaperId { get; set; }
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string? SubjectCode { get; set; }
    public List<string> ChapterNames { get; set; } = new();
    public int TotalQuestions { get; set; }
    public int? CorrectCount { get; set; }
    public decimal? TotalPoints { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public int Status { get; set; }
}
