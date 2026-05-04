using Backend.DTOs.PracticeExam;
using Backend.Models;

namespace Backend.Repositories.Interfaces
{
    public interface IPracticeExamRepository
    {
        /// <summary>
        /// Lấy thông tin Class (SubjectId, TeacherId) + validate sinh viên thuộc lớp.
        /// </summary>
        Task<Class?> GetClassWithValidationAsync(int classId, int studentId);

        /// <summary>
        /// Lấy proficiency data từ submissions đã nộp, group by (ChapterId, Difficulty).
        /// </summary>
        Task<List<StudentProficiencyRaw>> GetStudentProficiencyAsync(int studentId, List<int> chapterIds);

        /// <summary>
        /// Lấy toàn bộ Question IDs luyện tập khả dụng cho các chương đã chọn (Purpose=2, Status=Active, CreatedBy=teacherId).
        /// Có thể lọc theo mức độ câu hỏi.
        /// </summary>
        Task<List<int>> GetAllPracticeQuestionIdsAsync(List<int> chapterIds, int teacherId, List<int>? difficultyLevels = null);

        /// <summary>
        /// Lấy lịch sử làm đúng/sai từng câu hỏi của sinh viên trong các chương cụ thể (phục vụ Spaced Repetition).
        /// </summary>
        Task<List<PracticeQuestionResultDto>> GetStudentItemLevelHistoryAsync(int studentId, List<int> chapterIds);

        /// <summary>
        /// Đếm số câu hỏi luyện tập khả dụng theo chương (Purpose=2, Status=Active, CreatedBy=teacherId).
        /// Có thể lọc theo mức độ câu hỏi.
        /// </summary>
        Task<int> CountPracticeQuestionsAsync(int chapterId, int teacherId, List<int>? difficultyLevels = null);

        /// <summary>
        /// Tạo Paper (ExamId=null) + gắn câu hỏi.
        /// </summary>
        Task<Paper> CreatePracticePaperAsync(List<int> questionIds);

        /// <summary>
        /// Tạo Submission cho paper luyện tập.
        /// </summary>
        Task<Submission> CreatePracticeSubmissionAsync(int studentId, int paperId);

        /// <summary>
        /// Lấy Submission kèm Paper, Questions, QuestionAnswers cho submit/result.
        /// </summary>
        Task<Submission?> GetPracticeSubmissionFullAsync(int submissionId, int studentId);

        /// <summary>
        /// Lấy lịch sử luyện tập (Paper.ExamId == null).
        /// </summary>
        Task<List<PracticeHistoryRaw>> GetPracticeHistoryAsync(int studentId, int? classId);

        /// <summary>
        /// Lấy Paper luyện tập kèm Questions + QuestionAnswers + BlankInputs.
        /// </summary>
        Task<Paper?> GetPracticePaperWithQuestionsAsync(int paperId);

        /// <summary>
        /// Lấy chapters theo subjectId.
        /// </summary>
        Task<List<Chapter>> GetChaptersBySubjectIdAsync(int subjectId);

        /// <summary>
        /// Lưu thay đổi vào DB.
        /// </summary>
        Task SaveChangesAsync();
    }

}
