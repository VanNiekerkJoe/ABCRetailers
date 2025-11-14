using ABCRetailers.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailers.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersApiController : ControllerBase
    {
        private readonly AuthDbContext _db; // Fixed: Correct class name
        public OrdersApiController(AuthDbContext db) // Fixed: Correct parameter type
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var orders = await _db.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Customer)
                .ToListAsync();
            return Ok(orders);
        }

        [HttpPost("process/{id}")]
        public async Task<IActionResult> Process(string id)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound("Order not found");

            order.OrderStatus = "Processing";
            await _db.SaveChangesAsync();
            return Ok(new { message = "Order processed", orderId = id });
        }

        [HttpPost("update-status/{id}")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] string status)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound("Order not found");

            var validStatuses = new[] { "Pending", "Confirmed", "Processing", "Shipped", "Delivered", "Cancelled", "Refunded" };
            if (!validStatuses.Contains(status))
            {
                return BadRequest("Invalid status");
            }

            order.OrderStatus = status;
            await _db.SaveChangesAsync();
            return Ok(new { message = $"Order status updated to {status}", orderId = id });
        }

        [HttpPost("cancel/{id}")]
        public async Task<IActionResult> Cancel(string id)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound("Order not found");

            order.OrderStatus = "Cancelled";
            order.CancelledAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Order cancelled", orderId = id });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return Ok(order);
        }
    }
}