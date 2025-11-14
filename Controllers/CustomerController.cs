using ABCRetailers.Data;
using ABCRetailers.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers.Controllers
{
    public class CustomerController : Controller
    {
        private readonly AuthDbContext _db;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(AuthDbContext db, ILogger<CustomerController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
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

                var customers = await _db.Customers
                    .Include(c => c.User)
                    .ToListAsync();

                return View(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customers");
                TempData["ErrorMessage"] = "Error loading customers. Please try again.";
                return View(new List<Customer>());
            }
        }

        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin")
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Customer customer)
        {
            try
            {
                var role = HttpContext.Session.GetString("Role");
                if (role != "Admin")
                {
                    TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                    return RedirectToAction("Index");
                }

                if (ModelState.IsValid)
                {
                    _db.Customers.Add(customer);
                    await _db.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Customer created successfully!";
                    return RedirectToAction("Index");
                }
                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                TempData["ErrorMessage"] = "Error creating customer. Please try again.";
                return View(customer);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var role = HttpContext.Session.GetString("Role");
                if (role != "Admin")
                {
                    TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                    return RedirectToAction("Index");
                }

                var customer = await _db.Customers
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer not found!";
                    return RedirectToAction("Index");
                }

                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer details");
                TempData["ErrorMessage"] = "Error loading customer details. Please try again.";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var role = HttpContext.Session.GetString("Role");
                if (role != "Admin")
                {
                    TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                    return RedirectToAction("Index");
                }

                var customer = await _db.Customers
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Customer not found!";
                    return RedirectToAction("Index");
                }

                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer for edit");
                TempData["ErrorMessage"] = "Error loading customer. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Customer customer)
        {
            try
            {
                var role = HttpContext.Session.GetString("Role");
                if (role != "Admin")
                {
                    TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                    return RedirectToAction("Index");
                }

                if (ModelState.IsValid)
                {
                    var existingCustomer = await _db.Customers.FindAsync(customer.Id);
                    if (existingCustomer == null)
                    {
                        TempData["ErrorMessage"] = "Customer not found!";
                        return RedirectToAction("Index");
                    }

                    existingCustomer.FirstName = customer.FirstName;
                    existingCustomer.LastName = customer.LastName;
                    existingCustomer.PhoneNumber = customer.PhoneNumber;
                    existingCustomer.DefaultShippingAddress = customer.DefaultShippingAddress;
                    existingCustomer.UpdatedAt = DateTime.UtcNow;

                    await _db.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Customer updated successfully!";
                    return RedirectToAction("Index");
                }
                return View(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer");
                TempData["ErrorMessage"] = "Error updating customer. Please try again.";
                return View(customer);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var role = HttpContext.Session.GetString("Role");
                if (role != "Admin")
                {
                    TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                    return RedirectToAction("Index");
                }

                var customer = await _db.Customers.FindAsync(id);
                if (customer != null)
                {
                    _db.Customers.Remove(customer);
                    await _db.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Customer deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Customer not found!";
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer");
                TempData["ErrorMessage"] = "Error deleting customer. Please try again.";
                return RedirectToAction("Index");
            }
        }
    }
}