using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ABCRetailers.Controllers
{
    public class OrderController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IAzureStorageService storageService, ILogger<OrderController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _storageService.GetAllEntitiesAsync<Order>();
            return View(orders);
        }

        public async Task<IActionResult> Create()
        {
            var customers = await _storageService.GetAllEntitiesAsync<Customer>();
            var products = await _storageService.GetAllEntitiesAsync<Product>();
            var viewModel = new OrderCreateViewModel
            {
                Customers = customers,
                Products = products
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get customer and product details
                    var customer = await _storageService.GetEntityAsync<Customer>("Customer", model.CustomerId);
                    var product = await _storageService.GetEntityAsync<Product>("Product", model.ProductId);
                    if (customer == null || product == null)
                    {
                        ModelState.AddModelError("", "Invalid customer or product selected.");
                        await PopulateDropdowns(model);
                        return View(model);
                    }

                    // Check stock availability
                    if (product.StockAvailable < model.Quantity)
                    {
                        ModelState.AddModelError("Quantity", $"Insufficient stock. Available: {product.StockAvailable}");
                        await PopulateDropdowns(model);
                        return View(model);
                    }

                    // Create order with UTC DateTime
                    var order = new Order
                    {
                        CustomerId = model.CustomerId,
                        Username = customer.Username,
                        ProductId = model.ProductId,
                        ProductName = product.ProductName,
                        OrderDate = DateTime.SpecifyKind(model.OrderDate, DateTimeKind.Utc),
                        Quantity = model.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price * model.Quantity,
                        Status = "Submitted"
                    };

                    await _storageService.AddEntityAsync(order);

                    // Update product stock
                    product.StockAvailable -= model.Quantity;
                    await _storageService.UpdateEntityAsync(product);

                    // === QUEUE INTEGRATION: Send order notification to queue ===
                    var orderMessage = new
                    {
                        OrderId = order.OrderId,
                        CustomerId = order.CustomerId,
                        CustomerName = customer.Name + " " + customer.Surname,
                        ProductName = product.ProductName,
                        Quantity = order.Quantity,
                        TotalPrice = order.TotalPrice,
                        OrderDate = order.OrderDate,
                        Status = order.Status
                    };
                    await _storageService.SendMessageAsync("order-notifications", JsonSerializer.Serialize(orderMessage));
                    _logger.LogInformation("Order notification sent to queue for Order ID: {OrderId}", order.OrderId);

                    // === QUEUE INTEGRATION: Send stock update to queue ===
                    var stockMessage = new
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        PreviousStock = product.StockAvailable + model.Quantity,
                        NewStock = product.StockAvailable,
                        UpdatedBy = "Order System",
                        UpdateDate = DateTime.UtcNow
                    };
                    await _storageService.SendMessageAsync("stock-updates", JsonSerializer.Serialize(stockMessage));
                    _logger.LogInformation("Stock update sent to queue for Product: {ProductName}", product.ProductName);

                    TempData["Success"] = "Order created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating order: {ex.Message}");
                    _logger.LogError(ex, "Error creating order");
                }
            }
            await PopulateDropdowns(model);
            return View(model);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var order = await _storageService.GetEntityAsync<Order>("Order", id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var order = await _storageService.GetEntityAsync<Order>("Order", id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get the original order to track status changes
                    var originalOrder = await _storageService.GetEntityAsync<Order>("Order", order.RowKey);
                    var previousStatus = originalOrder?.Status;

                    // Set DateTime Kind to UTC before updating
                    order.OrderDate = DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc);

                    await _storageService.UpdateEntityAsync(order);

                    // === QUEUE INTEGRATION: Send status update to queue if status changed ===
                    if (originalOrder != null && originalOrder.Status != order.Status)
                    {
                        var statusMessage = new
                        {
                            OrderId = order.OrderId,
                            CustomerId = order.CustomerId,
                            CustomerName = order.Username,
                            PreviousStatus = previousStatus,
                            NewStatus = order.Status,
                            UpdatedDate = DateTime.UtcNow,
                            UpdatedBy = "System"
                        };
                        await _storageService.SendMessageAsync("order-notifications", JsonSerializer.Serialize(statusMessage));
                        _logger.LogInformation("Order status update sent to queue: {OrderId} from {PreviousStatus} to {NewStatus}",
                            order.OrderId, previousStatus, order.Status);
                    }

                    TempData["Success"] = "Order updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating order: {ex.Message}");
                    _logger.LogError(ex, "Error updating order {OrderId}", order.OrderId);
                }
            }
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var order = await _storageService.GetEntityAsync<Order>("Order", id);
                if (order != null)
                {
                    // === QUEUE INTEGRATION: Send cancellation notification to queue ===
                    var cancelMessage = new
                    {
                        OrderId = order.OrderId,
                        CustomerId = order.CustomerId,
                        CustomerName = order.Username,
                        Action = "DELETE",
                        DeleteTime = DateTime.UtcNow
                    };
                    await _storageService.SendMessageAsync("order-notifications", JsonSerializer.Serialize(cancelMessage));
                    _logger.LogInformation("Order deletion notification sent to queue for Order ID: {OrderId}", order.OrderId);
                }

                await _storageService.DeleteEntityAsync<Order>("Order", id);
                TempData["Success"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
                _logger.LogError(ex, "Error deleting order {OrderId}", id);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            try
            {
                var product = await _storageService.GetEntityAsync<Product>("Product", productId);
                if (product != null)
                {
                    return Json(new
                    {
                        success = true,
                        price = product.Price,
                        stock = product.StockAvailable,
                        productName = product.ProductName
                    });
                }
                return Json(new { success = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product price for {ProductId}", productId);
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateOrderStatus(string id, string newStatus)
        {
            try
            {
                var order = await _storageService.GetEntityAsync<Order>("Order", id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                var previousStatus = order.Status;
                order.Status = newStatus;
                await _storageService.UpdateEntityAsync(order);

                // === QUEUE INTEGRATION: Send status update to queue ===
                var statusMessage = new
                {
                    OrderId = order.OrderId,
                    CustomerId = order.CustomerId,
                    CustomerName = order.Username,
                    PreviousStatus = previousStatus,
                    NewStatus = newStatus,
                    UpdatedDate = DateTime.UtcNow,
                    UpdatedBy = "System"
                };
                await _storageService.SendMessageAsync("order-notifications", JsonSerializer.Serialize(statusMessage));
                _logger.LogInformation("Order status update sent to queue via AJAX: {OrderId} to {NewStatus}", order.OrderId, newStatus);

                return Json(new { success = true, message = $"Order status updated to {newStatus}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for {OrderId}", id);
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task PopulateDropdowns(OrderCreateViewModel model)
        {
            model.Customers = await _storageService.GetAllEntitiesAsync<Customer>();
            model.Products = await _storageService.GetAllEntitiesAsync<Product>();
        }
    }
}