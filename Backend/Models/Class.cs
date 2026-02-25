using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Class
{
    public int ClassId { get; set; }

    public int TeacherId { get; set; }

    public string Name { get; set; } = null!;

    public string Semester { get; set; } = null!;

    public string InvitationCode { get; set; } = null!;

    public int InvitationCodeStatus { get; set; }

    public int SubjectId { get; set; }

    public int Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public byte[] ConcurrencyStamp { get; set; } = null!;

    public virtual ICollection<ClassMember> ClassMembers { get; set; } = new List<ClassMember>();

    public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();

    public virtual User Teacher { get; set; } = null!;
    public virtual Subject Subject { get; set; } = null!;
}
