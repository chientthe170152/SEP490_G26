using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Chapter
{
    public int ChapterId { get; set; }

    public int SubjectId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<ExamBlueprintChapter> ExamBlueprintChapters { get; set; } = new List<ExamBlueprintChapter>();

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    public virtual Subject Subject { get; set; } = null!;
}
