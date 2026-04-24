using MTCA.Domain.Classrooms.Enums;
using MTCA.Domain.Identity;

namespace MTCA.Domain.Classrooms;

public class ClassroomStudent
{
    public int Id { get; set; }
    public int ClassroomId { get; set; }
    public string StudentUserId { get; set; } = default!;
    public ClassroomStudentStatus Status { get; set; }
    public DateTime? JoinedAt { get; set; }
    public DateTime? RequestedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public byte[] RowVersion { get; set; } = default!;

    public Classroom Classroom { get; set; } = default!;
    public ApplicationUser Student { get; set; } = default!;
}
