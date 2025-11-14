using ABCRetailers.Data;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers.Controllers
{
    public class ProductController : Controller
    {
        private readonly AuthDbContext _db;
        private readonly ILogger<ProductController> _logger;

        public ProductController(AuthDbContext db, ILogger<ProductController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IActionResult> Index(ProductSearchViewModel searchModel)
        {
            try
            {
                var username = HttpContext.Session.GetString("Username");
                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Index", "Login");
                }

                var productsQuery = _db.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive && p.IsPublished)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(searchModel.Search))
                {
                    productsQuery = productsQuery.Where(p =>
                        p.Name.Contains(searchModel.Search) || // FIXED: ProductName -> Name
                        p.Description.Contains(searchModel.Search) ||
                        p.Brand.Contains(searchModel.Search) ||
                        (p.Category != null && p.Category.Name.Contains(searchModel.Search))); // FIXED: Handle Category as object
                }

                // Apply category filter
                if (!string.IsNullOrEmpty(searchModel.Category) && searchModel.Category != "All")
                {
                    productsQuery = productsQuery.Where(p =>
                        p.Category != null && p.Category.Name == searchModel.Category); // FIXED: Handle Category as object
                }

                // Apply price range filter
                if (searchModel.MinPrice.HasValue)
                {
                    productsQuery = productsQuery.Where(p => p.Price >= searchModel.MinPrice.Value);
                }

                if (searchModel.MaxPrice.HasValue)
                {
                    productsQuery = productsQuery.Where(p => p.Price <= searchModel.MaxPrice.Value);
                }

                // Apply sorting
                productsQuery = searchModel.SortBy switch
                {
                    "price_low" => productsQuery.OrderBy(p => p.Price),
                    "price_high" => productsQuery.OrderByDescending(p => p.Price),
                    "name" => productsQuery.OrderBy(p => p.Name), // FIXED: ProductName -> Name
                    "newest" => productsQuery.OrderByDescending(p => p.CreatedAt),
                    "rating" => productsQuery.OrderByDescending(p => p.AverageRating),
                    _ => productsQuery.OrderBy(p => p.Name) // FIXED: ProductName -> Name
                };

                var products = await productsQuery.ToListAsync();

                // Get categories for filter dropdown
                var categories = await _db.Categories
                    .Where(c => c.IsActive)
                    .Select(c => c.Name) // FIXED: Get category names as strings
                    .Distinct()
                    .ToListAsync();

                var viewModel = new ProductSearchViewModel
                {
                    Search = searchModel.Search,
                    Category = searchModel.Category,
                    SortBy = searchModel.SortBy,
                    MinPrice = searchModel.MinPrice,
                    MaxPrice = searchModel.MaxPrice,
                    Products = products,
                    Categories = categories
                };

                ViewBag.IsAdmin = HttpContext.Session.GetString("Role") == "Admin";
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products");
                TempData["ErrorMessage"] = "Error loading products. Please try again.";
                return View(new ProductSearchViewModel());
            }
        }

        public async Task<IActionResult> Details(string id)
        {
            try
            {
                var product = await _db.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive && p.IsPublished);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found!";
                    return RedirectToAction("Index");
                }

                // Get related products (same category)
                var relatedProducts = await _db.Products
                    .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsActive && p.IsPublished)
                    .Take(4)
                    .ToListAsync();

                // Get featured products
                var featuredProducts = await _db.Products
                    .Where(p => p.IsFeatured && p.IsActive && p.IsPublished && p.Id != product.Id)
                    .Take(4)
                    .ToListAsync();

                ViewBag.RelatedProducts = relatedProducts;
                ViewBag.FeaturedProducts = featuredProducts;
                ViewBag.IsAdmin = HttpContext.Session.GetString("Role") == "Admin";

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product details for {ProductId}", id);
                TempData["ErrorMessage"] = "Error loading product details. Please try again.";
                return RedirectToAction("Index");
            }
        }

        // Admin-only actions
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductViewModel model)
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
                    // Find or create category
                    var category = await _db.Categories
                        .FirstOrDefaultAsync(c => c.Name == model.Category);

                    if (category == null)
                    {
                        category = new Category
                        {
                            Name = model.Category,
                            Description = model.Category,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.Categories.Add(category);
                        await _db.SaveChangesAsync();
                    }

                    var product = new Product
                    {
                        Name = model.ProductName, // FIXED: ProductName -> Name
                        Description = model.Description,
                        Price = model.Price,
                        StockQuantity = model.StockAvailable, // FIXED: StockAvailable -> StockQuantity
                        MainImageUrl = model.ImageUrl, // FIXED: ImageUrl -> MainImageUrl
                        CategoryId = category.Id, // FIXED: Use CategoryId instead of string Category
                        Brand = model.Brand,
                        Weight = model.Weight,
                        Dimensions = model.Dimensions,
                        SKU = model.SKU,
                        IsFeatured = model.IsFeatured,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _db.Products.Add(product);
                    await _db.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Product created successfully!";
                    return RedirectToAction("Index");
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                TempData["ErrorMessage"] = "Error creating product. Please try again.";
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(string id)
        {
            try
            {
                var role = HttpContext.Session.GetString("Role");
                if (role != "Admin")
                {
                    TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                    return RedirectToAction("Index");
                }

                var product = await _db.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == id);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found!";
                    return RedirectToAction("Index");
                }

                var model = new ProductViewModel
                {
                    Id = product.Id,
                    ProductName = product.Name, // FIXED: ProductName -> Name
                    Description = product.Description,
                    Price = product.Price,
                    StockAvailable = product.StockQuantity, // FIXED: StockAvailable -> StockQuantity
                    ImageUrl = product.MainImageUrl, // FIXED: ImageUrl -> MainImageUrl
                    Category = product.Category?.Name ?? "General", // FIXED: Get category name
                    Brand = product.Brand,
                    Weight = product.Weight,
                    Dimensions = product.Dimensions,
                    SKU = product.SKU,
                    IsFeatured = product.IsFeatured
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product for edit");
                TempData["ErrorMessage"] = "Error loading product. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductViewModel model)
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
                    var product = await _db.Products.FindAsync(model.Id);
                    if (product == null)
                    {
                        TempData["ErrorMessage"] = "Product not found!";
                        return RedirectToAction("Index");
                    }

                    // Find or create category
                    var category = await _db.Categories
                        .FirstOrDefaultAsync(c => c.Name == model.Category);

                    if (category == null)
                    {
                        category = new Category
                        {
                            Name = model.Category,
                            Description = model.Category,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.Categories.Add(category);
                        await _db.SaveChangesAsync();
                    }

                    product.Name = model.ProductName; // FIXED: ProductName -> Name
                    product.Description = model.Description;
                    product.Price = model.Price;
                    product.StockQuantity = model.StockAvailable; // FIXED: StockAvailable -> StockQuantity
                    product.MainImageUrl = model.ImageUrl; // FIXED: ImageUrl -> MainImageUrl
                    product.CategoryId = category.Id; // FIXED: Use CategoryId
                    product.Brand = model.Brand;
                    product.Weight = model.Weight;
                    product.Dimensions = model.Dimensions;
                    product.SKU = model.SKU;
                    product.IsFeatured = model.IsFeatured;
                    product.UpdatedAt = DateTime.UtcNow;

                    await _db.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Product updated successfully!";
                    return RedirectToAction("Index");
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                TempData["ErrorMessage"] = "Error updating product. Please try again.";
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var role = HttpContext.Session.GetString("Role");
                if (role != "Admin")
                {
                    TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                    return RedirectToAction("Index");
                }

                var product = await _db.Products.FindAsync(id);
                if (product != null)
                {
                    // Soft delete by setting IsActive to false
                    product.IsActive = false;
                    product.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Product deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Product not found!";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product");
                TempData["ErrorMessage"] = "Error deleting product. Please try again.";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> Featured()
        {
            try
            {
                var username = HttpContext.Session.GetString("Username");
                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Index", "Login");
                }

                var featuredProducts = await _db.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsFeatured && p.IsActive && p.IsPublished)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                ViewBag.IsAdmin = HttpContext.Session.GetString("Role") == "Admin";
                return View(featuredProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading featured products");
                TempData["ErrorMessage"] = "Error loading featured products. Please try again.";
                return View(new List<Product>());
            }
        }
    }
}