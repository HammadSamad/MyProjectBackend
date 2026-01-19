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
    public class ComplaintMessageController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;
        private readonly IConfiguration _config;

        public ComplaintMessageController(LaptopHarbourDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // ================= CREATE =================
        [HttpPost]
        public async Task<IActionResult> AddMessage([FromForm] CreateComplaintMessage model)
        {
            var complaint = await _context.Complaints.FindAsync(model.ComplaintId);
            if (complaint == null) return NotFound("Complaint not found.");

            string? imageName = null;

            // Handle image upload
            if (model.Image != null && model.Image.Length > 0)
            {
                var uploadPath = Path.Combine(_config["StoredFilesPath"] ?? "wwwroot/upload", "ComplaintMessages");
                Directory.CreateDirectory(uploadPath);

                var ext = Path.GetExtension(model.Image.FileName);
                imageName = Guid.NewGuid() + ext;
                var filePath = Path.Combine(uploadPath, imageName);

                using var stream = System.IO.File.Create(filePath);
                await model.Image.CopyToAsync(stream);
            }

            var message = new ComplaintMessage
            {
                ComplaintId = model.ComplaintId,
                SenderType = model.SenderType,
                Message = model.Message,
                Image = imageName,
                CreatedAt = DateTime.UtcNow
            };

            _context.ComplaintMessages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Message added successfully", messageId = message.MessageId });
        }

        // ================= GET ALL =================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ComplaintMessageDTO>>> GetAllMessages()
        {
            var messages = await _context.ComplaintMessages
                .Select(cm => new ComplaintMessageDTO
                {
                    MessageId = cm.MessageId,
                    ComplaintId = cm.ComplaintId,
                    SenderType = cm.SenderType,
                    Message = cm.Message,
                    Image = cm.Image,
                    CreatedAt = cm.CreatedAt
                })
                .ToListAsync();

            return Ok(messages);
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        public async Task<ActionResult<ComplaintMessageDTO>> GetMessageById(long id)
        {
            var message = await _context.ComplaintMessages
                .Where(cm => cm.MessageId == id)
                .Select(cm => new ComplaintMessageDTO
                {
                    MessageId = cm.MessageId,
                    ComplaintId = cm.ComplaintId,
                    SenderType = cm.SenderType,
                    Message = cm.Message,
                    Image = cm.Image,
                    CreatedAt = cm.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (message == null) return NotFound("Message not found.");
            return Ok(message);
        }

        // ================= UPDATE =================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMessage(long id, [FromForm] CreateComplaintMessage model)
        {
            var message = await _context.ComplaintMessages.FindAsync(id);
            if (message == null) return NotFound("Message not found.");

            string? imageName = null;

            // Handle new image upload
            if (model.Image != null && model.Image.Length > 0)
            {
                var uploadPath = Path.Combine(_config["StoredFilesPath"] ?? "wwwroot/upload", "ComplaintMessages");
                Directory.CreateDirectory(uploadPath);

                var ext = Path.GetExtension(model.Image.FileName);
                imageName = Guid.NewGuid() + ext;
                var filePath = Path.Combine(uploadPath, imageName);

                using var stream = System.IO.File.Create(filePath);
                await model.Image.CopyToAsync(stream);

                // Delete old image if exists
                if (!string.IsNullOrEmpty(message.Image))
                {
                    var oldFile = Path.Combine(uploadPath, message.Image);
                    if (System.IO.File.Exists(oldFile)) System.IO.File.Delete(oldFile);
                }

                message.Image = imageName;
            }

            message.Message = model.Message ?? message.Message;
            message.SenderType = model.SenderType ?? message.SenderType;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Message updated successfully" });
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(long id)
        {
            var message = await _context.ComplaintMessages.FindAsync(id);
            if (message == null) return NotFound("Message not found.");

            // Delete image file if exists
            if (!string.IsNullOrEmpty(message.Image))
            {
                var uploadPath = Path.Combine(_config["StoredFilesPath"] ?? "wwwroot/upload", "ComplaintMessages");
                var file = Path.Combine(uploadPath, message.Image);
                if (System.IO.File.Exists(file)) System.IO.File.Delete(file);
            }

            _context.ComplaintMessages.Remove(message);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Message deleted successfully" });
        }
    }
}
