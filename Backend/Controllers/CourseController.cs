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

        public CourseController(ICourseService service)
        {
            _service = service;
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
    }
}