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
    [Authorize] // Require JWT
    public class ComplaintMessageController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public ComplaintMessageController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // GET: api/ComplaintMessage
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ComplaintMessageDTO>>> GetAllMessages()
        {
            var messages = await _context.ComplaintMessages
                .Select(m => new ComplaintMessageDTO
                {
                    MessageId = m.MessageId,
                    ComplaintId = m.ComplaintId,
                    Message = m.Message
                }).ToListAsync();

            return Ok(messages);
        }

        // GET: api/ComplaintMessage/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ComplaintMessageDTO>> GetMessage(long id)
        {
            var message = await _context.ComplaintMessages.FindAsync(id);
            if (message == null) return NotFound();

            var messageDto = new ComplaintMessageDTO
            {
                MessageId = message.MessageId,
                ComplaintId = message.ComplaintId,
                Message = message.Message
            };

            return Ok(messageDto);
        }

        // POST: api/ComplaintMessage
        [HttpPost]
        public async Task<ActionResult<ComplaintMessageDTO>> CreateMessage([FromBody] CreateComplaintMessage model)
        {
            // Extract UserId from JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return Unauthorized("User not authenticated.");
            int userId = int.Parse(userIdClaim);

            // Optional: Verify complaint exists and belongs to user
            var complaint = await _context.Complaints.FindAsync(model.ComplaintId);
            if (complaint == null) return BadRequest("Complaint not found.");

            var message = new ComplaintMessage
            {
                ComplaintId = model.ComplaintId,
                Message = model.Message,
                CreatedAt = DateTime.UtcNow
            };

            _context.ComplaintMessages.Add(message);
            await _context.SaveChangesAsync();

            var messageDto = new ComplaintMessageDTO
            {
                MessageId = message.MessageId,
                ComplaintId = message.ComplaintId,
                Message = message.Message
            };

            return CreatedAtAction(nameof(GetMessage), new { id = message.MessageId }, messageDto);
        }

        // PUT: api/ComplaintMessage/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMessage(long id, [FromBody] CreateComplaintMessage model)
        {
            var message = await _context.ComplaintMessages.FindAsync(id);
            if (message == null) return NotFound();

            // Optional: Allow only the creator of the complaint to update
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return Unauthorized("User not authenticated.");
            int userId = int.Parse(userIdClaim);

            var complaint = await _context.Complaints.FindAsync(message.ComplaintId);
            if (complaint == null) return BadRequest("Complaint not found.");
            if (complaint.UserId != userId) return Forbid("You can only update messages for your own complaints.");

            message.Message = model.Message;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/ComplaintMessage/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(long id)
        {
            var message = await _context.ComplaintMessages.FindAsync(id);
            if (message == null) return NotFound();

            // Optional: Only allow the complaint creator to delete
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return Unauthorized("User not authenticated.");
            int userId = int.Parse(userIdClaim);

            var complaint = await _context.Complaints.FindAsync(message.ComplaintId);
            if (complaint == null) return BadRequest("Complaint not found.");
            if (complaint.UserId != userId) return Forbid("You can only delete messages for your own complaints.");

            _context.ComplaintMessages.Remove(message);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
