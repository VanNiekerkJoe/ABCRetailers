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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(string id)
        {
            try
            {
                var product = await _storageService.GetEntityAsync<Product>("Product", id);
                if (product == null)
                {
                    return NotFound($"Product with ID {id} not found");
                }
                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product {ProductId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ListProducts()
        {
            try
            {
                var products = await _storageService.GetAllEntitiesAsync<Product>();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing products");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(string id, Product product)
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
                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}