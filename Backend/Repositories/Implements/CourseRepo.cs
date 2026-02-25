using Backend.DTOs;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
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
                    // Use the Subject navigation for subject name
                    SubjectName = c.Subject != null ? c.Subject.Name : string.Empty,
                    TeacherName = c.Teacher != null ? c.Teacher.FullName : string.Empty,
                    InvitationCode = c.InvitationCode,
                    // Map Semester from DB
                    Semester = c.Semester ?? string.Empty,
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
                    SubjectName = c.Subject != null ? c.Subject.Name : string.Empty,
                    TeacherName = c.Teacher != null ? c.Teacher.FullName : string.Empty,
                    InvitationCode = c.InvitationCode,
                    Semester = c.Semester ?? string.Empty,
                    StudentCount = c.ClassMembers.Count(),
                    ExamCount = c.Exams.Count,
                    Role = "Teacher" // When listing all classes, role is not user-specific; consumer can ignore or override.
                })
                .ToListAsync();
        }

        public async Task<Class?> GetByIdAsync(int classId)
        {
            // Include Subject and Teacher so SubjectId/Subject are available.
            return await _context.Classes
                .Include(c => c.ClassMembers)
                .Include(c => c.Exams)
                .Include(c => c.Subject)
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.ClassId == classId);
        }

        // Return exams that belong to the class and are visible now.
        // Exams with NULL VisibleFrom are treated as immediately visible.
        // Compute Status based on OpenAt / CloseAt:
        // - 1 => Open (now between OpenAt and CloseAt)
        // - 2 => Upcoming (within 30 minutes before OpenAt)
        // - 0 => Closed (otherwise)
        public async Task<List<ExamInCourseDTO>> GetExamsByClassAsync(int classId)
        {
            var now = DateTime.UtcNow;
            var upcomingThreshold = now.AddMinutes(30);

            // Include exams where VisibleFrom is null (considered visible immediately)
            // or VisibleFrom is in the past (<= now).
            var query = _context.Exams
                .Where(e => e.ClassId == classId && (e.VisibleFrom == null || e.VisibleFrom <= now));

            // Project to DTO including ChapterId and computed Status
            return await query
                .Select(e => new ExamInCourseDTO
                {
                    ExamId = e.ExamId,
                    Title = e.Title,
                    Code = null,
                    SubjectName = e.Subject != null ? e.Subject.Name : string.Empty,
                    TeacherName = e.Teacher != null ? e.Teacher.FullName : null,
                    ChapterId = _context.ExamBlueprintChapters
                                    .Where(ebc => e.ExamBlueprintId != null && ebc.ExamBlueprintId == e.ExamBlueprintId)
                                    .Select(ebc => (int?)ebc.ChapterId)
                                    .FirstOrDefault(),
                    VisibleFrom = e.VisibleFrom,
                    OpenAt = e.OpenAt,
                    CloseAt = e.CloseAt,
                    DurationMinutes = e.Duration,
                    // Compute status using OpenAt/CloseAt where available, otherwise fall back to stored Status.
                    Status = e.OpenAt != null
                        ? ( (e.OpenAt <= now && (e.CloseAt == null || e.CloseAt >= now)) ? 1
                            : (e.OpenAt > now && e.OpenAt <= upcomingThreshold) ? 2
                            : 0 )
                        : e.Status,
                    ShowScore = e.ShowScore,
                    ShowAnswer = e.ShowAnswer,
                    AllowLateSubmission = e.AllowLateSubmission
                })
                .ToListAsync();
        }
    }
}