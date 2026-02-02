using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Backend_Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
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
        // ================= HELPER =================
        //private bool TryGetUserId(out int userId)
        //{
        //    userId = 0;
        //    var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    return !string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out userId);
        //}

        // ================= CREATE ORDER =================
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrder model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Order data is required." });

            if (model.TotalAmount <= 0)
                return BadRequest(new { success = false, message = "Total amount must be greater than zero." });

            try
            {
                // Use the UserId from the model since JWT is removed
                var userId = model.UserId;
                if (userId <= 0)
                    return BadRequest(new { success = false, message = "Valid user ID is required." });

                // Create order
                var order = new Order
                {
                    UserId = userId,
                    TotalAmount = model.TotalAmount,
                    PaymentMethodId = model.PaymentMethodId,
                    OrderStatus = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Add to UserRecentOrders
                var existingRecent = await _context.UserRecentOrders
                    .FirstOrDefaultAsync(u => u.UserId == userId && u.OrderId == order.OrderId);

                if (existingRecent != null)
                    existingRecent.CreatedAt = DateTime.UtcNow;
                else
                    _context.UserRecentOrders.Add(new UserRecentOrder
                    {
                        UserId = userId,
                        OrderId = order.OrderId,
                        CreatedAt = DateTime.UtcNow
                    });

                // Notify Admins
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
                        Message = $"Order #{order.OrderId} has been placed by user #{userId}.",
                        Type = "Order",
                        TargetAudience = "Admin",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // Notify User
                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Title = "Order Placed Successfully",
                    Message = $"Your order #{order.OrderId} has been placed successfully.",
                    Type = "Order",
                    TargetAudience = "User",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                // Send order email asynchronously
                _ = SendOrderEmailToUser(order.OrderId);

                return Ok(new
                {
                    success = true,
                    message = "Order created successfully.",
                    orderId = order.OrderId
                });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Database error occurred while creating the order.",
                    details = dbEx.ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An unexpected error occurred while creating the order.",
                    details = ex.ToString()
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
                var order = await _context.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null) return NotFound(new { message = "Order not found." });

                order.OrderStatus = status;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // ---------------- Notify User on Every Status Update ----------------
                _context.Notifications.Add(new Notification
                {
                    UserId = order.UserId,
                    Title = "Order Status Updated",
                    Message = $"Your order #{order.OrderId} status has been changed to '{status}'.",
                    Type = "Order",
                    TargetAudience = "User",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                // ---------------- If Order is Cancelled ----------------
                if (status.ToLower() == "cancelled" || status.ToLower() == "canceled")
                {
                    // Notify Admin
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

                    // Send Cancel Email to User
                    await SendOrderCancelEmailToUser(order.OrderId);
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Order status updated and notifications sent successfully." });
            }
            catch
            {
                return StatusCode(500, new { message = "Failed to update order status." });
            }
        }

        // ================= SEND ORDER EMAIL =================
        private async Task SendOrderEmailToUser(long orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null || order.User == null || string.IsNullOrWhiteSpace(order.User.Email))
                    return;

                var orderItems = await _context.OrderItems
                    .Where(oi => oi.OrderId == orderId)
                    .Include(oi => oi.Variant)
                        .ThenInclude(v => v.Product)
                    .ToListAsync();

                string body = $"<h3>We have received your order #{order.OrderId}</h3>";
                body += $"<p>Total Amount: {order.TotalAmount:C}</p>";
                body += "<h4>Order Items:</h4><ul>";

                foreach (var item in orderItems)
                    body += $"<li>{item.Variant?.Product?.ProductName} - {item.Quantity} x {item.Price:C}</li>";

                body += "</ul><p>Thank you for shopping with us!</p>";

                await _emailService.SendEmailAsync(order.User.Email, "Your Order has been received", body);
            }
            catch { }
        }

        // ================= SEND ORDER CANCEL EMAIL =================
        private async Task SendOrderCancelEmailToUser(long orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null || order.User == null || string.IsNullOrWhiteSpace(order.User.Email))
                    return;

                string body = $"<h3>Your order #{order.OrderId} has been cancelled</h3>";
                body += "<p>If you have already paid, your refund will be processed according to our policy.</p>";

                await _emailService.SendEmailAsync(order.User.Email, "Order Cancelled", body);
            }
            catch { }
        }

        // ================= GET ALL ORDERS =================
        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
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

        // ================= GET ORDER BY ID =================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(long id)
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

            if (order == null) return NotFound();
            return Ok(order);
        }

        // ================= GET ORDERS BY USER =================
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(int userId)
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

        // ================= DELETE ORDER =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(long id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order deleted successfully." });
        }



        [HttpGet("user/{userId}/orders")]
        public async Task<IActionResult> GetOrdersByUserId(int userId)
        {
            try
            {
                if (userId <= 0)
                    return BadRequest(new { message = "Invalid user id." });

                // 1️⃣ Load all orders for this user with items, variants, products, images, and shipments
                var orders = await _context.Orders
                    .Where(o => o.UserId == userId)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Variant)
                            .ThenInclude(v => v.Product)
                                .ThenInclude(p => p.ProductImages)
                    .Include(o => o.Shipments)
                    .Include(o => o.OrderAddresses)
                        .ThenInclude(oa => oa.Address)
                            .ThenInclude(a => a.City) // Include City navigation
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();

                if (orders == null || !orders.Any())
                    return NotFound(new { message = "No orders found for this user." });

                // 2️⃣ Prepare DTOs for each order
                var ordersDto = new List<OrderDetailsDTO>();

                foreach (var order in orders)
                {
                    var itemsDto = new List<OrderDetailsItemDTO>();

                    foreach (var oi in order.OrderItems)
                    {
                        var variant = oi.Variant;
                        var product = variant?.Product;

                        if (product == null) continue;

                        var coverImage = product.ProductImages
                            .FirstOrDefault(i => i.IsCover == true)?.ImageUrl ?? "";

                        // Load specifications for this variant
                        var specs = await _context.VariantSpecificationOptions
                            .Where(vso => vso.VariantId == oi.VariantId)
                            .Select(vso => new VariantSpecificationOptionDTO
                            {
                                OptionId = vso.OptionId,
                                SpecificationName = vso.Option.Specification.SpecificationName,
                                OptionValue = vso.Option.OptionValue
                            })
                            .ToListAsync();

                        itemsDto.Add(new OrderDetailsItemDTO
                        {
                            ProductId = product.ProductId,
                            VariantId = oi.VariantId,
                            VariantSpecificationOptionsId = oi.VariantSpecificationOptionsId,
                            Quantity = oi.Quantity,
                            UserId = order.UserId,
                            Price = oi.Price,
                            ProductName = product.ProductName,
                            Image = coverImage,
                            VariantSpecifications = specs
                        });
                    }

                    // Get order address
                    var orderAddress = order.OrderAddresses.FirstOrDefault();
                    OrderAdressDTO? addressDto = null;

                    if (orderAddress != null && orderAddress.Address != null)
                    {
                        addressDto = new OrderAdressDTO
                        {
                            OrderAddressId = orderAddress.OrderAddressId,
                            OrderId = orderAddress.OrderId,
                            RecipientName = orderAddress.RecipientName,
                            AddressId = orderAddress.AddressId,
                            Phone = orderAddress.Phone,
                            AddressLine1 = orderAddress.Address.AddressLine1,
                            // Map AddressLine2 to Label
                            Label = orderAddress.Address.AddressLine2,
                            CityName = orderAddress.Address.City?.CityName,
                            CreatedAt = orderAddress.CreatedAt
                        };
                    }

                    // Latest shipment
                    var latestShipment = order.Shipments
                        .OrderByDescending(s => s.CreatedAt)
                        .FirstOrDefault();

                    ordersDto.Add(new OrderDetailsDTO
                    {
                        OrderId = order.OrderId,
                        UserId = order.UserId,
                        ItemsCount = itemsDto.Sum(i => i.Quantity ?? 0),
                        DeliveryStatus = latestShipment?.Status ?? "Pending",
                        TotalAmount = order.TotalAmount,
                        Items = itemsDto,
                        OrderAddress = addressDto
                    });
                }

                return Ok(ordersDto);
            }
            catch (Exception ex)
            {
                // Log the exception here
                return StatusCode(500, new { error = "Failed to fetch user orders.", details = ex.Message });
            }
        }





    }
}
