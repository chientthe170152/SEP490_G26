using MTCA.Domain.Common;
using MTCA.Domain.Identity;
using MTCA.Domain.MasterData;

namespace MTCA.Domain.Classrooms;

public class Classroom : AggregateRoot<int>
{
    public string Name { get; set; } = default!;
    public int SubjectId { get; set; }
    public int SemesterId { get; set; }
    public Guid TeacherUserId { get; set; }
    public string? JoinCode { get; set; }
    public bool JoinCodeEnabled { get; set; }
    public bool IsArchived { get; set; }

    public Subject Subject { get; set; } = default!;
    public Semester Semester { get; set; } = default!;
    public ApplicationUser Teacher { get; set; } = default!;
    public ICollection<ClassroomStudent> Students { get; set; } = new List<ClassroomStudent>();
}
