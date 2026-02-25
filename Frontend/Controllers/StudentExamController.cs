using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers
{
    public class StudentExamController : Controller
    {
        // POST: /StudentExam/TakeExam
        // Hides examId and paperId from the URL
        [HttpPost]
        public IActionResult TakeExam(int examId, int paperId)
        {
            if (examId <= 0 || paperId <= 0)
            {
                // Optionally redirect to an error page or back to the exam list
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ExamId = examId;
            ViewBag.PaperId = paperId;
            return View();
        }
    }
}
