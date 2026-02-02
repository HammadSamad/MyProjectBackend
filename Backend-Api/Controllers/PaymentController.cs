using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Backend_Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;
        private readonly IEmailService _emailService;

        public PaymentController(LaptopHarbourDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ================= CREATE =================
        [HttpPost]
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

                order.OrderStatus = "Paid";
                order.UpdatedAt = DateTime.UtcNow;
                _context.Orders.Update(order);

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
                        Message = $"Payment received for Order #{order.OrderId}. Amount: {payment.Amount}.",
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
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ================= GET ALL =================
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<PaymentDTO>>> GetAllPayments()
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

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        public async Task<ActionResult<PaymentDTO>> GetPaymentById(
            long id,
            [FromQuery] int userId)
        {
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

        // ================= GET BY ORDER =================
        [HttpGet("order/{orderId}")]
        public async Task<ActionResult<IEnumerable<PaymentDTO>>> GetPaymentsByOrder(
            long orderId,
            [FromQuery] int userId)
        {
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

        // ================= UPDATE =================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePayment(long id, [FromBody] PaymentDTO model)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return NotFound(new { error = "Payment not found." });

            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && payment.UserId != model.UserId)
                return Forbid();

            payment.Amount = model.Amount;
            payment.Status = model.Status;
            payment.PaymentMethodId = model.PaymentMethodId;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Payment updated successfully." });
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePayment(
            long id,
            [FromQuery] int userId)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return NotFound(new { error = "Payment not found." });

            var isAdmin = User.IsInRole("Admin");

            if (!isAdmin && payment.UserId != userId)
                return Forbid();

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Payment deleted successfully." });
        }
    }
}
