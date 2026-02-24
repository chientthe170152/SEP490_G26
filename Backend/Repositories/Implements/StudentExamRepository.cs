using Backend.Models;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories.Implements
{
    public class StudentExamRepository : IStudentExamRepository
    {
        private readonly MtcaSep490G26Context _context;

        public StudentExamRepository(MtcaSep490G26Context context)
        {
            _context = context;
        }

        public async Task<Paper?> GetPaperWithQuestionsAsync(int examId, int paperId)
        {
            return await _context.Papers
                .Include(p => p.Exam)
                .Include(p => p.PaperQuestions)
                .ThenInclude(pq => pq.Question)
                .FirstOrDefaultAsync(p => p.ExamId == examId && p.PaperId == paperId);
        }

        public async Task<Submission> CreateSubmissionAsync(Submission submission)
        {
            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();
            return submission;
        }

        public async Task<Submission?> GetActiveSubmissionAsync(int studentId, int paperId)
        {
            // Status 1 = Active / In Progress
            return await _context.Submissions
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.PaperId == paperId && s.Status == 1);
        }

        public async Task<StudentAnswer?> GetStudentAnswerAsync(int submissionId, int questionIndex)
        {
            return await _context.StudentAnswers
                .FirstOrDefaultAsync(sa => sa.SubmissionId == submissionId && sa.QuestionIndex == questionIndex);
        }

        public async Task AddOrUpdateStudentAnswerAsync(StudentAnswer answer)
        {
            var existing = await GetStudentAnswerAsync(answer.SubmissionId, answer.QuestionIndex);
            if (existing != null)
            {
                // Update the actual response instead of updating the whole object
                existing.ResponseText = answer.ResponseText;
                _context.StudentAnswers.Update(existing);
            }
            else
            {
                _context.StudentAnswers.Add(answer);
            }
            await _context.SaveChangesAsync();
        }

        public async Task CompleteSubmissionAsync(int submissionId)
        {
            var submission = await _context.Submissions.FindAsync(submissionId);
            if (submission != null)
            {
                submission.Status = 2; // e.g. 2 = Submitted
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetExamSubmissionCountAsync(int studentId, int examId)
        {
            return await _context.Submissions
                .Include(s => s.Paper)
                .CountAsync(s => s.StudentId == studentId && s.Paper.ExamId == examId);
        }

        public async Task<Paper?> GetPaperWithExamAsync(int paperId)
        {
            return await _context.Papers
                .Include(p => p.Exam)
                .FirstOrDefaultAsync(p => p.PaperId == paperId);
        }
    }
}
