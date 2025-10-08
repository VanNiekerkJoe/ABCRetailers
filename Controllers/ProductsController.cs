using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ABCRetailers.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IAzureStorageService storageService, ILogger<ProductController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _storageService.GetAllEntitiesAsync<Product>();
            return View(products);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            // Manual price parsing for edit too
            if (Request.Form.TryGetValue("Price", out var priceFormValue))
            {
                if (decimal.TryParse(priceFormValue, out var parsedPrice))
                {
                    product.Price = parsedPrice;
                    _logger.LogInformation("Raw price from form: {PriceFormValue}", priceFormValue.ToString());
                    _logger.LogInformation("Successfully parsed price: {Price}", parsedPrice);
                }
                else
                {
                    _logger.LogWarning("Failed to parse price: {PriceFormValue}", priceFormValue.ToString());
                }
            }

            _logger.LogInformation("Final product price: {Price}", product.Price);

            if (ModelState.IsValid)
            {
                try
                {
                    if (product.Price <= 0)
                    {
                        ModelState.AddModelError("Price", "Price must be greater than $0.00");
                        return View(product);
                    }

                    // Upload image if provided
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageUrl = await _storageService.UploadImageAsync(imageFile, "product-images");
                        product.ImageUrl = imageUrl;

                        // === QUEUE INTEGRATION: Send image processing message to queue ===
                        var imageMessage = new
                        {
                            ProductId = product.RowKey,
                            ProductName = product.ProductName,
                            ImageUrl = imageUrl,
                            Action = "CREATE",
                            ProcessTime = DateTime.UtcNow,
                            FileName = imageFile.FileName,
                            FileSize = imageFile.Length
                        };
                        await _storageService.SendMessageAsync("image-processing", JsonSerializer.Serialize(imageMessage));
                        _logger.LogInformation("Image processing message sent to queue for Product: {ProductName}", product.ProductName);
                    }

                    await _storageService.AddEntityAsync(product);

                    // === QUEUE INTEGRATION: Send product creation notification ===
                    var productMessage = new
                    {
                        ProductId = product.RowKey,
                        ProductName = product.ProductName,
                        Price = product.Price,
                        Stock = product.StockAvailable,
                        Action = "CREATE",
                        CreatedTime = DateTime.UtcNow
                    };
                    await _storageService.SendMessageAsync("product-updates", JsonSerializer.Serialize(productMessage));
                    _logger.LogInformation("Product creation notification sent to queue for: {ProductName}", product.ProductName);

                    TempData["Success"] = $"Product '{product.ProductName}' created successfully with price {product.Price:C}!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product.");
                    ModelState.AddModelError("", $"Error creating product: {ex.Message}");
                }
            }
            return View(product);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var product = await _storageService.GetEntityAsync<Product>("Product", id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {
            // Manual price parsing for edit too
            if (Request.Form.TryGetValue("Price", out var priceFormValue))
            {
                if (decimal.TryParse(priceFormValue, out var parsedPrice))
                {
                    product.Price = parsedPrice;
                    _logger.LogInformation("Edit: Successfully parsed price: {Price}", parsedPrice);
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the original product to preserve ETag
                    var originalProduct = await _storageService.GetEntityAsync<Product>("Product", product.RowKey);
                    if (originalProduct == null)
                    {
                        return NotFound();
                    }

                    // Track if stock changed for queue message
                    var stockChanged = originalProduct.StockAvailable != product.StockAvailable;
                    var previousStock = originalProduct.StockAvailable;

                    // Update properties but keep the original ETag
                    originalProduct.ProductName = product.ProductName;
                    originalProduct.Description = product.Description;
                    originalProduct.Price = product.Price;
                    originalProduct.StockAvailable = product.StockAvailable;

                    // Upload new image if provided
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageUrl = await _storageService.UploadImageAsync(imageFile, "product-images");
                        originalProduct.ImageUrl = imageUrl;

                        // === QUEUE INTEGRATION: Send image update message to queue ===
                        var imageMessage = new
                        {
                            ProductId = product.RowKey,
                            ProductName = product.ProductName,
                            ImageUrl = imageUrl,
                            Action = "UPDATE",
                            ProcessTime = DateTime.UtcNow,
                            FileName = imageFile.FileName,
                            FileSize = imageFile.Length
                        };
                        await _storageService.SendMessageAsync("image-processing", JsonSerializer.Serialize(imageMessage));
                        _logger.LogInformation("Image update message sent to queue for Product: {ProductName}", product.ProductName);
                    }

                    await _storageService.UpdateEntityAsync(originalProduct);

                    // === QUEUE INTEGRATION: Send product update notification ===
                    var productMessage = new
                    {
                        ProductId = product.RowKey,
                        ProductName = product.ProductName,
                        Price = product.Price,
                        PreviousStock = previousStock,
                        NewStock = product.StockAvailable,
                        StockChanged = stockChanged,
                        Action = "UPDATE",
                        UpdatedTime = DateTime.UtcNow
                    };
                    await _storageService.SendMessageAsync("product-updates", JsonSerializer.Serialize(productMessage));
                    _logger.LogInformation("Product update notification sent to queue for: {ProductName}", product.ProductName);

                    TempData["Success"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating product: {Message}", ex.Message);
                    ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                }
            }
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var product = await _storageService.GetEntityAsync<Product>("Product", id);
                if (product != null)
                {
                    // === QUEUE INTEGRATION: Send product deletion notification ===
                    var deleteMessage = new
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        Action = "DELETE",
                        DeletedTime = DateTime.UtcNow
                    };
                    await _storageService.SendMessageAsync("product-updates", JsonSerializer.Serialize(deleteMessage));
                    _logger.LogInformation("Product deletion notification sent to queue for: {ProductName}", product.ProductName);
                }

                await _storageService.DeleteEntityAsync<Product>("Product", id);
                TempData["Success"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting product: {ex.Message}";
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}