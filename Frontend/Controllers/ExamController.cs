using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers
{
    public class ExamController : Controller
    {
        public IActionResult AssignExam()
        {
            return View("assign_exam");
        }
    }
}
