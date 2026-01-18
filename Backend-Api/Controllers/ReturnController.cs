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
    public class ReturnController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public ReturnController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ================= CREATE RETURN =================
        // POST: api/Return
        [HttpPost]
        public async Task<IActionResult> CreateReturn([FromBody] CreateReturn model)
        {
            var order = await _context.Orders.FindAsync(model.OrderId);
            if (order == null)
                return NotFound("Order not found.");

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

            return Ok(new { message = "Return request created successfully", returnId = newReturn.ReturnId });
        }

        // ================= GET ALL RETURNS =================
        // GET: api/Return
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReturnDTO>>> GetAllReturns()
        {
            var returns = await _context.Returns
                .Select(r => new ReturnDTO
                {
                    ReturnId = r.ReturnId,
                    OrderId = r.OrderId,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(returns);
        }

        // ================= GET RETURN BY ID =================
        // GET: api/Return/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ReturnDTO>> GetReturnById(long id)
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
                return NotFound("Return not found.");

            return Ok(r);
        }

        // ================= UPDATE RETURN =================
        // PUT: api/Return/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReturn(long id, [FromBody] CreateReturn model)
        {
            var r = await _context.Returns.FindAsync(id);
            if (r == null)
                return NotFound("Return not found.");

            r.Reason = model.Reason ?? r.Reason;
            // Status and other properties can be updated separately

            await _context.SaveChangesAsync();
            return Ok(new { message = "Return updated successfully." });
        }

        // ================= UPDATE STATUS =================
        // PATCH: api/Return/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateReturnStatus(long id, [FromBody] string status)
        {
            var r = await _context.Returns.FindAsync(id);
            if (r == null)
                return NotFound("Return not found.");

            r.Status = status;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Return status updated to {status}" });
        }

        // ================= DELETE =================
        // DELETE: api/Return/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReturn(long id)
        {
            var r = await _context.Returns.FindAsync(id);
            if (r == null)
                return NotFound("Return not found.");

            _context.Returns.Remove(r);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Return deleted successfully." });
        }
    }
}
