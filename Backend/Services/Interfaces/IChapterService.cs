using Backend.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.Services.Interfaces
{
    /// <summary>
    /// Service interface for chapter business logic.
    /// </summary>
    public interface IChapterService
    {
        Task<List<ChapterDTO>> GetAllAsync();
        Task<ChapterDTO?> GetByIdAsync(int chapterId);
    }
}
