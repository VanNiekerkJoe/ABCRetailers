using ABCRetailers.Data;
using ABCRetailers.Models;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    public class CustomerController : Controller
    {
        private readonly AuthDbContext _db;
        public CustomerController(AuthDbContext db) => _db = db;

        public IActionResult Index()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Index", "Login");
            }

            // Only admin should access customer management
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            return View(_db.Customers.ToList());
        }

        public IActionResult Create() => View();

        [HttpPost]
        public IActionResult Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                _db.Customers.Add(customer);
                _db.SaveChanges();
                TempData["Success"] = "Customer created successfully!";
                return RedirectToAction("Index");
            }
            return View(customer);
        }
    }
}