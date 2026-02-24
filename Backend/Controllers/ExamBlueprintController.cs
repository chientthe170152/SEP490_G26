using Backend.DTOs.ExamBlueprint;
using Backend.Exceptions;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    [Route("api/exam-blueprints")]
    [ApiController]
    [Authorize(Roles = "Teacher,Giáo viên,Admin,Quản trị viên,Administrator")]
    public class ExamBlueprintController : ControllerBase
    {
        private readonly IExamBlueprintService _examBlueprintService;

        public ExamBlueprintController(IExamBlueprintService examBlueprintService)
        {
            _examBlueprintService = examBlueprintService;
        }

        [HttpGet]
        public async Task<IActionResult> GetBlueprints([FromQuery] BlueprintListQueryDto query)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0) return Unauthorized(new { message = "Invalid token." });

                var result = await _examBlueprintService.GetBlueprintsAsync(query, userId, IsAdmin());
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetBlueprintDetail(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0) return Unauthorized(new { message = "Invalid token." });

                var result = await _examBlueprintService.GetBlueprintDetailAsync(id, userId, IsAdmin());
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("subjects")]
        public async Task<IActionResult> GetSubjects()
        {
            try
            {
                var result = await _examBlueprintService.GetSubjectsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("subjects/{subjectId:int}/chapters")]
        public async Task<IActionResult> GetChaptersBySubject(int subjectId)
        {
            try
            {
                var result = await _examBlueprintService.GetChaptersBySubjectAsync(subjectId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateBlueprint([FromBody] CreateExamBlueprintRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId <= 0) return Unauthorized(new { message = "Invalid token." });

                var result = await _examBlueprintService.CreateBlueprintAsync(userId, request);
                return CreatedAtAction(nameof(GetBlueprintDetail), new { id = result.ExamBlueprintId }, result);
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        private int GetCurrentUserId()
        {
            var userIdString = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdString, out var userId) ? userId : 0;
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin")
                || User.IsInRole("Quản trị viên")
                || User.IsInRole("Administrator");
        }

        private IActionResult HandleException(Exception ex)
        {
            return ex switch
            {
                ExamBlueprintValidationException vex => BadRequest(new { message = vex.Message, errors = vex.Errors }),
                KeyNotFoundException kex => NotFound(new { message = kex.Message }),
                UnauthorizedAccessException => Forbid(),
                _ => StatusCode(500, new { message = "Đã xảy ra lỗi trong quá trình xử lý ma trận đề.", details = ex.Message })
            };
        }
    }
}
