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
    public class PaymentMethodController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public PaymentMethodController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ================= CREATE =================
        // POST: api/PaymentMethod
        [HttpPost]
        public async Task<IActionResult> CreatePaymentMethod([FromBody] CreatePaymentMethod model)
        {
            if (await _context.PaymentMethods.AnyAsync(x => x.MethodName == model.MethodName))
                return BadRequest("Payment method already exists.");

            var paymentMethod = new PaymentMethod
            {
                MethodName = model.MethodName,
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentMethods.Add(paymentMethod);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Payment method created successfully",
                paymentMethodId = paymentMethod.PaymentMethodId
            });
        }

        // ================= GET ALL =================
        // GET: api/PaymentMethod
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PaymentMethodDTO>>> GetAllPaymentMethods()
        {
            var methods = await _context.PaymentMethods
                .Select(pm => new PaymentMethodDTO
                {
                    PaymentMethodId = pm.PaymentMethodId,
                    MethodName = pm.MethodName
                })
                .ToListAsync();

            return Ok(methods);
        }

        // ================= GET BY ID =================
        // GET: api/PaymentMethod/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PaymentMethodDTO>> GetPaymentMethodById(int id)
        {
            var method = await _context.PaymentMethods
                .Where(pm => pm.PaymentMethodId == id)
                .Select(pm => new PaymentMethodDTO
                {
                    PaymentMethodId = pm.PaymentMethodId,
                    MethodName = pm.MethodName
                })
                .FirstOrDefaultAsync();

            if (method == null)
                return NotFound("Payment method not found.");

            return Ok(method);
        }

        // ================= UPDATE =================
        // PUT: api/PaymentMethod/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePaymentMethod(int id, [FromBody] CreatePaymentMethod model)
        {
            var paymentMethod = await _context.PaymentMethods.FindAsync(id);
            if (paymentMethod == null)
                return NotFound("Payment method not found.");

            paymentMethod.MethodName = model.MethodName;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Payment method updated successfully." });
        }

        // ================= DELETE =================
        // DELETE: api/PaymentMethod/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePaymentMethod(int id)
        {
            var paymentMethod = await _context.PaymentMethods
                .Include(pm => pm.Payments)
                .Include(pm => pm.Orders)
                .FirstOrDefaultAsync(pm => pm.PaymentMethodId == id);

            if (paymentMethod == null)
                return NotFound("Payment method not found.");

            // Safety check: don’t delete if already in use
            if (paymentMethod.Payments.Any() || paymentMethod.Orders.Any())
                return BadRequest("This payment method is already in use and cannot be deleted.");

            _context.PaymentMethods.Remove(paymentMethod);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Payment method deleted successfully." });
        }
    }
}
