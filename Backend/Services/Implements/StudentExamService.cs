using Backend.DTOs.StudentExam;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;

namespace Backend.Services.Implements
{
    public class StudentExamService : IStudentExamService
    {
        private readonly IStudentExamRepository _studentExamRepository;

        public StudentExamService(IStudentExamRepository studentExamRepository)
        {
            _studentExamRepository = studentExamRepository;
        }

        public async Task<ExamPaperDto?> GetExamPaperAsync(int studentId, int examId, int paperId)
        {
            // Optional: verify that the student is actually assigned to the class of the exam
            var paper = await _studentExamRepository.GetPaperWithQuestionsAsync(examId, paperId);
            if (paper == null || paper.Exam == null) return null;

            return new ExamPaperDto
            {
                ExamId = paper.Exam.ExamId,
                Title = paper.Exam.Title ?? string.Empty,
                Description = paper.Exam.Description,
                MaxAttempts = paper.Exam.MaxAttempts,
                Duration = paper.Exam.Duration,
                PaperId = paper.PaperId,
                Questions = paper.PaperQuestions.Select(pq => new QuestionDto
                {
                    QuestionId = pq.Question.QuestionId,
                    ContentLatex = pq.Question.ContentLatex,
                    QuestionType = pq.Question.QuestionType,
                    Difficulty = pq.Question.Difficulty,
                    Answer = pq.Question.Answer
                }).ToList()
            };
        }

        public async Task<Submission> StartExamAsync(int studentId, StartSubmissionRequest request)
        {
            // Check if already active
            var active = await _studentExamRepository.GetActiveSubmissionAsync(studentId, request.PaperId);
            if (active != null)
            {
                return active; // Return existing submission to continue
            }

            // Check if student has reached MaxAttempts for this exam
            var paper = await _studentExamRepository.GetPaperWithExamAsync(request.PaperId);
            if (paper != null && paper.Exam != null)
            {
                var attemptCount = await _studentExamRepository.GetExamSubmissionCountAsync(studentId, paper.ExamId);
                if (paper.Exam.MaxAttempts > 0 && attemptCount >= paper.Exam.MaxAttempts)
                {
                    throw new InvalidOperationException("Maximum attempts reached for this exam.");
                }
            }

            var submission = new Submission
            {
                StudentId = studentId,
                PaperId = request.PaperId,
                Status = 1, 
                CreatedAtUtc = DateTime.UtcNow
            };

            return await _studentExamRepository.CreateSubmissionAsync(submission);
        }

        public async Task SaveAnswerAsync(int studentId, int submissionId, SubmitAnswerRequest request)
        {
            var answer = new StudentAnswer
            {
                SubmissionId = submissionId,
                QuestionIndex = request.QuestionIndex,
                ResponseText = request.ResponseText
            };

            await _studentExamRepository.AddOrUpdateStudentAnswerAsync(answer);
        }

        public async Task SubmitExamAsync(int studentId, int submissionId)
        {
            await _studentExamRepository.CompleteSubmissionAsync(submissionId);
        }
    }
}
