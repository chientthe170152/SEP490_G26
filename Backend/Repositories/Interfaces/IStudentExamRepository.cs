using Backend.Models;

namespace Backend.Repositories.Interfaces
{
    public interface IStudentExamRepository
    {
        Task<Paper?> GetPaperWithQuestionsAsync(int examId, int paperId);
        Task<Submission> CreateSubmissionAsync(Submission submission);
        Task<Submission?> GetActiveSubmissionAsync(int studentId, int paperId);
        Task<StudentAnswer?> GetStudentAnswerAsync(int submissionId, int questionIndex);
        Task AddOrUpdateStudentAnswerAsync(StudentAnswer answer);
        Task CompleteSubmissionAsync(int submissionId);
        Task<int> GetExamSubmissionCountAsync(int studentId, int examId);
        Task<Paper?> GetPaperWithExamAsync(int paperId);
    }
}
