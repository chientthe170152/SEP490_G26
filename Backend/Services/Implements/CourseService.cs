using Backend.DTOs.Course;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Services.Implements
{
    public class CourseService : ICourseService
    {
        private readonly ICourseRepo _repo;

        public CourseService(ICourseRepo repo)
        {
            _repo = repo;
        }

        public Task<List<CourseDTO>> GetCoursesForUserAsync(int userId)
        {
            // Keep business logic minimal for now: delegate to repo.
            // Additional business rules (filtering, sorting, masking invitation codes, etc.) can be added here.
            return _repo.GetCoursesForUserAsync(userId);
        }

        public Task<List<CourseDTO>> GetAllAsync()
        {
            return _repo.GetAllAsync();
        }

        public Task<CourseDTO?> GetByIdAsync(int classId)
        {
            return _repo.GetByIdAsync(classId);
        }

        // New: delegate to repo
        public Task<List<ExamInCourseDTO>> GetExamsByClassAsync(int classId)
        {
            return _repo.GetExamsByClassAsync(classId);
        }
    }
}
