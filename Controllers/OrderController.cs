using ABCRetailers.Data;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers.Controllers
{
    public class OrderController : Controller
    {
        private readonly AuthDbContext _db;
        private readonly ILogger<OrderController> _logger;

        public OrderController(AuthDbContext db, ILogger<OrderController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string search = "", string status = "", string sort = "newest", int page = 1, int pageSize = 10)
        {
            try
            {
                var username = HttpContext.Session.GetString("Username");
                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Index", "Login");
                }

                // Only admin should access order management
                var role = HttpContext.Session.GetString("Role");
                if (role != "Admin")
                {
                    TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                    return RedirectToAction("Index", "Home");
                }

                var ordersQuery = _db.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .Include(o => o.Customer)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    ordersQuery = ordersQuery.Where(o =>
                        o.Id.Contains(search) ||
                        o.OrderNumber.Contains(search) ||
                        (o.Customer != null && o.Customer.FirstName.Contains(search)) ||
                        (o.Customer != null && o.Customer.LastName.Contains(search)) ||
                        o.OrderItems.Any(oi => oi.ProductName.Contains(search)));
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(status) && status != "All")
                {
                    ordersQuery = ordersQuery.Where(o => o.OrderStatus == status);
                }

                // Apply sorting
                ordersQuery = sort switch
                {
                    "oldest" => ordersQuery.OrderBy(o => o.OrderedAt),
                    "price_high" => ordersQuery.OrderByDescending(o => o.TotalAmount),
                    "price_low" => ordersQuery.OrderBy(o => o.TotalAmount),
                    _ => ordersQuery.OrderByDescending(o => o.OrderedAt)
                };

                // Get total count for pagination
                var totalCount = await ordersQuery.CountAsync();

                // Apply pagination
                var orders = await ordersQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Prepare view model
                var viewModel = new OrderIndexViewModel
                {
                    Orders = orders,
                    Search = search,
                    Status = status,
                    Sort = sort,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount
                };

                ViewBag.Statuses = new List<string> { "All", "Pending", "Confirmed", "Processing", "Shipped", "Delivered", "Cancelled", "Refunded" };
                ViewBag.SortOptions = new List<string> { "newest", "oldest", "price_high", "price_low" };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders");
                TempData["ErrorMessage"] = "Error loading orders. Please try again.";
                return View(new OrderIndexViewModel { Orders = new List<Order>() });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessOrder(string orderId)
        {
            try
            {
                var order = await _db.Orders.FindAsync(orderId);
                if (order != null)
                {
                    order.OrderStatus = "Processing";
                    await _db.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Order {orderId} has been processed successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Order not found!";
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order {OrderId}", orderId);
                TempData["ErrorMessage"] = "Error processing order. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(string orderId, string status)
        {
            try
            {
                var order = await _db.Orders.FindAsync(orderId);
                if (order != null)
                {
                    order.OrderStatus = status;
                    await _db.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Order {orderId} status updated to {status}!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Order not found!";
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for {OrderId}", orderId);
                TempData["ErrorMessage"] = "Error updating order status. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(string orderId)
        {
            try
            {
                var order = await _db.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order != null)
                {
                    if (order.OrderStatus == "Cancelled")
                    {
                        TempData["ErrorMessage"] = "Order is already cancelled.";
                        return RedirectToAction("Index");
                    }

                    // Restore product stock if order was processed
                    if (order.OrderStatus == "Processing" || order.OrderStatus == "Shipped")
                    {
                        foreach (var orderItem in order.OrderItems)
                        {
                            var product = await _db.Products.FindAsync(orderItem.ProductId);
                            if (product != null)
                            {
                                product.StockQuantity += orderItem.Quantity;
                            }
                        }
                    }

                    order.OrderStatus = "Cancelled";
                    order.CancelledAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Order {orderId} has been cancelled successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Order not found!";
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
                TempData["ErrorMessage"] = "Error cancelling order. Please try again.";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Details(string id)
        {
            try
            {
                var order = await _db.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Order not found!";
                    return RedirectToAction("Index");
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order details for {OrderId}", id);
                TempData["ErrorMessage"] = "Error loading order details. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteOrder(string orderId)
        {
            try
            {
                var order = await _db.Orders.FindAsync(orderId);
                if (order != null)
                {
                    _db.Orders.Remove(order);
                    await _db.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Order {orderId} has been deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Order not found!";
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {OrderId}", orderId);
                TempData["ErrorMessage"] = "Error deleting order. Please try again.";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> ExportOrders(string format = "csv")
        {
            try
            {
                var orders = await _db.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .Include(o => o.Customer)
                    .OrderByDescending(o => o.OrderedAt)
                    .ToListAsync();

                if (format.ToLower() == "csv")
                {
                    var csv = "OrderID,OrderNumber,CustomerName,ProductName,Quantity,UnitPrice,TotalPrice,OrderDate,Status\n";
                    csv += string.Join("\n", orders.SelectMany(o =>
                        o.OrderItems.Select(oi =>
                            $"{o.Id},{o.OrderNumber},{o.Customer?.FirstName} {o.Customer?.LastName},{oi.ProductName},{oi.Quantity},{oi.UnitPrice},{oi.TotalPrice},{o.OrderedAt:yyyy-MM-dd},{o.OrderStatus}")));

                    var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                    return File(bytes, "text/csv", $"orders_export_{DateTime.Now:yyyyMMddHHmmss}.csv");
                }

                TempData["ErrorMessage"] = "Unsupported export format.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting orders");
                TempData["ErrorMessage"] = "Error exporting orders. Please try again.";
                return RedirectToAction("Index");
            }
        }
    }
}