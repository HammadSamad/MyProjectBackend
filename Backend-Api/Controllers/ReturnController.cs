using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReturnController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        // Allowed statuses for returns
        private readonly string[] allowedStatuses = new[] { "Pending", "Approved", "Rejected", "Completed" };

        public ReturnController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ================= CREATE RETURN =================
        [HttpPost]
        [Authorize(Roles = "Admin,Finance")]
        public async Task<IActionResult> CreateReturn([FromBody] CreateReturn model)
        {
            if (model == null)
                return BadRequest(new { error = "Invalid request payload" });

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var order = await _context.Orders.FindAsync(model.OrderId);
                if (order == null)
                    return NotFound(new { error = "Order not found" });

                var newReturn = new Return
                {
                    OrderId = model.OrderId,
                    UserId = order.UserId,
                    Reason = model.Reason,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Returns.Add(newReturn);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Return request created successfully", returnId = newReturn.ReturnId });
            }
            catch (DbUpdateException dbEx)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Database error occurred while creating return", details = dbEx.Message });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
            }
        }

        // ================= GET ALL RETURNS WITH PAGINATION =================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReturnDTO>>> GetAllReturns(
            int page = 1,
            int pageSize = 20,
            string? statusFilter = null)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;

            try
            {
                var query = _context.Returns.AsQueryable();

                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    query = query.Where(r => r.Status == statusFilter);
                }

                var totalCount = await query.CountAsync();

                var returns = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new ReturnDTO
                    {
                        ReturnId = r.ReturnId,
                        OrderId = r.OrderId,
                        Status = r.Status,
                        CreatedAt = r.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    totalCount,
                    page,
                    pageSize,
                    returns
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch returns", details = ex.Message });
            }
        }

        // ================= GET RETURN BY ID =================
        [HttpGet("{id}")]
        public async Task<ActionResult<ReturnDTO>> GetReturnById(long id)
        {
            try
            {
                var r = await _context.Returns
                    .Where(r => r.ReturnId == id)
                    .Select(r => new ReturnDTO
                    {
                        ReturnId = r.ReturnId,
                        OrderId = r.OrderId,
                        Status = r.Status,
                        CreatedAt = r.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (r == null)
                    return NotFound(new { error = "Return not found" });

                return Ok(r);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch return", details = ex.Message });
            }
        }

        // ================= UPDATE RETURN =================
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Finance")]
        public async Task<IActionResult> UpdateReturn(long id, [FromBody] CreateReturn model)
        {
            if (model == null)
                return BadRequest(new { error = "Invalid request payload" });

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var r = await _context.Returns.FindAsync(id);
                if (r == null)
                    return NotFound(new { error = "Return not found" });

                r.Reason = model.Reason ?? r.Reason;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Return updated successfully" });
            }
            catch (DbUpdateException dbEx)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Database error occurred while updating return", details = dbEx.Message });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
            }
        }

        // ================= UPDATE STATUS =================
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin,Finance")]
        public async Task<IActionResult> UpdateReturnStatus(long id, [FromBody] string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return BadRequest(new { error = "Status cannot be empty" });

            if (!allowedStatuses.Contains(status))
                return BadRequest(new { error = $"Invalid status. Allowed values: {string.Join(", ", allowedStatuses)}" });

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var r = await _context.Returns.FindAsync(id);
                if (r == null)
                    return NotFound(new { error = "Return not found" });

                r.Status = status;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = $"Return status updated to {status}" });
            }
            catch (DbUpdateException dbEx)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Database error occurred while updating return status", details = dbEx.Message });
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
        public async Task<IActionResult> DeleteReturn(long id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var r = await _context.Returns.FindAsync(id);
                if (r == null)
                    return NotFound(new { error = "Return not found" });

                _context.Returns.Remove(r);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Return deleted successfully" });
            }
            catch (DbUpdateException dbEx)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Database error occurred while deleting return", details = dbEx.Message });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
            }
        }
    }
}
