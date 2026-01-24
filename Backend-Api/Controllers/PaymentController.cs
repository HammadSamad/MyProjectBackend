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
            try
            {
                if (model == null)
                    return BadRequest(new { error = "Invalid request payload." });

                var orderExists = await _context.Orders.AnyAsync(o => o.OrderId == model.OrderId);
                if (!orderExists)
                    return BadRequest(new { error = "Order does not exist." });

                var paymentMethodExists = await _context.PaymentMethods
                    .AnyAsync(pm => pm.PaymentMethodId == model.PaymentMethodId);
                if (!paymentMethodExists)
                    return BadRequest(new { error = "Payment method does not exist." });

                var duplicateExists = await _context.Payments
                    .AnyAsync(p => p.OrderId == model.OrderId && p.PaymentMethodId == model.PaymentMethodId);
                if (duplicateExists)
                    return BadRequest(new { error = "A payment for this order with the selected payment method already exists." });

                var payment = new Payment
                {
                    OrderId = model.OrderId,
                    PaymentMethodId = model.PaymentMethodId,
                    Amount = model.Amount,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Payment created successfully",
                    paymentId = payment.PaymentId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to create payment.", details = ex.Message });
            }
        }

        // ================= GET ALL =================
        [HttpGet]
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

                if (!payments.Any())
                    return Ok(new { message = "No payments found.", data = new List<PaymentDTO>() });

                return Ok(payments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch payments.", details = ex.Message });
            }
        }

        // ================= GET UNPAID PAYMENTS =================
        [HttpGet("unpaid")]
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

                if (!unpaidPayments.Any())
                    return Ok(new { message = "No unpaid payments found.", data = new List<PaymentDTO>() });

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
                var payment = await _context.Payments
                    .Where(p => p.PaymentId == id)
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
                    return NotFound(new { error = "Payment not found." });

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
                var payments = await _context.Payments
                    .Where(p => p.OrderId == orderId)
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
                    return Ok(new { message = "No payments found for this order.", data = new List<PaymentDTO>() });

                return Ok(payments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch payments by order.", details = ex.Message });
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

                var duplicateExists = await _context.Payments
                    .AnyAsync(p => p.PaymentId != id
                                   && p.OrderId == model.OrderId
                                   && p.PaymentMethodId == model.PaymentMethodId);
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

        // ================= MARK AS PAID + SEND EMAIL =================
        [HttpPut("mark-paid/{id}")]
        public async Task<IActionResult> MarkAsPaid(long id)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Order)
                        .ThenInclude(o => o.User)   // Order → User navigation
                    .FirstOrDefaultAsync(p => p.PaymentId == id);

                if (payment == null)
                    return NotFound(new { error = "Payment not found." });

                if (payment.Status == "Paid")
                    return BadRequest(new { error = "Payment is already marked as Paid." });

                payment.Status = "Paid";
                payment.PaidAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // ---------- Send Confirmation Email ----------
                var to = payment.Order.User.Email;
                var subject = "Payment Confirmation - Laptop Harbour";
                var body = $@"
                    <h2>Payment Successful</h2>
                    <p>Dear {payment.Order.User.Username},</p>
                    <p>Your payment has been successfully received.</p>
                    <hr/>
                    <p><strong>Order ID:</strong> {payment.OrderId}</p>
                    <p><strong>Payment ID:</strong> {payment.PaymentId}</p>
                    <p><strong>Amount:</strong> {payment.Amount}</p>
                    <p><strong>Status:</strong> Paid</p>
                    <p><strong>Date:</strong> {payment.PaidAt:yyyy-MM-dd HH:mm}</p>
                    <hr/>
                    <p>Thank you for shopping with <b>Laptop Harbour</b>.</p>
                ";

                await _emailService.SendEmailAsync(to, subject, body);

                return Ok(new
                {
                    message = "Payment marked as Paid and confirmation email sent successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to mark payment as Paid or send confirmation email.",
                    details = ex.Message
                });
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
