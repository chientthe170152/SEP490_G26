using Backend.DTOs.StudentExam;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    [Route("api/student/exams")]
    [ApiController]
    [Authorize(Roles = "Student,Học sinh")]
    public class StudentExamController : ControllerBase
    {
        private readonly IStudentExamService _studentExamService;

        public StudentExamController(IStudentExamService studentExamService)
        {
            _studentExamService = studentExamService;
        }

        private int GetStudentId()
        {
            var userIdString = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            return 0; 
        }

        [HttpGet("{examId}/paper/{paperId}")]
        public async Task<IActionResult> GetExamPaper(int examId, int paperId)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return Unauthorized("Invalid token.");

            var paper = await _studentExamService.GetExamPaperAsync(studentId, examId, paperId);
            if (paper == null)
            {
                return NotFound("Exam paper not found or access denied.");
            }

            return Ok(paper);
        }

        [HttpPost("submission/start")]
        public async Task<IActionResult> StartSubmission([FromBody] StartSubmissionRequest request)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return Unauthorized("Invalid token.");

            var submission = await _studentExamService.StartExamAsync(studentId, request);
            return Ok(new { submissionId = submission.SubmissionId, paperId = submission.PaperId, status = "Started" });
        }

        [HttpPost("submission/{submissionId}/answer")]
        public async Task<IActionResult> SaveAnswer(int submissionId, [FromBody] SubmitAnswerRequest request)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return Unauthorized("Invalid token.");

            await _studentExamService.SaveAnswerAsync(studentId, submissionId, request);
            return Ok(new { message = "Answer saved successfully." });
        }

        [HttpPost("submission/{submissionId}/answers/batch")]
        public async Task<IActionResult> SaveBulkAnswers(int submissionId, [FromBody] List<SubmitAnswerRequest> requests)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return Unauthorized("Invalid token.");

            await _studentExamService.SaveBulkAnswersAsync(studentId, submissionId, requests);
            return Ok(new { message = "Answers bulk saved successfully." });
        }

        [HttpPost("submission/{submissionId}/submit")]
        public async Task<IActionResult> SubmitExam(int submissionId)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return Unauthorized("Invalid token.");

            await _studentExamService.SubmitExamAsync(studentId, submissionId);
            return Ok(new { message = "Exam submitted successfully." });
        }
    }
}
