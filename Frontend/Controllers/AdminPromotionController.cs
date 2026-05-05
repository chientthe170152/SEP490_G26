using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers;

[Route("Admin/PromotionRequests")]
public class AdminPromotionController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.Breadcrumb = new[] { ("Duyệt câu hỏi", (string?)null) };
        return View();
    }

    [HttpGet("{id}")]
    public IActionResult Detail(int id)
    {
        ViewBag.Breadcrumb = new[] { 
            ("Duyệt câu hỏi", "/Admin/PromotionRequests"),
            ($"Chi tiết yêu cầu #{id}", (string?)null)
        };
        ViewBag.RequestId = id;
        return View();
    }
}
