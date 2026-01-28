using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Backend_Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;
        private readonly IEmailService _emailService;

        public OrderController(LaptopHarbourDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ================= CREATE ORDER =================
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrder model)
        {
            if (model == null) return BadRequest(new { message = "Order data is required." });
            if (model.UserId <= 0) return BadRequest(new { message = "Invalid User ID." });
            if (model.TotalAmount <= 0) return BadRequest(new { message = "Total amount must be greater than zero." });

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

                // ---------------- User Recent Orders ----------------
                var existingRecent = await _context.UserRecentOrders
                    .FirstOrDefaultAsync(u => u.UserId == model.UserId && u.OrderId == order.OrderId);

                if (existingRecent != null)
                    existingRecent.CreatedAt = DateTime.UtcNow;
                else
                    _context.UserRecentOrders.Add(new UserRecentOrder
                    {
                        UserId = model.UserId,
                        OrderId = order.OrderId,
                        CreatedAt = DateTime.UtcNow
                    });

                await _context.SaveChangesAsync();

                // ---------------- Notify Admin ----------------
                var adminUsers = await _context.UserRoles
                    .Where(ur => ur.Role.RoleName == "Admin")
                    .Select(ur => ur.User)
                    .Distinct()
                    .ToListAsync();

                foreach (var admin in adminUsers)
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserId = admin.UserId,
                        Title = "New Order Received",
                        Message = $"Order #{order.OrderId} has been placed by user #{order.UserId}.",
                        Type = "Order",
                        TargetAudience = "Admin",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
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

        // ================= UPDATE ORDER STATUS =================
        [HttpPatch("status/{id}")]
        public async Task<IActionResult> UpdateOrderStatus(long id, [FromBody] string status)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid order ID." });
            if (string.IsNullOrWhiteSpace(status)) return BadRequest(new { message = "Order status is required." });

            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null) return NotFound(new { message = "Order not found." });

                order.OrderStatus = status;
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // If order is cancelled, notify admin
                if (status.ToLower() == "cancelled" || status.ToLower() == "canceled")
                {
                    var adminUsers = await _context.UserRoles
                        .Where(ur => ur.Role.RoleName == "Admin")
                        .Select(ur => ur.User)
                        .Distinct()
                        .ToListAsync();

                    foreach (var admin in adminUsers)
                    {
                        _context.Notifications.Add(new Notification
                        {
                            UserId = admin.UserId,
                            Title = "Order Cancelled",
                            Message = $"Order #{order.OrderId} has been cancelled by user #{order.UserId}.",
                            Type = "OrderCancel",
                            TargetAudience = "Admin",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = "Order status updated successfully." });
            }
            catch
            {
                return StatusCode(500, new { message = "Failed to update order status." });
            }
        }

        // ================= MARK NOTIFICATION AS READ =================
        [HttpPut("notification/mark-read/{notificationId}")]
        public async Task<IActionResult> MarkNotificationAsRead(long notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null) return NotFound(new { message = "Notification not found." });

            if (notification.IsRead == true) return Ok(new { message = "Already marked as read." });

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            // Send email to user after admin sees it
            if (!string.IsNullOrWhiteSpace(notification.Message))
            {
                if (notification.Type == "Order")
                    await SendOrderEmailToUser(notification.Message);
                else if (notification.Type == "OrderCancel")
                    await SendOrderCancelEmailToUser(notification.Message);
            }

            return Ok(new { message = "Notification marked as read and user notified." });
        }

        // ================= SEND ORDER EMAIL =================
        private async Task SendOrderEmailToUser(string notificationMessage)
        {
            try
            {
                var parts = notificationMessage.Split(' ');
                if (!long.TryParse(parts[1].TrimStart('#'), out var orderId)) return;

                var order = await _context.Orders
                    .Where(o => o.OrderId == orderId)
                    .Include(o => o.User)
                    .FirstOrDefaultAsync();

                if (order == null || order.User == null || string.IsNullOrWhiteSpace(order.User.Email))
                    return;

                var orderItems = await _context.OrderItems
                    .Where(oi => oi.OrderId == orderId)
                    .Include(oi => oi.Variant)
                    .ToListAsync();

                string body = $"<h3>We have received your order #{order.OrderId}</h3>";
                body += $"<p>Total Amount: {order.TotalAmount:C}</p>";
                body += "<h4>Order Items:</h4><ul>";

                foreach (var item in orderItems)
                    body += $"<li>{item.Variant?.Product.ProductName} - {item.Quantity} x {item.Price:C} = {item.Quantity * item.Price:C}</li>";

                body += "</ul><p>Thank you for shopping with us!</p>";

                await _emailService.SendEmailAsync(order.User.Email, "Your Order has been received", body);
            }
            catch
            {
                // Optional: log error
            }
        }

        // ================= SEND ORDER CANCEL EMAIL =================
        private async Task SendOrderCancelEmailToUser(string notificationMessage)
        {
            try
            {
                var parts = notificationMessage.Split(' ');
                if (!long.TryParse(parts[1].TrimStart('#'), out var orderId)) return;

                var order = await _context.Orders
                    .Where(o => o.OrderId == orderId)
                    .Include(o => o.User)
                    .FirstOrDefaultAsync();

                if (order == null || order.User == null || string.IsNullOrWhiteSpace(order.User.Email))
                    return;

                string body = $"<h3>Your order #{order.OrderId} has been cancelled</h3>";
                body += "<p>We have processed the cancellation of your order.</p>";
                body += "<p>If you have already made payment, the refund will be processed as per our policy.</p>";
                body += "<p>Thank you!</p>";

                await _emailService.SendEmailAsync(order.User.Email, "Order Cancelled", body);
            }
            catch
            {
                // Optional: log error
            }
        }

        // ================= GET ALL ORDERS =================
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

                if (!orders.Any()) return NotFound(new { message = "No orders found." });

                return Ok(orders);
            }
            catch
            {
                return StatusCode(500, new { message = "Failed to fetch orders." });
            }
        }

        // ================= GET ORDER BY ID =================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(long id)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid order ID." });

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

                if (order == null) return NotFound(new { message = "Order not found." });

                return Ok(order);
            }
            catch
            {
                return StatusCode(500, new { message = "Failed to fetch the order." });
            }
        }

        // ================= GET ORDERS BY USER =================
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(int userId)
        {
            if (userId <= 0) return BadRequest(new { message = "Invalid user ID." });

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

                if (!orders.Any()) return NotFound(new { message = "No orders found for this user." });

                return Ok(orders);
            }
            catch
            {
                return StatusCode(500, new { message = "Failed to fetch user orders." });
            }
        }

        // ================= UPDATE ORDER =================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(long id, [FromBody] CreateOrder model)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid order ID." });
            if (model == null) return BadRequest(new { message = "Order data is required." });

            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null) return NotFound(new { message = "Order not found." });

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

        // ================= DELETE ORDER =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(long id)
        {
            if (id <= 0) return BadRequest(new { message = "Invalid order ID." });

            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null) return NotFound(new { message = "Order not found." });

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Order deleted successfully." });
            }
            catch
            {
                return StatusCode(500, new { message = "Failed to delete order." });
            }
        }
    }
}
