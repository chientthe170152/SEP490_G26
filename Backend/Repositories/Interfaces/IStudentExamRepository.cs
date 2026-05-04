using Backend.DTOs.StudentExam;
using Backend.Models;

namespace Backend.Repositories.Interfaces
{
    public interface IStudentExamRepository
    {
        Task<Paper?> GetPaperWithQuestionsAsync(int examId, int paperId);
        Task<Submission> CreateSubmissionAsync(Submission submission);
        Task<Submission?> GetAnyActiveSubmissionAsync(int studentId);
        Task<int> GetExamSubmissionCountAsync(int studentId, int examId);
        Task<Paper?> GetPaperWithExamAsync(int paperId);
        Task<Paper?> GetRandomPaperForExamAsync(int examId);
        Task<int?> GetPreviousPaperIdAsync(int studentId, int examId);
        
        // Security and validations
        Task<bool> CanStudentTakeExamAsync(int studentId, int examId);
        Task<Submission?> GetSubmissionByIdAsync(int submissionId);
        Task ForceSubmitOverdueExamsAsync(int examId);

        Task<ExamPreviewData?> GetExamPreviewAsync(int examId);

        Task<ExamInfoForStudentDto?> GetExamInfoForStudentAsync(int examId, int studentId);
        Task<Submission?> GetActiveSubmissionForExamAsync(int studentId, int examId);

        /// <summary>
        /// Lấy lịch sử bài nộp tổng hợp — projection trực tiếp, không eager load.
        /// </summary>
        Task<List<SubmissionHistoryRaw>> GetSubmissionHistoryRawAsync(int studentId, int? classId);
    }
}
