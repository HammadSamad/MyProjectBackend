using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        [HttpPost]
        public async Task<IActionResult> CreatePaymentMethod([FromBody] CreatePaymentMethod model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.MethodName))
                    return BadRequest(new { error = "MethodName is required." });

                // Prevent duplicate
                bool exists = await _context.PaymentMethods
                    .AnyAsync(pm => pm.MethodName!.ToLower() == model.MethodName.ToLower());
                if (exists)
                    return BadRequest(new { error = "Payment method already exists." });

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
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to create payment method.", details = ex.Message });
            }
        }

        // ================= GET ALL =================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PaymentMethodDTO>>> GetAllPaymentMethods()
        {
            try
            {
                var methods = await _context.PaymentMethods
                    .Select(pm => new PaymentMethodDTO
                    {
                        PaymentMethodId = pm.PaymentMethodId,
                        MethodName = pm.MethodName
                    })
                    .ToListAsync();

                if (methods.Count == 0)
                    return Ok(new { message = "No payment methods found.", data = new List<PaymentMethodDTO>() });

                return Ok(methods);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch payment methods.", details = ex.Message });
            }
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        public async Task<ActionResult<PaymentMethodDTO>> GetPaymentMethodById(int id)
        {
            try
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
                    return NotFound(new { error = "Payment method not found." });

                return Ok(method);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch payment method.", details = ex.Message });
            }
        }

        // ================= UPDATE =================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePaymentMethod(int id, [FromBody] CreatePaymentMethod model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.MethodName))
                    return BadRequest(new { error = "MethodName is required." });

                var paymentMethod = await _context.PaymentMethods.FindAsync(id);
                if (paymentMethod == null)
                    return NotFound(new { error = "Payment method not found." });

                // Prevent duplicate name on update
                bool duplicateExists = await _context.PaymentMethods
                    .AnyAsync(pm => pm.PaymentMethodId != id &&
                                    pm.MethodName!.ToLower() == model.MethodName.ToLower());
                if (duplicateExists)
                    return BadRequest(new { error = "Another payment method with the same name already exists." });

                paymentMethod.MethodName = model.MethodName;
                paymentMethod.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Payment method updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to update payment method.", details = ex.Message });
            }
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePaymentMethod(int id)
        {
            try
            {
                var paymentMethod = await _context.PaymentMethods
                    .Include(pm => pm.Payments)
                    .Include(pm => pm.Orders)
                    .FirstOrDefaultAsync(pm => pm.PaymentMethodId == id);

                if (paymentMethod == null)
                    return NotFound(new { error = "Payment method not found." });

                if (paymentMethod.Payments.Count > 0 || paymentMethod.Orders.Count > 0)
                    return BadRequest(new { error = "This payment method is already in use and cannot be deleted." });

                _context.PaymentMethods.Remove(paymentMethod);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Payment method deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to delete payment method.", details = ex.Message });
            }
        }
    }
}
