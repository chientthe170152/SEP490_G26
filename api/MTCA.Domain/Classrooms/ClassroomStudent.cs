using MTCA.Domain.Classrooms.Enums;
using MTCA.Domain.Common;
using MTCA.Domain.Identity;

namespace MTCA.Domain.Classrooms;

public class ClassroomStudent : BaseEntity<int>, IConcurrencyAware
{
    public int ClassroomId { get; set; }
    public Guid StudentUserId { get; set; }
    public ClassroomStudentStatus Status { get; set; }
    public DateTime? JoinedAt { get; set; }
    public DateTime? RequestedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public byte[] RowVersion { get; set; } = default!;

    public Classroom Classroom { get; set; } = default!;
    public ApplicationUser Student { get; set; } = default!;
}
