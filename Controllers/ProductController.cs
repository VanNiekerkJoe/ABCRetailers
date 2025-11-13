using ABCRetailers.Data;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailers.Controllers
{
    public class ProductController : Controller
    {
        private readonly AuthDbContext _db;
        public ProductController(AuthDbContext db) => _db = db;

        public IActionResult Index() => View(_db.Products.ToList());
    }
}