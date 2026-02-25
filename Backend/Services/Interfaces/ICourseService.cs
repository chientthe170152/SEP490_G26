using Backend.DTOs;
using Backend.Models;

namespace Backend.Services.Interfaces
{
    public interface ICourseService
    {
        Task<List<CourseDTO>> GetCoursesForUserAsync(int userId);
        Task<List<CourseDTO>> GetAllAsync();
        Task<Class?> GetByIdAsync(int classId);
    }
}
