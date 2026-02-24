using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class ClassMember
{
    public int ClassId { get; set; }

    public int StudentId { get; set; }

    public int MemberStatus { get; set; }

    public byte[] ConcurrencyStamp { get; set; } = null!;

    public virtual Class Class { get; set; } = null!;

    public virtual User Student { get; set; } = null!;
}
