using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderItemController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public OrderItemController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ================= CREATE =================
        // POST: api/OrderItem
        [HttpPost]
        public async Task<IActionResult> CreateOrderItem([FromBody] CreateOrderitem model)
        {
            var orderExists = await _context.Orders.AnyAsync(o => o.OrderId == model.OrderId);
            if (!orderExists)
                return BadRequest("Order does not exist");

            var variantExists = await _context.ProductVariants.AnyAsync(v => v.VariantId == model.VariantId);
            if (!variantExists)
                return BadRequest("Product Variant does not exist");

            var orderItem = new OrderItem
            {
                OrderId = model.OrderId,
                VariantId = model.VariantId,
                Quantity = model.Quantity,
                Price = model.Price,
                CreatedAt = DateTime.UtcNow
            };

            _context.OrderItems.Add(orderItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order item added successfully", orderItemId = orderItem.OrderItemId });
        }

        // ================= GET ALL =================
        // GET: api/OrderItem
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderItemDTO>>> GetAllOrderItems()
        {
            var items = await _context.OrderItems
                .Select(oi => new OrderItemDTO
                {
                    OrderItemId = oi.OrderItemId,
                    OrderId = oi.OrderId,
                    VariantId = oi.VariantId,
                    Quantity = oi.Quantity,
                    Price = oi.Price,
                    CreatedAt = oi.CreatedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        // ================= GET BY ID =================
        // GET: api/OrderItem/5
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderItemDTO>> GetOrderItemById(long id)
        {
            var item = await _context.OrderItems
                .Where(oi => oi.OrderItemId == id)
                .Select(oi => new OrderItemDTO
                {
                    OrderItemId = oi.OrderItemId,
                    OrderId = oi.OrderId,
                    VariantId = oi.VariantId,
                    Quantity = oi.Quantity,
                    Price = oi.Price,
                    CreatedAt = oi.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound("Order item not found");

            return Ok(item);
        }

        // ================= GET BY ORDER =================
        // GET: api/OrderItem/order/10
        [HttpGet("order/{orderId}")]
        public async Task<ActionResult<IEnumerable<OrderItemDTO>>> GetItemsByOrder(long orderId)
        {
            var items = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .Select(oi => new OrderItemDTO
                {
                    OrderItemId = oi.OrderItemId,
                    OrderId = oi.OrderId,
                    VariantId = oi.VariantId,
                    Quantity = oi.Quantity,
                    Price = oi.Price,
                    CreatedAt = oi.CreatedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        // ================= UPDATE =================
        // PUT: api/OrderItem/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrderItem(long id, [FromBody] CreateOrderitem model)
        {
            var orderItem = await _context.OrderItems.FindAsync(id);
            if (orderItem == null)
                return NotFound("Order item not found");

            orderItem.VariantId = model.VariantId;
            orderItem.Quantity = model.Quantity;
            orderItem.Price = model.Price;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Order item updated successfully" });
        }

        // ================= DELETE =================
        // DELETE: api/OrderItem/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrderItem(long id)
        {
            var orderItem = await _context.OrderItems.FindAsync(id);
            if (orderItem == null)
                return NotFound("Order item not found");

            _context.OrderItems.Remove(orderItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order item deleted successfully" });
        }
    }
}
