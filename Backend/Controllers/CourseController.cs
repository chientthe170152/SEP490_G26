using Backend.DTOs;
using Backend.Models;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CourseController : ControllerBase
    {
        private readonly ICourseService _service;
        private readonly IChapterService _chapterService;
        private readonly ILogger<CourseController> _logger;

        public CourseController(ICourseService service, IChapterService chapterService, ILogger<CourseController> logger)
        {
            _service = service;
            _chapterService = chapterService;
            _logger = logger;
        }

        // TEMPORARY for debugging only
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        // TEMPORARY for debugging only
        [HttpGet("my")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMyClasses()
        {
            // Keep same behaviour as before; now it will run without auth
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
            {
                // If no user id available, return all classes for quick verification or return BadRequest.
                return BadRequest("No user id claim; for debug you can return all or set a default id.");
            }

            var result = await _service.GetCoursesForUserAsync(userId);
            return Ok(result);
        }

        // New: return exams for a class that are visible now
        [HttpGet("{id}/exams")]
        [AllowAnonymous]
        public async Task<IActionResult> GetExamsForClass(int id)
        {
            var exams = await _service.GetExamsByClassAsync(id);
            return Ok(exams);
        }

        // New: return chapters belonging to the class's subject
        // Route: GET api/course/{id}/chapters
        [HttpGet("{id}/chapters")]
        [AllowAnonymous]
        public async Task<IActionResult> GetChaptersForClass(int id)
        {
            try
            {
                // Get the Class to read SubjectId
                var classEntity = await _service.GetByIdAsync(id);
                if (classEntity == null) return NotFound();

                var subjectId = classEntity.SubjectId;

                // Fetch all chapters and filter by subjectId (ChapterService returns DTOs)
                var chapters = await _chapterService.GetAllAsync();
                var filtered = chapters.Where(c => c.SubjectId == subjectId).ToList();

                return Ok(filtered);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load chapters for class {ClassId}.", id);
                return StatusCode(500, "Failed to load chapters for class.");
            }
        }
    }
}