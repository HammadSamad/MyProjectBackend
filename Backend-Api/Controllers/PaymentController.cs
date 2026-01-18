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
    public class PaymentController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public PaymentController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ================= CREATE =================
        // POST: api/Payment
        [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePayment model)
        {
            var orderExists = await _context.Orders.AnyAsync(o => o.OrderId == model.OrderId);
            if (!orderExists)
                return BadRequest("Order does not exist.");

            var paymentMethodExists = await _context.PaymentMethods
                .AnyAsync(pm => pm.PaymentMethodId == model.PaymentMethodId);
            if (!paymentMethodExists)
                return BadRequest("Payment method does not exist.");

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

        // ================= GET ALL =================
        // GET: api/Payment
        [HttpGet]
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
        // GET: api/Payment/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PaymentDTO>> GetPaymentById(long id)
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
                return NotFound("Payment not found.");

            return Ok(payment);
        }

        // ================= GET BY ORDER =================
        // GET: api/Payment/order/10
        [HttpGet("order/{orderId}")]
        public async Task<ActionResult<IEnumerable<PaymentDTO>>> GetPaymentsByOrder(long orderId)
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

            return Ok(payments);
        }

        // ================= UPDATE =================
        // PUT: api/Payment/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePayment(long id, [FromBody] PaymentDTO model)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return NotFound("Payment not found.");

            payment.Amount = model.Amount;
            payment.Status = model.Status;
            payment.PaymentMethodId = model.PaymentMethodId;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Payment updated successfully." });
        }

        // ================= MARK AS PAID =================
        // PUT: api/Payment/mark-paid/5
        [HttpPut("mark-paid/{id}")]
        public async Task<IActionResult> MarkAsPaid(long id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return NotFound("Payment not found.");

            payment.Status = "Paid";
            payment.PaidAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Payment marked as Paid." });
        }

        // ================= DELETE =================
        // DELETE: api/Payment/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePayment(long id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return NotFound("Payment not found.");

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Payment deleted successfully." });
        }
    }
}
