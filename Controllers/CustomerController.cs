using ABCRetailers.Data;
using ABCRetailers.Models;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    public class CustomerController : Controller
    {
        private readonly AuthDbContext _db;
        public CustomerController(AuthDbContext db) => _db = db;

        public IActionResult Index() => View(_db.Customers.ToList());

        public IActionResult Create() => View();

        [HttpPost]
        public IActionResult Create(Customer customer)
        {
            if (ModelState.IsValid)
            {
                _db.Customers.Add(customer);
                _db.SaveChanges();
                TempData["Success"] = "Customer created!";
                return RedirectToAction("Index");
            }
            return View(customer);
        }
    }
}