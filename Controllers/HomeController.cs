using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
              
                return RedirectToAction("Index", "Login");
            }

            ViewBag.Username = username;
            ViewBag.Role = HttpContext.Session.GetString("Role");
            return View();
        }
    }
}