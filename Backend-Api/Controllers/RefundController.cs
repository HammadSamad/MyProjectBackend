using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class RefundController : ControllerBase
{
    private readonly LaptopHarbourDbContext _context;

    // Allowed refund statuses
    private readonly string[] allowedStatuses = new[] { "Pending", "Completed", "Rejected" };

    public RefundController(LaptopHarbourDbContext context)
    {
        _context = context;
    }

    // ================= CREATE REFUND =================
    [HttpPost]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> CreateRefund([FromBody] CreateRefund model)
    {
        if (model == null)
            return BadRequest(new { error = "Invalid request payload" });

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var payment = await _context.Payments.FindAsync(model.PaymentId);
            if (payment == null)
                return NotFound(new { error = "Payment not found" });

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

            await transaction.CommitAsync();

            return Ok(new { message = "Refund created successfully", refundId = refund.RefundId });
        }
        catch (DbUpdateException dbEx)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { error = "Database error occurred while creating refund", details = dbEx.Message });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
        }
    }

    // ================= GET ALL WITH PAGINATION =================
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RefundDTO>>> GetAllRefunds(
        int page = 1,
        int pageSize = 20,
        string? statusFilter = null)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        try
        {
            var query = _context.Refunds.AsQueryable();

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                query = query.Where(r => r.Status == statusFilter);
            }

            var totalCount = await query.CountAsync();

            var refunds = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new RefundDTO
                {
                    RefundId = r.RefundId,
                    PaymentId = r.PaymentId,
                    ReturnId = r.ReturnId,
                    Amount = r.Amount,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                })
                .ToListAsync();

            return Ok(new
            {
                totalCount,
                page,
                pageSize,
                refunds
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to fetch refunds", details = ex.Message });
        }
    }

    // ================= GET BY ID =================
    [HttpGet("{id}")]
    public async Task<ActionResult<RefundDTO>> GetRefundById(long id)
    {
        try
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
                    CreatedAt = r.CreatedAt,
                })
                .FirstOrDefaultAsync();

            if (refund == null)
                return NotFound(new { error = "Refund not found" });

            return Ok(refund);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to fetch refund", details = ex.Message });
        }
    }

    // ================= UPDATE STATUS =================
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> UpdateRefundStatus(long id, [FromBody] string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return BadRequest(new { error = "Status cannot be empty" });

        if (!allowedStatuses.Contains(status))
            return BadRequest(new { error = $"Invalid status. Allowed values: {string.Join(", ", allowedStatuses)}" });

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var refund = await _context.Refunds.FindAsync(id);
            if (refund == null)
                return NotFound(new { error = "Refund not found" });

            refund.Status = status;

            if (status == "Completed")
                refund.RefundedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { message = $"Refund status updated to {status}" });
        }
        catch (DbUpdateException dbEx)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { error = "Database error occurred while updating refund status", details = dbEx.Message });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
        }
    }

    // ================= DELETE =================
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> DeleteRefund(long id)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var refund = await _context.Refunds.FindAsync(id);
            if (refund == null)
                return NotFound(new { error = "Refund not found" });

            _context.Refunds.Remove(refund);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { message = "Refund deleted successfully" });
        }
        catch (DbUpdateException dbEx)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { error = "Database error occurred while deleting refund", details = dbEx.Message });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
        }
    }
}
