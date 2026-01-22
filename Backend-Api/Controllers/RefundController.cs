using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class RefundController : ControllerBase
{
    private readonly LaptopHarbourDbContext _context;

    public RefundController(LaptopHarbourDbContext context)
    {
        _context = context;
    }

    // ================= CREATE REFUND =================
    [HttpPost]
    public async Task<IActionResult> CreateRefund([FromBody] CreateRefund model)
    {
        var payment = await _context.Payments.FindAsync(model.PaymentId);
        if (payment == null)
            return NotFound("Payment not found.");

        var refund = new Refund
        {
            PaymentId = model.PaymentId,
            ReturnId = model.ReturnId,
            Amount = model.Amount,
            Reason = model.Reason,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.Refunds.Add(refund);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Refund created successfully", refundId = refund.RefundId });
    }

    // ================= GET ALL =================
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RefundDTO>>> GetAllRefunds()
    {
        var refunds = await _context.Refunds
            .Select(r => new RefundDTO
            {
                RefundId = r.RefundId,
                PaymentId = r.PaymentId,
                ReturnId = r.ReturnId,
                Amount = r.Amount,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(refunds);
    }

    // ================= GET BY ID =================
    [HttpGet("{id}")]
    public async Task<ActionResult<RefundDTO>> GetRefundById(long id)
    {
        var refund = await _context.Refunds
            .Where(r => r.RefundId == id)
            .Select(r => new RefundDTO
            {
                RefundId = r.RefundId,
                PaymentId = r.PaymentId,
                ReturnId = r.ReturnId,
                Amount = r.Amount,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (refund == null)
            return NotFound("Refund not found.");

        return Ok(refund);
    }

    // ================= UPDATE STATUS =================
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateRefundStatus(long id, [FromBody] string status)
    {
        var refund = await _context.Refunds.FindAsync(id);
        if (refund == null)
            return NotFound("Refund not found.");

        refund.Status = status;

        if (status == "Completed")
            refund.RefundedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = $"Refund status updated to {status}" });
    }

    // ================= DELETE =================
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRefund(long id)
    {
        var refund = await _context.Refunds.FindAsync(id);
        if (refund == null)
            return NotFound("Refund not found.");

        _context.Refunds.Remove(refund);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Refund deleted successfully" });
    }
}
