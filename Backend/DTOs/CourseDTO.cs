using Backend.Models;

namespace Backend.DTOs
{
    public class CourseDTO
    {
        public int ClassId { get; set; }

        public string ClassName { get; set; } = null!;

        public string SubjectName { get; set; } = null!;

        public string TeacherName { get; set; } = null!;

        public string InvitationCode { get; set; } = null!;

        public int StudentCount { get; set; }

        public int ExamCount { get; set; }

        public string Role { get; set; } = null!;
    }
}
