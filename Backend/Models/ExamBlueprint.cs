using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class ExamBlueprint
{
    public int ExamBlueprintId { get; set; }

    public int SubjectId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int TotalQuestions { get; set; }

    public byte[] ConcurrencyStamp { get; set; } = null!;

    public virtual ICollection<ExamBlueprintChapter> ExamBlueprintChapters { get; set; } = new List<ExamBlueprintChapter>();

    public virtual Subject Subject { get; set; } = null!;
}
