using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers
{
    public class QuestionBankController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("QuestionBank/Detail/{id}")]
        public IActionResult Detail(int id)
        {
            ViewBag.BankId = id;
            return View();
        }
    }
}
