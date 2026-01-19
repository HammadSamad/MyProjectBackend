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
    public class OrderController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public OrderController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ================= CREATE =================
        // POST: api/Order
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrder model)
        {
            var order = new Order
            {
                UserId = model.UserId,
                TotalAmount = model.TotalAmount,
                PaymentMethodId = model.PaymentMethodId,
                OrderStatus = model.OrderStatus,
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // ---------------------------
            // Add to UserRecentOrder
            // ---------------------------
            var existingRecent = await _context.UserRecentOrders
                .FirstOrDefaultAsync(u => u.UserId == model.UserId && u.OrderId == order.OrderId);

            if (existingRecent != null)
            {
                existingRecent.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                var recentOrder = new UserRecentOrder
                {
                    UserId = model.UserId,
                    OrderId = order.OrderId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserRecentOrders.Add(recentOrder);
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Order created successfully", orderId = order.OrderId });
        }

        // ================= GET ALL =================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetAllOrders()
        {
            var orders = await _context.Orders
                .Select(o => new OrderDTO
                {
                    OrderId = o.OrderId,
                    UserId = o.UserId,
                    TotalAmount = o.TotalAmount,
                    PaymentMethodId = o.PaymentMethodId,
                    OrderStatus = o.OrderStatus,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt
                })
                .ToListAsync();

            return Ok(orders);
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDTO>> GetOrderById(long id)
        {
            var order = await _context.Orders
                .Where(o => o.OrderId == id)
                .Select(o => new OrderDTO
                {
                    OrderId = o.OrderId,
                    UserId = o.UserId,
                    TotalAmount = o.TotalAmount,
                    PaymentMethodId = o.PaymentMethodId,
                    OrderStatus = o.OrderStatus,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (order == null)
                return NotFound("Order not found");

            return Ok(order);
        }

        // ================= GET BY USER =================
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetOrdersByUser(int userId)
        {
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Select(o => new OrderDTO
                {
                    OrderId = o.OrderId,
                    UserId = o.UserId,
                    TotalAmount = o.TotalAmount,
                    PaymentMethodId = o.PaymentMethodId,
                    OrderStatus = o.OrderStatus,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt
                })
                .ToListAsync();

            return Ok(orders);
        }

        // ================= UPDATE =================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(long id, [FromBody] CreateOrder model)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound("Order not found");

            order.TotalAmount = model.TotalAmount;
            order.PaymentMethodId = model.PaymentMethodId;
            order.OrderStatus = model.OrderStatus;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Order updated successfully" });
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(long id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound("Order not found");

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order deleted successfully" });
        }

        // ================= UPDATE ORDER STATUS ONLY =================
        [HttpPatch("status/{id}")]
        public async Task<IActionResult> UpdateOrderStatus(long id, [FromBody] string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound("Order not found");

            order.OrderStatus = status;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Order status updated successfully" });
        }
    }
}
