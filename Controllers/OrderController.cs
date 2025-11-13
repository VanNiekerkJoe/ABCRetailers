using ABCRetailers.Data;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    public class OrderController : Controller
    {
        private readonly AuthDbContext _db;
        public OrderController(AuthDbContext db) => _db = db;

        public IActionResult Index() => View(_db.Orders.ToList());

        [HttpPost]
        public IActionResult Process(string id)
        {
            var order = _db.Orders.Find(id);
            if (order != null)
            {
                order.Status = "Processed";
                _db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}