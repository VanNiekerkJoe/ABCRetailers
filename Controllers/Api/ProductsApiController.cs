using ABCRetailers.Models;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ABCRetailers.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IAzureStorageService storageService, ILogger<ProductsController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        // GET: api/products/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(string id)
        {
            try
            {
                var product = await _storageService.GetEntityAsync<Product>("Product", id);
                if (product == null)
                {
                    return NotFound($"Product with ID {id} not found");
                }
                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product {ProductId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/products
        [HttpGet]
        public async Task<ActionResult<List<Product>>> ListProducts()
        {
            try
            {
                var products = await _storageService.GetAllEntitiesAsync<Product>();
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing products");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/products/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<Product>> UpdateProduct(string id, Product product)
        {
            try
            {
                if (id != product.RowKey)
                {
                    return BadRequest("Product ID mismatch");
                }

                var existingProduct = await _storageService.GetEntityAsync<Product>("Product", id);
                if (existingProduct == null)
                {
                    return NotFound($"Product with ID {id} not found");
                }

                await _storageService.UpdateEntityAsync(product);
                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}