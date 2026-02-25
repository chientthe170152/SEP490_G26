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
                Code = paper.Code,
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
            // If PaperId is not provided, pick a random one for this exam
            int assignedPaperId;
            if (request.PaperId.HasValue && request.PaperId.Value > 0)
            {
                assignedPaperId = request.PaperId.Value;
            }
            else
            {
                var randomPaper = await _studentExamRepository.GetRandomPaperForExamAsync(request.ExamId);
                if (randomPaper == null)
                {
                    throw new InvalidOperationException("No papers found for this exam.");
                }
                assignedPaperId = randomPaper.PaperId;
            }

            // Check if already active
            var active = await _studentExamRepository.GetActiveSubmissionAsync(studentId, assignedPaperId);
            if (active != null)
            {
                return active; // Return existing submission to continue
            }

            // Check if student has reached MaxAttempts for this exam
            var paper = await _studentExamRepository.GetPaperWithExamAsync(assignedPaperId);
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
                PaperId = assignedPaperId,
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

        public async Task SaveBulkAnswersAsync(int studentId, int submissionId, IEnumerable<SubmitAnswerRequest> requests)
        {
            var answers = requests.Select(r => new StudentAnswer
            {
                SubmissionId = submissionId,
                QuestionIndex = r.QuestionIndex,
                ResponseText = r.ResponseText
            });

            await _studentExamRepository.AddOrUpdateBulkStudentAnswersAsync(answers);
        }

        public async Task SubmitExamAsync(int studentId, int submissionId)
        {
            await _studentExamRepository.CompleteSubmissionAsync(submissionId);
        }
    }
}
