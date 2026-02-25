using Backend.DTOs.ExamBlueprint;

namespace Backend.Services.Interfaces
{
    public interface IExamBlueprintService
    {
        Task<List<SubjectOptionDto>> GetSubjectsAsync();
        Task<List<ChapterOptionDto>> GetChaptersBySubjectAsync(int subjectId);
        Task<BlueprintListResponseDto> GetBlueprintsAsync(BlueprintListQueryDto query, int currentUserId, bool isAdmin);
        Task<BlueprintDetailDto> GetBlueprintDetailAsync(int id, int currentUserId, bool isAdmin);
        Task<CreateExamBlueprintResponse> CreateBlueprintAsync(int currentUserId, CreateExamBlueprintRequest request);
    }
}
