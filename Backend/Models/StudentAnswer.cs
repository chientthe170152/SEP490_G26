using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class StudentAnswer
{
    public int AnsId { get; set; }

    public int SubmissionId { get; set; }

    public int QuestionIndex { get; set; }

    public string? ResponseText { get; set; }

    public byte[] ConcurrencyStamp { get; set; } = null!;

    public virtual Submission Submission { get; set; } = null!;
}
