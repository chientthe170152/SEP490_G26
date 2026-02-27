using Backend.DTOs;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChaptersController : ControllerBase
    {
        private readonly IChapterService _chapterService;
        private readonly ILogger<ChaptersController> _logger;

        public ChaptersController(IChapterService chapterService, ILogger<ChaptersController> logger)
        {
            _chapterService = chapterService;
            _logger = logger;
        }

        // GET: api/chapters
        // Return all chapters for frontend filter UI.
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var chapters = await _chapterService.GetAllAsync();
                return Ok(chapters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load chapters.");
                return StatusCode(500, "Failed to load chapters.");
            }
        }

        // GET: api/chapters/{id}
        // Return a single chapter by id (optional convenience endpoint).
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var chapter = await _chapterService.GetByIdAsync(id);
                if (chapter == null) return NotFound();
                return Ok(chapter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load chapter {ChapterId}.", id);
                return StatusCode(500, "Failed to load chapter.");
            }
        }
    }
}
