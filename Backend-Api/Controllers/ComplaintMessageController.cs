using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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

        private async Task<string?> SaveMultipleImages(List<IFormFile>? images, string folder)
        {
            if (images == null || images.Count == 0) return null;

            try
            {
                var uploadPath = Path.Combine(_config["StoredFilesPath"] ?? "wwwroot/upload", folder);
                Directory.CreateDirectory(uploadPath);

                List<string> fileNames = new();

                foreach (var file in images)
                {
                    var ext = Path.GetExtension(file.FileName).ToLower();

                    // Only allow image files
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
                    if (!allowedExtensions.Contains(ext))
                        throw new Exception($"File '{file.FileName}' has unsupported format.");

                    var fileName = Guid.NewGuid() + ext;
                    var filePath = Path.Combine(uploadPath, fileName);

                    using var stream = System.IO.File.Create(filePath);
                    await file.CopyToAsync(stream);

                    fileNames.Add(fileName);
                }

                return JsonSerializer.Serialize(fileNames);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save images: {ex.Message}");
            }
        }

        // ================= CREATE =================
        [HttpPost]
        public async Task<IActionResult> AddMessage([FromForm] CreateComplaintMessage model)
        {
            try
            {
                if (model == null)
                    return BadRequest(new { error = "Message data is required." });

                var complaint = await _context.Complaints.FindAsync(model.ComplaintId);
                if (complaint == null) return NotFound(new { error = "Complaint not found." });

                var imageJson = await SaveMultipleImages(model.Images, "ComplaintMessages");

                var message = new ComplaintMessage
                {
                    ComplaintId = model.ComplaintId,
                    SenderType = model.SenderType,
                    Message = model.Message,
                    Image = imageJson,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ComplaintMessages.Add(message);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Message added successfully", messageId = message.MessageId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to add message", details = ex.Message });
            }
        }

        // ================= GET ALL =================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ComplaintMessageDTO>>> GetAllMessages()
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch messages", details = ex.Message });
            }
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        public async Task<ActionResult<ComplaintMessageDTO>> GetMessageById(long id)
        {
            try
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

                if (message == null)
                    return NotFound(new { error = "Message not found." });

                return Ok(message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch message", details = ex.Message });
            }
        }

        // ================= UPDATE =================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMessage(long id, [FromForm] CreateComplaintMessage model)
        {
            try
            {
                var message = await _context.ComplaintMessages.FindAsync(id);
                if (message == null) return NotFound(new { error = "Message not found." });

                if (model.Images != null && model.Images.Count > 0)
                {
                    var imageJson = await SaveMultipleImages(model.Images, "ComplaintMessages");
                    message.Image = imageJson;
                }

                message.Message = model.Message ?? message.Message;
                message.SenderType = model.SenderType ?? message.SenderType;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Message updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to update message", details = ex.Message });
            }
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(long id)
        {
            try
            {
                var message = await _context.ComplaintMessages.FindAsync(id);
                if (message == null) return NotFound(new { error = "Message not found." });

                _context.ComplaintMessages.Remove(message);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Message deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to delete message", details = ex.Message });
            }
        }
    }
}
