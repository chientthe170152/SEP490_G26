using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class ExamBlueprintChapter
{
    public int ExamBlueprintId { get; set; }

    public int ChapterId { get; set; }

    public int Difficulty { get; set; }

    public int TotalOfQuestions { get; set; }

    public byte[] ConcurrencyStamp { get; set; } = null!;

    public virtual Chapter Chapter { get; set; } = null!;

    public virtual ExamBlueprint ExamBlueprint { get; set; } = null!;
}
