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
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;
        private readonly IEmailService _emailService;

        public PaymentController(LaptopHarbourDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ================= HELPER =================
        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out userId);
        }

        // ================= CREATE =================
        [HttpPost("payment")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePayment model)
        {
            if (model == null)
                return BadRequest(new { success = false, message = "Invalid request payload." });

            if (model.UserId <= 0)
                return BadRequest(new { success = false, message = "Valid user ID is required." });

            if (model.Amount <= 0)
                return BadRequest(new { success = false, message = "Payment amount must be greater than zero." });

            try
            {
                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.OrderId == model.OrderId);

                if (order == null)
                    return BadRequest(new { success = false, message = "Order does not exist." });

                if (order.UserId != model.UserId)
                    return StatusCode(403, new
                    {
                        success = false,
                        message = "You do not have access to this order."
                    });

                if (order.OrderStatus == "Paid")
                    return BadRequest(new
                    {
                        success = false,
                        message = "Order is already paid."
                    });

                var paymentMethodExists = await _context.PaymentMethods
                    .AnyAsync(pm => pm.PaymentMethodId == model.PaymentMethodId);

                if (!paymentMethodExists)
                    return BadRequest(new { success = false, message = "Payment method does not exist." });

                var duplicateExists = await _context.Payments
                    .AnyAsync(p =>
                        p.OrderId == model.OrderId &&
                        p.PaymentMethodId == model.PaymentMethodId);

                if (duplicateExists)
                    return BadRequest(new
                    {
                        success = false,
                        message = "A payment with this method already exists for this order."
                    });

                using var transaction = await _context.Database.BeginTransactionAsync();

                // Create payment
                var payment = new Payment
                {
                    OrderId = order.OrderId,
                    UserId = model.UserId,
                    PaymentMethodId = model.PaymentMethodId,
                    Amount = model.Amount,
                    Status = "Completed",
                    CreatedAt = DateTime.UtcNow,
                    TransactionReference =
                        $"PAY-{DateTime.UtcNow:yyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}"
                };

                _context.Payments.Add(payment);

                // Update order status
                order.OrderStatus = "Paid";
                order.UpdatedAt = DateTime.UtcNow;
                _context.Orders.Update(order);

                // 🔔 Notify User
                _context.Notifications.Add(new Notification
                {
                    UserId = model.UserId,
                    Title = "Payment Successful",
                    Message = $"Your payment for Order #{order.OrderId} was successful.",
                    Type = "Payment",
                    TargetAudience = "User",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                // 🔔 Notify Admins
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
                        Title = "New Payment Received",
                        Message =
                            $"Payment received for Order #{order.OrderId}. Amount: {payment.Amount}.",
                        Type = "Payment",
                        TargetAudience = "Admin",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = "Payment successful. Order marked as Paid.",
                    paymentId = payment.PaymentId,
                    transactionReference = payment.TransactionReference
                });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Database error occurred while creating payment.",
                    details = dbEx.ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to create payment due to an unexpected error.",
                    details = ex.ToString()
                });
            }
        }



        // ================= GET ALL =================
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<PaymentDTO>>> GetAllPayments()
        {
            try
            {
                var payments = await _context.Payments
                    .Select(p => new PaymentDTO
                    {
                        PaymentId = p.PaymentId,
                        OrderId = p.OrderId,
                        PaymentMethodId = p.PaymentMethodId,
                        Amount = p.Amount,
                        Status = p.Status
                    })
                    .ToListAsync();

                return Ok(payments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch payments.", details = ex.Message });
            }
        }

        // ================= GET UNPAID =================
        [HttpGet("unpaid")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<PaymentDTO>>> GetUnpaidPayments()
        {
            try
            {
                var unpaidPayments = await _context.Payments
                    .Where(p => p.Status != "Paid")
                    .Select(p => new PaymentDTO
                    {
                        PaymentId = p.PaymentId,
                        OrderId = p.OrderId,
                        PaymentMethodId = p.PaymentMethodId,
                        Amount = p.Amount,
                        Status = p.Status
                    })
                    .ToListAsync();

                return Ok(unpaidPayments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch unpaid payments.", details = ex.Message });
            }
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        public async Task<ActionResult<PaymentDTO>> GetPaymentById(long id)
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized();

                var isAdmin = User.IsInRole("Admin");

                var payment = await _context.Payments
                    .Where(p => p.PaymentId == id && (isAdmin || p.UserId == userId))
                    .Select(p => new PaymentDTO
                    {
                        PaymentId = p.PaymentId,
                        OrderId = p.OrderId,
                        PaymentMethodId = p.PaymentMethodId,
                        Amount = p.Amount,
                        Status = p.Status
                    })
                    .FirstOrDefaultAsync();

                if (payment == null)
                    return NotFound(new { error = "Payment not found or access denied." });

                return Ok(payment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch payment.", details = ex.Message });
            }
        }

        // ================= GET BY ORDER =================
        [HttpGet("order/{orderId}")]
        public async Task<ActionResult<IEnumerable<PaymentDTO>>> GetPaymentsByOrder(long orderId)
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized();

                var isAdmin = User.IsInRole("Admin");

                var payments = await _context.Payments
                    .Where(p => p.OrderId == orderId && (isAdmin || p.UserId == userId))
                    .Select(p => new PaymentDTO
                    {
                        PaymentId = p.PaymentId,
                        OrderId = p.OrderId,
                        PaymentMethodId = p.PaymentMethodId,
                        Amount = p.Amount,
                        Status = p.Status
                    })
                    .ToListAsync();

                return Ok(payments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch payments by order.", details = ex.Message });
            }
        }

        // ================= GET MY PAYMENTS =================
        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<PaymentDTO>>> GetMyPayments()
        {
            try
            {
                if (!TryGetUserId(out var userId))
                    return Unauthorized();

                var payments = await _context.Payments
                    .Where(p => p.UserId == userId)
                    .Select(p => new PaymentDTO
                    {
                        PaymentId = p.PaymentId,
                        OrderId = p.OrderId,
                        PaymentMethodId = p.PaymentMethodId,
                        Amount = p.Amount,
                        Status = p.Status
                    })
                    .ToListAsync();

                if (!payments.Any())
                    return Ok(new { message = "No payments found.", data = new List<PaymentDTO>() });

                return Ok(payments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch payments.", details = ex.Message });
            }
        }

        // ================= UPDATE =================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePayment(long id, [FromBody] PaymentDTO model)
        {
            try
            {
                var payment = await _context.Payments.FindAsync(id);
                if (payment == null)
                    return NotFound(new { error = "Payment not found." });

                if (!TryGetUserId(out var userId))
                    return Unauthorized();

                if (!User.IsInRole("Admin") && payment.UserId != userId)
                    return Forbid();

                var duplicateExists = await _context.Payments
                    .AnyAsync(p => p.PaymentId != id && p.OrderId == model.OrderId && p.PaymentMethodId == model.PaymentMethodId);

                if (duplicateExists)
                    return BadRequest(new { error = "A payment for this order with the selected payment method already exists." });

                payment.Amount = model.Amount;
                payment.Status = model.Status;
                payment.PaymentMethodId = model.PaymentMethodId;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Payment updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to update payment.", details = ex.Message });
            }
        }

        // ================= MARK AS PAID =================
        [HttpPut("mark-paid/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkAsPaid(long id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Order)
                        .ThenInclude(o => o.User)
                    .FirstOrDefaultAsync(p => p.PaymentId == id);

                if (payment == null)
                    return NotFound(new { error = "Payment not found." });

                if (payment.Status == "Paid")
                    return BadRequest(new { error = "Payment is already marked as Paid." });

                payment.Status = "Paid";
                payment.PaidAt = DateTime.UtcNow;

                if (string.IsNullOrEmpty(payment.TransactionReference))
                    payment.TransactionReference = $"PAY-{DateTime.UtcNow:yyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 4)}";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Email (null-safe)
                try
                {
                    var emailUser = payment.Order?.User;
                    if (!string.IsNullOrEmpty(emailUser?.Email))
                    {
                        var to = emailUser.Email;
                        var subject = "Payment Confirmation - Laptop Harbour";
                        var body = $@"
                            <h2>Payment Successful</h2>
                            <p>Dear {emailUser.Username},</p>
                            <p>Your payment has been successfully received.</p>
                            <hr/>
                            <p><strong>Order ID:</strong> {payment.OrderId}</p>
                            <p><strong>Payment ID:</strong> {payment.PaymentId}</p>
                            <p><strong>Transaction Reference:</strong> {payment.TransactionReference}</p>
                            <p><strong>Amount:</strong> {payment.Amount:C}</p>
                            <p><strong>Status:</strong> Paid</p>
                            <p><strong>Date:</strong> {payment.PaidAt:yyyy-MM-dd HH:mm}</p>
                            <hr/>
                            <p>Thank you for shopping with <b>Laptop Harbour</b>.</p>
                        ";
                        await _emailService.SendEmailAsync(to, subject, body);
                    }
                }
                catch { /* ignore email errors */ }

                return Ok(new
                {
                    message = "Payment marked as Paid and confirmation email sent successfully.",
                    transactionReference = payment.TransactionReference
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Failed to mark payment as Paid.", details = ex.Message });
            }
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePayment(long id)
        {
            try
            {
                var payment = await _context.Payments.FindAsync(id);
                if (payment == null)
                    return NotFound(new { error = "Payment not found." });

                if (!TryGetUserId(out var userId))
                    return Unauthorized();

                if (!User.IsInRole("Admin") && payment.UserId != userId)
                    return Forbid();

                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Payment deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to delete payment.", details = ex.Message });
            }
        }
    }
}
