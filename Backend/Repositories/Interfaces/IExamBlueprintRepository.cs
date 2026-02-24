using Backend.DTOs.ExamBlueprint;
using Backend.Models;

namespace Backend.Repositories.Interfaces
{
    public interface IExamBlueprintRepository
    {
        Task<List<SubjectOptionDto>> GetSubjectsAsync();
        Task<bool> SubjectExistsAsync(int subjectId);
        Task<List<ChapterOptionDto>> GetChaptersBySubjectAsync(int subjectId);
        Task<(List<BlueprintListItemDto> Items, int TotalCount)> GetBlueprintsAsync(BlueprintListQueryDto query, int currentUserId, bool isAdmin);
        Task<BlueprintDetailDto?> GetBlueprintDetailAsync(int id, int currentUserId, bool isAdmin);
        Task<ExamBlueprint> CreateBlueprintAsync(ExamBlueprint blueprint, IEnumerable<ExamBlueprintChapter> rows);
    }
}
