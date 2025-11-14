using ABCRetailers.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers.Controllers
{
    public class LoginController : Controller
    {
        private readonly AuthDbContext _db;
        private readonly ILogger<LoginController> _logger;

        public LoginController(AuthDbContext db, ILogger<LoginController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public IActionResult Index()
        {
            try
            {
                HttpContext.Session.Clear();
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Login Index");
                TempData["ErrorMessage"] = "System error. Please try again.";
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> Index(string username, string password)
        {
            try
            {
                _logger.LogInformation("Login attempt for user: {Username}", username);

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

        public IActionResult Logout()
        {
            try
            {
                var username = HttpContext.Session.GetString("Username");
                var role = HttpContext.Session.GetString("Role");

                _logger.LogInformation("User {Username} (Role: {Role}) initiated logout", username, role);
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Logout view");
                TempData["ErrorMessage"] = "Error during logout. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public IActionResult PerformLogout()
        {
            try
            {
                var username = HttpContext.Session.GetString("Username");
                var role = HttpContext.Session.GetString("Role");

                HttpContext.Session.Clear();
                _logger.LogInformation("User {Username} (Role: {Role}) logged out successfully", username, role);

                TempData["SuccessMessage"] = "You have been successfully logged out.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout processing");
                TempData["ErrorMessage"] = "Error during logout. Please try again.";
                return RedirectToAction("Index");
            }
        }
    }
}