using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Question
{
    public int QuestionId { get; set; }

    public int CreatedByUserId { get; set; }

    public string QuestionType { get; set; } = null!;

    public string ContentLatex { get; set; } = null!;

    public string? Answer { get; set; }

    public int ChapterId { get; set; }

    public int Difficulty { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public string Status { get; set; } = null!;

    public byte[] ConcurrencyStamp { get; set; } = null!;

    public virtual Chapter Chapter { get; set; } = null!;

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual ICollection<PaperQuestion> PaperQuestions { get; set; } = new List<PaperQuestion>();
}
