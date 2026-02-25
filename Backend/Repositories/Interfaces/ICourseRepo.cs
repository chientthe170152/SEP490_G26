using Backend.DTOs;
using Backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Repositories.Interfaces
{
    public interface ICourseRepo
    {
        Task<List<CourseDTO>> GetCoursesForUserAsync(int userId);
        Task<List<CourseDTO>> GetAllAsync();
        Task<Class?> GetByIdAsync(int classId);
        Task<List<ExamInCourseDTO>> GetExamsByClassAsync(int classId);
    }
}
