using Backend.DTOs;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Repositories.Implements
{
    public class CourseRepo : ICourseRepo
    {
        private readonly MtcaSep490G26Context _context;

        public CourseRepo(MtcaSep490G26Context context)
        {
            _context = context;
        }

        public async Task<List<CourseDTO>> GetCoursesForUserAsync(int userId)
        {
            return await _context.Classes
                .Where(c => c.TeacherId == userId || c.ClassMembers.Any(m => m.StudentId == userId))
                .Select(c => new CourseDTO
                {
                    ClassId = c.ClassId,
                    ClassName = c.Name,
                    SubjectName = c.Subject.Name != null ? c.Subject.Name : string.Empty,
                    TeacherName = c.Teacher != null ? c.Teacher.FullName : string.Empty,
                    InvitationCode = c.InvitationCode,
                    StudentCount = c.ClassMembers.Count(),
                    ExamCount = c.Exams.Count,
                    Role = c.TeacherId == userId ? "Teacher" : "Student"
                })
                .ToListAsync();
        }

        public async Task<List<CourseDTO>> GetAllAsync()
        {
            return await _context.Classes
                .Select(c => new CourseDTO
                {
                    ClassId = c.ClassId,
                    ClassName = c.Name,
                    SubjectName = c.Subject.Name != null ? c.Subject.Name : string.Empty,
                    TeacherName = c.Teacher != null ? c.Teacher.FullName : string.Empty,
                    InvitationCode = c.InvitationCode,
                    StudentCount = c.ClassMembers.Count(),
                    ExamCount = c.Exams.Count,
                    Role = "Teacher" // When listing all classes, role is not user-specific; consumer can ignore or override.
                })
                .ToListAsync();
        }

        public async Task<Class?> GetByIdAsync(int classId)
        {
            return await _context.Classes
                .Include(c => c.ClassMembers)
                .Include(c => c.Exams)
                .Include(c => c.Subject)
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.ClassId == classId);
        }
    }
}
