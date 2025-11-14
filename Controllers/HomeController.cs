using ABCRetailers.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers.Controllers
{
    public class HomeController : Controller
    {
        private readonly AuthDbContext _db;

        public HomeController(AuthDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Index", "Login");
            }

            var role = HttpContext.Session.GetString("Role");

 //love you mom and jesus and dad / sis
            if (role == "Admin")
            {
                ViewBag.TotalProducts = await _db.Products.CountAsync(p => p.IsActive && p.IsPublished); // FIXED: Added IsPublished
                ViewBag.TotalOrders = await _db.Orders.CountAsync();
                ViewBag.PendingOrders = await _db.Orders.CountAsync(o => o.OrderStatus == "Pending"); // FIXED: Status -> OrderStatus, "Placed" -> "Pending"
                ViewBag.TotalRevenue = await _db.Orders.SumAsync(o => o.TotalAmount); // FIXED: TotalPrice -> TotalAmount

                // Log the actual values for debugging
                Console.WriteLine($"Total Products: {ViewBag.TotalProducts}");
                Console.WriteLine($"Total Orders: {ViewBag.TotalOrders}");
                Console.WriteLine($"Pending Orders: {ViewBag.PendingOrders}");
                Console.WriteLine($"Total Revenue: {ViewBag.TotalRevenue}");
            }
            else
            {
                // Customer stats
                var customerId = HttpContext.Session.GetString("CustomerId");
                if (int.TryParse(customerId, out int custId))
                {
                    ViewBag.MyOrdersCount = await _db.Orders.CountAsync(o => o.CustomerId == custId);
                    ViewBag.CartItemsCount = await _db.Carts.CountAsync(c => c.CustomerId == custId); // FIXED: UserId -> CustomerId
                }
                else
                {
                    ViewBag.MyOrdersCount = 0;
                    ViewBag.CartItemsCount = 0;
                }

                ViewBag.TotalProducts = await _db.Products.CountAsync(p => p.IsActive && p.IsPublished); // FIXED: Added IsPublished
                ViewBag.CustomerEmail = HttpContext.Session.GetString("CustomerEmail");
                ViewBag.ShippingAddress = HttpContext.Session.GetString("ShippingAddress");
            }

            ViewBag.Username = username;
            ViewBag.Role = role;
            return View();
        }
    }
}