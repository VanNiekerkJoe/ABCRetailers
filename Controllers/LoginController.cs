using ABCRetailers.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers.Controllers
{
    public class LoginController : Controller
    {
        private readonly AuthDbContext _db;
        private readonly ILogger<LoginController> _logger;
        private readonly IConfiguration _configuration;

        public LoginController(AuthDbContext db, ILogger<LoginController> logger, IConfiguration configuration)
        {
            _db = db;
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            try
            {
                HttpContext.Session.Clear();

                // Test database connection on login page load
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    ViewBag.DatabaseError = "Database configuration is missing.";
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Login Index");
                ViewBag.DatabaseError = "System configuration error. Please contact administrator.";
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Index(string username, string password)
        {
            try
            {
                _logger.LogInformation("Login attempt for user: {Username}", username);

                // Validate database connection first
                try
                {
                    var canConnect = await _db.Database.CanConnectAsync();
                    if (!canConnect)
                    {
                        TempData["ErrorMessage"] = "Database connection failed. Please try again later.";
                        return View();
                    }
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Database connection error during login");
                    TempData["ErrorMessage"] = "System temporarily unavailable. Please try again later.";
                    return View();
                }

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    TempData["ErrorMessage"] = "Please enter both username and password.";
                    return View();
                }

                // For development - accept plain text passwords from our sample data
                var user = await _db.Users
                    .FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == password);

                if (user != null)
                {
                    _logger.LogInformation("User {Username} authenticated successfully", username);

                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetString("Role", user.Role);
                    HttpContext.Session.SetString("Email", user.Email);

                    if (user.Role == "Customer")
                    {
                        var customer = await _db.Customers
                            .FirstOrDefaultAsync(c => c.UserId == user.Id);

                        if (customer != null)
                        {
                            HttpContext.Session.SetString("CustomerId", customer.Id.ToString());
                            HttpContext.Session.SetString("CustomerName", $"{customer.FirstName} {customer.LastName}");
                            HttpContext.Session.SetString("CustomerEmail", user.Email);
                            HttpContext.Session.SetString("ShippingAddress", customer.DefaultShippingAddress ?? "");

                            // Initialize cart count
                            var cartCount = await _db.Carts
                                .Where(c => c.CustomerId == customer.Id)
                                .SumAsync(c => c.Quantity);
                            HttpContext.Session.SetInt32("CartCount", cartCount);

                            _logger.LogInformation("Customer {CustomerId} logged in with {CartCount} cart items", customer.Id, cartCount);
                        }
                        else
                        {
                            _logger.LogWarning("No customer record found for user {UserId}", user.Id);
                        }
                    }
                    else
                    {
                        // Clear cart count for admin users
                        HttpContext.Session.Remove("CartCount");
                    }

                    TempData["SuccessMessage"] = $"Welcome back, {user.Username}!";
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogWarning("Invalid login attempt for username: {Username}", username);
                TempData["ErrorMessage"] = "Invalid username or password";
                TempData["LastAttemptedUsername"] = username;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for user: {Username}", username);
                TempData["ErrorMessage"] = $"System error during login: {ex.Message}";
                return View();
            }
        }

        // ... rest of your LoginController methods ...
    }
}