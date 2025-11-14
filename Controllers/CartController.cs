using ABCRetailers.Data;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers.Controllers
{
    public class CartController : Controller
    {
        private readonly AuthDbContext _db;
        private readonly ILogger<CartController> _logger;

        public CartController(AuthDbContext db, ILogger<CartController> logger)
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

                var customerId = HttpContext.Session.GetString("CustomerId");
                if (string.IsNullOrEmpty(customerId) || !int.TryParse(customerId, out int custId))
                {
                    TempData["ErrorMessage"] = "Please log in as a customer to view cart.";
                    return RedirectToAction("Index", "Home");
                }

                var cartItems = await _db.Carts
                    .Include(c => c.Product)
                    .Where(c => c.CustomerId == custId)
                    .ToListAsync();

                var cartViewModel = new CartViewModel
                {
                    Items = cartItems.Select(c => new CartItemViewModel
                    {
                        CartId = c.Id,
                        ProductId = c.ProductId,
                        ProductName = c.Product.Name,
                        ProductDescription = c.Product.Description,
                        Price = c.UnitPrice,
                        Quantity = c.Quantity,
                        ImageUrl = c.Product.MainImageUrl,
                        MaxQuantity = Math.Min(c.Product.StockQuantity, 20)
                    }).ToList()
                };

                return View(cartViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart");
                TempData["ErrorMessage"] = "Error loading cart. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(string productId, int quantity = 1)
        {
            try
            {
                var customerId = HttpContext.Session.GetString("CustomerId");
                if (string.IsNullOrEmpty(customerId) || !int.TryParse(customerId, out int custId))
                {
                    TempData["ErrorMessage"] = "Please log in as a customer to add items to cart.";
                    return RedirectToAction("Index", "Login");
                }

                var product = await _db.Products.FindAsync(productId);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found.";
                    return RedirectToAction("Index", "Product");
                }

                if (quantity > product.StockQuantity)
                {
                    TempData["ErrorMessage"] = $"Only {product.StockQuantity} units available.";
                    return RedirectToAction("Details", "Product", new { id = productId });
                }

                var existingCartItem = await _db.Carts
                    .FirstOrDefaultAsync(c => c.CustomerId == custId && c.ProductId == productId);

                if (existingCartItem != null)
                {
                    existingCartItem.Quantity += quantity;
                    if (existingCartItem.Quantity > product.StockQuantity)
                    {
                        existingCartItem.Quantity = product.StockQuantity;
                        TempData["WarningMessage"] = $"Quantity adjusted to maximum available stock ({product.StockQuantity}).";
                    }
                    existingCartItem.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var cartItem = new Cart
                    {
                        CustomerId = custId,
                        ProductId = productId,
                        Quantity = quantity,
                        UnitPrice = product.Price,
                        AddedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.Carts.Add(cartItem);
                }

                await _db.SaveChangesAsync();
                await UpdateCartCountInSession(custId);

                TempData["SuccessMessage"] = $"{product.Name} added to cart!";
                return RedirectToAction("Details", "Product", new { id = productId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to cart");
                TempData["ErrorMessage"] = "Error adding item to cart. Please try again.";
                return RedirectToAction("Index", "Product");
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCart(int cartId, int quantity)
        {
            try
            {
                var cartItem = await _db.Carts
                    .Include(c => c.Product)
                    .FirstOrDefaultAsync(c => c.Id == cartId);

                if (cartItem == null)
                {
                    TempData["ErrorMessage"] = "Cart item not found.";
                    return RedirectToAction("Index");
                }

                var customerId = HttpContext.Session.GetString("CustomerId");
                if (string.IsNullOrEmpty(customerId) || !int.TryParse(customerId, out int custId) || cartItem.CustomerId != custId)
                {
                    TempData["ErrorMessage"] = "Access denied.";
                    return RedirectToAction("Index");
                }

                if (quantity > cartItem.Product.StockQuantity)
                {
                    TempData["ErrorMessage"] = $"Only {cartItem.Product.StockQuantity} units available.";
                    return RedirectToAction("Index");
                }

                if (quantity <= 0)
                {
                    _db.Carts.Remove(cartItem);
                }
                else
                {
                    cartItem.Quantity = quantity;
                    cartItem.UnitPrice = cartItem.Product.Price;
                    cartItem.UpdatedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();
                await UpdateCartCountInSession(custId);

                TempData["SuccessMessage"] = "Cart updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart");
                TempData["ErrorMessage"] = "Error updating cart. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int cartId)
        {
            try
            {
                var cartItem = await _db.Carts.FindAsync(cartId);
                if (cartItem != null)
                {
                    var customerId = HttpContext.Session.GetString("CustomerId");
                    if (string.IsNullOrEmpty(customerId) || !int.TryParse(customerId, out int custId) || cartItem.CustomerId != custId)
                    {
                        TempData["ErrorMessage"] = "Access denied.";
                        return RedirectToAction("Index");
                    }

                    _db.Carts.Remove(cartItem);
                    await _db.SaveChangesAsync();
                    await UpdateCartCountInSession(custId);

                    TempData["SuccessMessage"] = "Item removed from cart!";
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from cart");
                TempData["ErrorMessage"] = "Error removing item from cart. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var customerId = HttpContext.Session.GetString("CustomerId");
                if (string.IsNullOrEmpty(customerId) || !int.TryParse(customerId, out int custId))
                {
                    TempData["ErrorMessage"] = "Please log in as a customer to manage cart.";
                    return RedirectToAction("Index", "Login");
                }

                var cartItems = await _db.Carts
                    .Where(c => c.CustomerId == custId)
                    .ToListAsync();

                if (cartItems.Any())
                {
                    _db.Carts.RemoveRange(cartItems);
                    await _db.SaveChangesAsync();
                    await UpdateCartCountInSession(custId);
                    TempData["SuccessMessage"] = "Cart cleared successfully!";
                }
                else
                {
                    TempData["InfoMessage"] = "Your cart is already empty.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                TempData["ErrorMessage"] = "Error clearing cart. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var customerId = HttpContext.Session.GetString("CustomerId");
                if (string.IsNullOrEmpty(customerId) || !int.TryParse(customerId, out int custId))
                {
                    TempData["ErrorMessage"] = "Please log in as a customer to checkout.";
                    return RedirectToAction("Index", "Login");
                }

                var cartItems = await _db.Carts
                    .Include(c => c.Product)
                    .Where(c => c.CustomerId == custId)
                    .ToListAsync();

                if (!cartItems.Any())
                {
                    TempData["ErrorMessage"] = "Your cart is empty.";
                    return RedirectToAction("Index");
                }

                var outOfStockItems = cartItems.Where(c => c.Quantity > c.Product.StockQuantity).ToList();
                if (outOfStockItems.Any())
                {
                    var productNames = string.Join(", ", outOfStockItems.Select(c => c.Product.Name));
                    TempData["ErrorMessage"] = $"Insufficient stock for: {productNames}. Please update your cart.";
                    return RedirectToAction("Index");
                }

                // Get customer details for shipping address
                var customer = await _db.Customers.FindAsync(custId);
                var shippingAddress = customer?.DefaultShippingAddress ?? "Not specified";

                var order = new Order
                {
                    OrderNumber = $"ORD{DateTime.UtcNow.Ticks}",
                    CustomerId = custId,
                    OrderStatus = "Pending",
                    PaymentStatus = "Pending",
                    PaymentMethod = "Credit Card",
                    Currency = "ZAR",
                    Subtotal = cartItems.Sum(c => c.UnitPrice * c.Quantity),
                    TaxAmount = cartItems.Sum(c => c.UnitPrice * c.Quantity) * 0.15m, // 15% tax
                    ShippingAmount = 0,
                    DiscountAmount = 0,
                    TotalAmount = cartItems.Sum(c => c.UnitPrice * c.Quantity) * 1.15m,
                    ShippingAddress = shippingAddress,
                    BillingAddress = shippingAddress,
                    OrderedAt = DateTime.UtcNow
                };

                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                foreach (var cartItem in cartItems)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = cartItem.ProductId,
                        ProductName = cartItem.Product.Name,
                        ProductSku = cartItem.Product.SKU,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.UnitPrice,
                        TotalPrice = cartItem.UnitPrice * cartItem.Quantity
                    };
                    _db.OrderItems.Add(orderItem);

                    cartItem.Product.StockQuantity -= cartItem.Quantity;
                }

                _db.Carts.RemoveRange(cartItems);
                await _db.SaveChangesAsync();
                await UpdateCartCountInSession(custId);

                TempData["SuccessMessage"] = "Order placed successfully! Thank you for your purchase.";
                return RedirectToAction("OrderHistory");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout");
                TempData["ErrorMessage"] = "Error during checkout. Please try again.";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> OrderHistory()
        {
            try
            {
                var customerId = HttpContext.Session.GetString("CustomerId");
                if (string.IsNullOrEmpty(customerId) || !int.TryParse(customerId, out int custId))
                {
                    TempData["ErrorMessage"] = "Please log in as a customer to view order history.";
                    return RedirectToAction("Index", "Login");
                }

                var orders = await _db.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .Where(o => o.CustomerId == custId)
                    .OrderByDescending(o => o.OrderedAt)
                    .ToListAsync();

                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order history");
                TempData["ErrorMessage"] = "Error loading order history. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToCartFromIndex(string productId, int quantity = 1)
        {
            try
            {
                var customerId = HttpContext.Session.GetString("CustomerId");
                if (string.IsNullOrEmpty(customerId) || !int.TryParse(customerId, out int custId))
                {
                    TempData["ErrorMessage"] = "Please log in as a customer to add items to cart.";
                    return RedirectToAction("Index", "Login");
                }

                var product = await _db.Products.FindAsync(productId);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found.";
                    return RedirectToAction("Index", "Product");
                }

                if (quantity > product.StockQuantity)
                {
                    TempData["ErrorMessage"] = $"Only {product.StockQuantity} units available.";
                    return RedirectToAction("Index", "Product");
                }

                var existingCartItem = await _db.Carts
                    .FirstOrDefaultAsync(c => c.CustomerId == custId && c.ProductId == productId);

                if (existingCartItem != null)
                {
                    existingCartItem.Quantity += quantity;
                    if (existingCartItem.Quantity > product.StockQuantity)
                    {
                        existingCartItem.Quantity = product.StockQuantity;
                        TempData["WarningMessage"] = $"Quantity adjusted to maximum available stock ({product.StockQuantity}).";
                    }
                    existingCartItem.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var cartItem = new Cart
                    {
                        CustomerId = custId,
                        ProductId = productId,
                        Quantity = quantity,
                        UnitPrice = product.Price,
                        AddedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.Carts.Add(cartItem);
                }

                await _db.SaveChangesAsync();
                await UpdateCartCountInSession(custId);

                TempData["SuccessMessage"] = $"{product.Name} added to cart!";
                return RedirectToAction("Index", "Product");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to cart from index");
                TempData["ErrorMessage"] = "Error adding item to cart. Please try again.";
                return RedirectToAction("Index", "Product");
            }
        }

        private async Task UpdateCartCountInSession(int customerId)
        {
            try
            {
                var cartCount = await _db.Carts
                    .Where(c => c.CustomerId == customerId)
                    .SumAsync(c => c.Quantity);

                HttpContext.Session.SetInt32("CartCount", cartCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart count in session");
            }
        }
    }
}