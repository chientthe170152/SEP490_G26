using System;
using System.Collections.Generic;

namespace Backend.DTOs.StudentExam;

public class ExamPreviewData
{
    public int ExamId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Duration { get; set; }
    public int Status { get; set; }
    public DateTime? OpenAt { get; set; }
    public DateTime? CloseAt { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public int TotalQuestions { get; set; }
    public int MaxAttempts { get; set; }
    public int PaperCount { get; set; }
    public int ShowScore { get; set; }
    public int ShowAnswer { get; set; }
    public int AnswerTimingMode { get; set; }
    public List<BlueprintChapterRaw> BlueprintChapters { get; set; } = new();
}
