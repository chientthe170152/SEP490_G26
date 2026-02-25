using System.Net.Http.Headers;
using System.Text.Json;
using Backend.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers
{
    public class CourseController : Controller
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CourseController> _logger;

        public CourseController(
            IHttpClientFactory httpFactory,
            IConfiguration configuration,
            ILogger<CourseController> logger)
        {
            _httpFactory = httpFactory;
            _configuration = configuration;
            _logger = logger;
        }

        // GET: /Course/CourseList
        // Fetch courses from backend and pass to the Razor view.
        public async Task<IActionResult> CourseList()
        {
            var backendBase = _configuration["Backend:BaseUrl"];
            if (string.IsNullOrWhiteSpace(backendBase))
            {
                _logger.LogError("Backend base URL not configured. Please set Backend:BaseUrl in appsettings.");
                return View("~/Views/Home/CourseList.cshtml", Enumerable.Empty<CourseDTO>());
            }

            // Decide endpoint:
            // - If authenticated and in "Teacher" role => backend GET api/course
            // - If authenticated and not teacher => backend GET api/course/my
            // - If not authenticated (dev) => fall back to api/course so view can show data
            string endpoint;
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Teacher"))
            {
                endpoint = "api/course";
            }
            else if (User.Identity?.IsAuthenticated == true)
            {
                // authenticated but not teacher -> try /my
                endpoint = "api/course/my";
            }
            else
            {
                // not authenticated -> use public endpoint for dev/debug
                endpoint = "api/course";
            }

            var client = _httpFactory.CreateClient();
            client.BaseAddress = new Uri(backendBase.TrimEnd('/'));

            try
            {
                // Forward access token when available
                var token = await HttpContext.GetTokenAsync("access_token");
                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not obtain access token from HttpContext.");
            }

            try
            {
                var resp = await client.GetAsync(endpoint);

                // log status for debugging
                _logger.LogInformation("GET {Endpoint} -> {StatusCode}", endpoint, resp.StatusCode);

                // If we attempted /my and backend returned 400 because there's no user id claim,
                // fall back to /api/course to get data for debugging.
                if (resp.StatusCode == System.Net.HttpStatusCode.BadRequest && endpoint.EndsWith("/my"))
                {
                    _logger.LogInformation("Backend returned 400 for /my; retrying /api/course for debug.");
                    resp = await client.GetAsync("api/course");
                }

                if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Backend requires authentication - challenge the current auth scheme.
                    _logger.LogWarning("Backend returned 401 for {Endpoint}", endpoint);
                    return Challenge();
                }

                resp.EnsureSuccessStatusCode();

                var stream = await resp.Content.ReadAsStreamAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var courses = await JsonSerializer.DeserializeAsync<List<CourseDTO>>(stream, options)
                              ?? new List<CourseDTO>();

                _logger.LogInformation("Fetched {Count} courses from backend", courses.Count);
                return View("~/Views/Home/CourseList.cshtml", courses);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP request to backend failed for endpoint {Endpoint}.", endpoint);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Failed to deserialize courses from backend response.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while loading courses.");
            }

            // On error return empty list so view can show empty state.
            return View("~/Views/Home/CourseList.cshtml", Enumerable.Empty<CourseDTO>());
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(CourseList));
        }
    }
}
