using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrder model)
        {
            if (model == null)
                return BadRequest(new { message = "Order data is required." });

            if (model.UserId <= 0)
                return BadRequest(new { message = "Invalid User ID." });

            if (model.TotalAmount <= 0)
                return BadRequest(new { message = "Total amount must be greater than zero." });

            try
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

                // -------- User Recent Orders --------
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

                return Ok(new
                {
                    success = true,
                    message = "Order created successfully.",
                    orderId = order.OrderId
                });
            }
            catch
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while creating the order. Please try again."
                });
            }
        }

        // ================= GET ALL =================
        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            try
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

                if (!orders.Any())
                    return NotFound(new { message = "No orders found." });

                return Ok(orders);
            }
            catch
            {
                return StatusCode(500, new { message = "Failed to fetch orders." });
            }
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(long id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid order ID." });

            try
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
                    return NotFound(new { message = "Order not found." });

                return Ok(order);
            }
            catch
            {
                return StatusCode(500, new { message = "Failed to fetch the order." });
            }
        }

        // ================= GET BY USER =================
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = "Invalid user ID." });

            try
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

                if (!orders.Any())
                    return NotFound(new { message = "No orders found for this user." });

                return Ok(orders);
            }
            catch
            {
                return StatusCode(500, new { message = "Failed to fetch user orders." });
            }
        }

        // ================= UPDATE =================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(long id, [FromBody] CreateOrder model)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid order ID." });

            if (model == null)
                return BadRequest(new { message = "Order data is required." });

            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                    return NotFound(new { message = "Order not found." });

                order.TotalAmount = model.TotalAmount;
                order.PaymentMethodId = model.PaymentMethodId;
                order.OrderStatus = model.OrderStatus;
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Order updated successfully." });
            }
            catch
            {
                return StatusCode(500, new { message = "Failed to update order." });
            }
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(long id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid order ID." });

            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                    return NotFound(new { message = "Order not found." });

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Order deleted successfully." });
            }
            catch
            {
                return StatusCode(500, new { message = "Failed to delete order." });
            }
        }

        // ================= UPDATE STATUS ONLY =================
        [HttpPatch("status/{id}")]
        public async Task<IActionResult> UpdateOrderStatus(long id, [FromBody] string status)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid order ID." });

            if (string.IsNullOrWhiteSpace(status))
                return BadRequest(new { message = "Order status is required." });

            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                    return NotFound(new { message = "Order not found." });

                order.OrderStatus = status;
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Order status updated successfully." });
            }
            catch
            {
                return StatusCode(500, new { message = "Failed to update order status." });
            }
        }
    }
}
