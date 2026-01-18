using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Require JWT for all actions
    public class ComplaintController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public ComplaintController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // GET: api/Complaint
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ComplaintDTO>>> GetAllComplaints()
        {
            var complaints = await _context.Complaints
                .Select(c => new ComplaintDTO
                {
                    ComplaintId = c.ComplaintId,
                    OrderId = c.OrderId,
                    Status = c.Status
                }).ToListAsync();

            return Ok(complaints);
        }

        // GET: api/Complaint/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ComplaintDTO>> GetComplaint(long id)
        {
            var c = await _context.Complaints.FindAsync(id);

            if (c == null) return NotFound();

            var complaintDto = new ComplaintDTO
            {
                ComplaintId = c.ComplaintId,
                OrderId = c.OrderId,
                Status = c.Status
            };

            return Ok(complaintDto);
        }

        // POST: api/Complaint
        [HttpPost]
        public async Task<ActionResult<ComplaintDTO>> CreateComplaint([FromBody] CreateComplaint model)
        {
            // Extract UserId safely from JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return Unauthorized("User not authenticated.");
            int userId = int.Parse(userIdClaim);

            var complaint = new Complaint
            {
                UserId = userId,
                OrderId = model.OrderId,
                Subject = model.Subject,
                Description = model.Description,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Complaints.Add(complaint);
            await _context.SaveChangesAsync();

            var complaintDto = new ComplaintDTO
            {
                ComplaintId = complaint.ComplaintId,
                OrderId = complaint.OrderId,
                Status = complaint.Status
            };

            return CreatedAtAction(nameof(GetComplaint), new { id = complaint.ComplaintId }, complaintDto);
        }

        // PUT: api/Complaint/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComplaint(long id, [FromBody] CreateComplaint model)
        {
            var complaint = await _context.Complaints.FindAsync(id);
            if (complaint == null) return NotFound();

            // Optionally allow only creator to update
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return Unauthorized("User not authenticated.");
            int userId = int.Parse(userIdClaim);

            if (complaint.UserId != userId) return Forbid("You can only update your own complaints.");

            complaint.Subject = model.Subject;
            complaint.Description = model.Description;
            complaint.UpdatedAt = DateTime.UtcNow;

            _context.Complaints.Update(complaint);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Complaint/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComplaint(long id)
        {
            var complaint = await _context.Complaints.FindAsync(id);
            if (complaint == null) return NotFound();

            // Optionally allow only creator to delete
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return Unauthorized("User not authenticated.");
            int userId = int.Parse(userIdClaim);

            if (complaint.UserId != userId) return Forbid("You can only delete your own complaints.");

            _context.Complaints.Remove(complaint);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
