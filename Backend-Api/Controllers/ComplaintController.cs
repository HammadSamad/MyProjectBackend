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
    public class ComplaintController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;
        private readonly IConfiguration _config;

        public ComplaintController(LaptopHarbourDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // ================= CREATE =================
        [HttpPost]
        public async Task<IActionResult> CreateComplaint([FromForm] CreateComplaint model)
        {
            string? imageName = null;

            if (model.Image != null && model.Image.Length > 0)
            {
                var uploadPath = Path.Combine(_config["StoredFilesPath"] ?? "wwwroot/upload", "Complaints");
                Directory.CreateDirectory(uploadPath);

                var ext = Path.GetExtension(model.Image.FileName);
                imageName = Guid.NewGuid() + ext;
                var filePath = Path.Combine(uploadPath, imageName);

                using var stream = System.IO.File.Create(filePath);
                await model.Image.CopyToAsync(stream);
            }

            var complaint = new Complaint
            {
                UserId = model.UserId,
                OrderId = model.OrderId,
                Subject = model.Subject,
                Description = model.Description,
                Priority = model.Priority,
                Status = "Pending",
                Image = imageName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Complaints.Add(complaint);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Complaint created successfully", complaintId = complaint.ComplaintId });
        }

        // ================= GET ALL =================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ComplaintDTO>>> GetAllComplaints()
        {
            var complaints = await _context.Complaints
                .Select(c => new ComplaintDTO
                {
                    ComplaintId = c.ComplaintId,
                    UserId = c.UserId,
                    OrderId = c.OrderId,
                    Subject = c.Subject,
                    Description = c.Description,
                    Status = c.Status,
                    Priority = c.Priority,
                    Image = c.Image,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();

            return Ok(complaints);
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        public async Task<ActionResult<ComplaintDTO>> GetComplaintById(long id)
        {
            var complaint = await _context.Complaints
                .Where(c => c.ComplaintId == id)
                .Select(c => new ComplaintDTO
                {
                    ComplaintId = c.ComplaintId,
                    UserId = c.UserId,
                    OrderId = c.OrderId,
                    Subject = c.Subject,
                    Description = c.Description,
                    Status = c.Status,
                    Priority = c.Priority,
                    Image = c.Image,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (complaint == null) return NotFound("Complaint not found.");
            return Ok(complaint);
        }

        // ================= UPDATE =================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComplaint(long id, [FromForm] CreateComplaint model)
        {
            var complaint = await _context.Complaints.FindAsync(id);
            if (complaint == null) return NotFound("Complaint not found.");

            // Handle new image upload
            if (model.Image != null && model.Image.Length > 0)
            {
                var uploadPath = Path.Combine(_config["StoredFilesPath"] ?? "wwwroot/upload", "Complaints");
                Directory.CreateDirectory(uploadPath);

                var ext = Path.GetExtension(model.Image.FileName);
                var imageName = Guid.NewGuid() + ext;
                var filePath = Path.Combine(uploadPath, imageName);

                using var stream = System.IO.File.Create(filePath);
                await model.Image.CopyToAsync(stream);

                // Delete old image
                if (!string.IsNullOrEmpty(complaint.Image))
                {
                    var oldFile = Path.Combine(uploadPath, complaint.Image);
                    if (System.IO.File.Exists(oldFile)) System.IO.File.Delete(oldFile);
                }

                complaint.Image = imageName;
            }

            complaint.Subject = model.Subject ?? complaint.Subject;
            complaint.Description = model.Description ?? complaint.Description;
            complaint.Priority = model.Priority ?? complaint.Priority;
            complaint.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Complaint updated successfully" });
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComplaint(long id)
        {
            var complaint = await _context.Complaints.FindAsync(id);
            if (complaint == null) return NotFound("Complaint not found.");

            // Delete image file
            if (!string.IsNullOrEmpty(complaint.Image))
            {
                var uploadPath = Path.Combine(_config["StoredFilesPath"] ?? "wwwroot/upload", "Complaints");
                var file = Path.Combine(uploadPath, complaint.Image);
                if (System.IO.File.Exists(file)) System.IO.File.Delete(file);
            }

            _context.Complaints.Remove(complaint);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Complaint deleted successfully" });
        }
    }
}
