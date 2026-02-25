using Backend.Models;

namespace Backend.Repositories.Interfaces
{
    public interface IStudentExamRepository
    {
        Task<Paper?> GetPaperWithQuestionsAsync(int examId, int paperId);
        Task<Submission> CreateSubmissionAsync(Submission submission);
        Task<Submission?> GetActiveSubmissionAsync(int studentId, int paperId);
        Task<StudentAnswer?> GetStudentAnswerAsync(int submissionId, int questionIndex);
        Task AddOrUpdateBulkStudentAnswersAsync(IEnumerable<StudentAnswer> answers);
        Task CompleteSubmissionAsync(int submissionId);
        Task<int> GetExamSubmissionCountAsync(int studentId, int examId);
        Task<Paper?> GetPaperWithExamAsync(int paperId);
        Task<Paper?> GetRandomPaperForExamAsync(int examId);
        
        // Security and validations
        Task<bool> CanStudentTakeExamAsync(int studentId, int examId);
        Task<Submission?> GetSubmissionByIdAsync(int submissionId);
        Task ForceSubmitOverdueExamsAsync(int examId);
    }
}
