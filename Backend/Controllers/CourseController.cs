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
        //[HttpGet]
        //public async Task<IActionResult> GetAll()
        //{
        //    return Ok(await _service.GetAllAsync());
        //}

        [HttpGet("my")]
        [Authorize(Roles = "Teacher,Student")]
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
        [Authorize(Roles = "Teacher,Student")]
        public async Task<IActionResult> GetExamsForClass(int id)
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            var exams = await _service.GetExamsByClassAsync(id);
            return Ok(exams);
        }

        // New: return chapters belonging to the class's subject
        // Route: GET api/course/{id}/chapters
        [HttpGet("{id}/chapters")]
        [Authorize]
        public async Task<IActionResult> GetChaptersForClass(int id)
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            var course = await _service.GetByIdAsync(id);

            if (course == null)
                return NotFound();

            return Ok(course.Chapters);
        }
        [HttpPost("{id}/leave")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> LeaveCourse(int id)
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            return Ok();
        }
    }
}