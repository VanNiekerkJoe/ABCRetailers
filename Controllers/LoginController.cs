using ABCRetailers.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers.Controllers
{
    public class LoginController : Controller
    {
        private readonly AuthDbContext _db;
        public LoginController(AuthDbContext db) => _db = db;

        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> Index(string username, string password)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username && u.Password == password);
            if (user != null)
            {
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role);
                return RedirectToAction("Index", "Home");
            }
            ViewBag.Error = "Invalid login";
            return View();
        }
    }
}