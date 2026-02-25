using Backend.DTOs.StudentExam;
using Backend.Models;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using System.Text.Json.Nodes;

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
            // Verify that the student is actually assigned to the class of the exam
            var canTake = await _studentExamRepository.CanStudentTakeExamAsync(studentId, examId);
            if (!canTake)
            {
                throw new UnauthorizedAccessException("Bạn không thuộc lớp được chỉ định để tham gia bài thi này.");
            }

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
                    Answer = SanitizeAnswer(pq.Question.Answer, pq.Question.QuestionType)
                }).ToList()
            };
        }

        private string SanitizeAnswer(string? answer, string? questionType)
        {
            if (string.IsNullOrWhiteSpace(answer)) return string.Empty;

            try
            {
                if (questionType == "MultipleChoice")
                {
                    var node = JsonNode.Parse(answer);
                    if (node is JsonObject jsonObj)
                    {
                        jsonObj.Remove("correct");
                        jsonObj.Remove("Correct");
                        return jsonObj.ToJsonString();
                    }
                    return answer;
                }
                else if (questionType == "StepByStep")
                {
                    var node = JsonNode.Parse(answer);
                    if (node is JsonArray jsonArray)
                    {
                        foreach (var item in jsonArray)
                        {
                            if (item is JsonObject stepObj)
                            {
                                stepObj.Remove("a");
                                stepObj.Remove("answer");
                                stepObj.Remove("Answer");
                            }
                        }
                        return jsonArray.ToJsonString();
                    }
                    return answer;
                }
                
                // For other types (e.g. ShortAnswer), the answer is just the correct text. We don't send it.
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<Submission> StartExamAsync(int studentId, StartSubmissionRequest request)
        {
            // Verify that the student is actually assigned to the class of the exam
            var canTake = await _studentExamRepository.CanStudentTakeExamAsync(studentId, request.ExamId);
            if (!canTake)
            {
                throw new UnauthorizedAccessException("Bạn không thuộc lớp được chỉ định để tham gia bài thi này.");
            }

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
                CreatedAtUtc = DateTime.UtcNow,
                Paper = paper!
            };

            return await _studentExamRepository.CreateSubmissionAsync(submission);
        }

        public async Task SaveAnswerAsync(int studentId, int submissionId, SubmitAnswerRequest request)
        {
            var submission = await _studentExamRepository.GetSubmissionByIdAsync(submissionId);
            if (submission == null) throw new InvalidOperationException("Submission not found.");
            
            if (submission.StudentId != studentId) 
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa bài làm này.");

            if (submission.Status != 1)
                throw new InvalidOperationException("Bài thi không ở trạng thái đang làm.");

            if (submission.Paper?.Exam != null)
            {
                var now = DateTime.UtcNow;
                var durationSeconds = submission.Paper.Exam.Duration * 60;
                var elapsedSeconds = (now - submission.CreatedAtUtc).TotalSeconds;
                
                if (elapsedSeconds > durationSeconds || (submission.Paper.Exam.CloseAt.HasValue && now >= submission.Paper.Exam.CloseAt.Value))
                {
                    await _studentExamRepository.CompleteSubmissionAsync(submissionId);
                    throw new InvalidOperationException("Đã hết thời gian làm bài hoặc kỳ thi đã đóng, hệ thống đã nộp bài tự động.");
                }
            }

            var answer = new StudentAnswer
            {
                SubmissionId = submissionId,
                QuestionIndex = request.QuestionIndex,
                ResponseText = request.ResponseText
            };

            await _studentExamRepository.AddOrUpdateBulkStudentAnswersAsync(new[] { answer });
        }

        public async Task SaveBulkAnswersAsync(int studentId, int submissionId, IEnumerable<SubmitAnswerRequest> requests)
        {
            var submission = await _studentExamRepository.GetSubmissionByIdAsync(submissionId);
            if (submission == null) throw new InvalidOperationException("Submission not found.");
            
            if (submission.StudentId != studentId) 
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa bài làm này.");

            if (submission.Status != 1)
                throw new InvalidOperationException("Bài thi không ở trạng thái đang làm.");

            if (submission.Paper?.Exam != null)
            {
                var now = DateTime.UtcNow;
                var durationSeconds = submission.Paper.Exam.Duration * 60;
                var elapsedSeconds = (now - submission.CreatedAtUtc).TotalSeconds;
                
                if (elapsedSeconds > durationSeconds || (submission.Paper.Exam.CloseAt.HasValue && now >= submission.Paper.Exam.CloseAt.Value))
                {
                    await _studentExamRepository.CompleteSubmissionAsync(submissionId);
                    throw new InvalidOperationException("Đã hết thời gian làm bài hoặc kỳ thi đã đóng, hệ thống đã nộp bài tự động.");
                }
            }

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
            var submission = await _studentExamRepository.GetSubmissionByIdAsync(submissionId);
            if (submission == null) throw new InvalidOperationException("Submission not found.");
            
            if (submission.StudentId != studentId) 
                throw new UnauthorizedAccessException("Bạn không có quyền nộp bài làm này.");

            await _studentExamRepository.CompleteSubmissionAsync(submissionId);
        }

        public async Task ForceSubmitOverdueSubmissionsAsync(int examId)
        {
            await _studentExamRepository.ForceSubmitOverdueExamsAsync(examId);
        }
    }
}
