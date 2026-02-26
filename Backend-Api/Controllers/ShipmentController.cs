using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // JWT authentication enabled
    public class ShipmentController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public ShipmentController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ================= HELPER: GET USER ID FROM JWT =================
        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out userId);
        }

        // ================= CREATE =================
        [HttpPost]
        public async Task<IActionResult> CreateShipment([FromBody] CreateShipment model)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized(new { error = "Invalid or missing JWT token." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1️⃣ Get latest pending order for this user
                var order = await _context.Orders
                    .Include(o => o.Shipments)
                    .Where(o => o.UserId == userId && o.OrderStatus != "Cancelled")
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();

                if (order == null)
                    return NotFound(new { error = "No valid orders found for shipment." });

                if (order.Shipments.Any())
                    return BadRequest(new { error = "A shipment already exists for this order." });

                // 2️⃣ Generate Unique Tracking Number
                string trackingNumber;
                do
                {
                    trackingNumber = GenerateTrackingNumber();
                } while (await _context.Shipments.AnyAsync(s => s.TrackingNumber == trackingNumber));

                // 3️⃣ Create Shipment
                var shipment = new Shipment
                {
                    OrderId = order.OrderId,
                    CourierName = model.CourierName,
                    TrackingNumber = trackingNumber,
                    ShippingCost = model.ShippingCost,
                    Status = model.Status ?? "Pending",
                    ExpectedDeliveryDate = model.ExpectedDeliveryDate,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Shipments.Add(shipment);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Shipment created successfully",
                    shipmentId = shipment.ShipmentId,
                    trackingNumber = shipment.TrackingNumber,
                    orderId = order.OrderId
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Failed to create shipment.", details = ex.Message });
            }
        }

        // ================= Helper Method =================
        private string GenerateTrackingNumber()
        {
            string prefix = "SHP";
            string timestamp = DateTime.UtcNow.ToString("yyMMddHHmmss");
            string randomPart = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();
            return $"{prefix}-{timestamp}-{randomPart}";
        }

        // ================= UPDATE =================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateShipment(long id, [FromBody] CreateShipment model)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized(new { error = "Invalid or missing JWT token." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var shipment = await _context.Shipments
                    .Include(s => s.Order)
                    .FirstOrDefaultAsync(s => s.ShipmentId == id && s.Order.UserId == userId);

                if (shipment == null)
                    return NotFound(new { error = "Shipment not found or does not belong to you." });

                if (shipment.Order.OrderStatus == "Cancelled")
                    return BadRequest(new { error = "Cannot update shipment for a cancelled order." });

                if (!string.IsNullOrEmpty(model.TrackingNumber) &&
                    model.TrackingNumber != shipment.TrackingNumber)
                {
                    if (await _context.Shipments.AnyAsync(s => s.TrackingNumber == model.TrackingNumber && s.ShipmentId != id))
                        return BadRequest(new { error = "Tracking number already exists." });
                }

                shipment.CourierName = model.CourierName;
                shipment.ShippingCost = model.ShippingCost;
                shipment.ExpectedDeliveryDate = model.ExpectedDeliveryDate;

                if (!string.IsNullOrEmpty(model.TrackingNumber))
                    shipment.TrackingNumber = model.TrackingNumber;

                shipment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Shipment updated successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Failed to update shipment.", details = ex.Message });
            }
        }

        // ================= UPDATE STATUS =================
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateShipmentStatus(long id, [FromBody] UpdateShipmentStatusRequest request)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized(new { error = "Invalid or missing JWT token." });

            if (request == null || string.IsNullOrWhiteSpace(request.Status))
                return BadRequest(new { error = "Status is required." });

            var validStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled", "Returned" };
            if (!validStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
                return BadRequest(new { error = "Invalid status value.", validStatuses = string.Join(", ", validStatuses) });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var shipment = await _context.Shipments
                    .Include(s => s.Order)
                    .FirstOrDefaultAsync(s => s.ShipmentId == id && s.Order.UserId == userId);

                if (shipment == null)
                    return NotFound(new { error = "Shipment not found or does not belong to you." });

                if (shipment.Order.OrderStatus == "Cancelled")
                    return BadRequest(new { error = "Cannot update shipment for a cancelled order." });

                if (shipment.Status?.ToLower() == request.Status.ToLower())
                    return BadRequest(new { error = $"Shipment is already in '{request.Status}' status." });

                var currentStatus = shipment.Status?.ToLower();
                var newStatus = request.Status.ToLower();

                var allowedTransitions = new Dictionary<string, List<string>>
                {
                    { "pending", new List<string> { "processing", "cancelled" } },
                    { "processing", new List<string> { "shipped", "cancelled" } },
                    { "shipped", new List<string> { "delivered", "returned" } },
                    { "delivered", new List<string> { "returned" } },
                    { "cancelled", new List<string>() },
                    { "returned", new List<string>() }
                };

                if (!string.IsNullOrEmpty(currentStatus) &&
                    allowedTransitions.ContainsKey(currentStatus) &&
                    !allowedTransitions[currentStatus].Contains(newStatus))
                {
                    return BadRequest(new
                    {
                        error = $"Cannot transition from '{currentStatus}' to '{newStatus}'.",
                        allowedNextStatuses = allowedTransitions[currentStatus]
                    });
                }

                var oldStatus = shipment.Status;
                shipment.Status = request.Status;

                if (request.Status.Equals("Shipped", StringComparison.OrdinalIgnoreCase))
                    shipment.ShippedAt = DateTime.UtcNow;
                else if (request.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
                {
                    shipment.DeliveredAt = DateTime.UtcNow;
                    shipment.Order.OrderStatus = "Delivered";
                    shipment.Order.UpdatedAt = DateTime.UtcNow;
                }
                else if (request.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    shipment.ShippedAt = null;
                    shipment.DeliveredAt = null;
                }

                shipment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = $"Shipment status updated from '{oldStatus}' to '{request.Status}'.",
                    shipmentId = shipment.ShipmentId,
                    trackingNumber = shipment.TrackingNumber,
                    newStatus = request.Status,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Failed to update shipment status.", details = ex.Message });
            }
        }

        public class UpdateShipmentStatusRequest
        {
            [Required]
            [StringLength(50, MinimumLength = 1)]
            [RegularExpression("^(Pending|Processing|Shipped|Delivered|Cancelled|Returned)$")]
            public string Status { get; set; } = string.Empty;

            [StringLength(500)]
            public string? Notes { get; set; }
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShipment(long id)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized(new { error = "Invalid or missing JWT token." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var shipment = await _context.Shipments
                    .Include(s => s.Order)
                    .FirstOrDefaultAsync(s => s.ShipmentId == id && s.Order.UserId == userId);

                if (shipment == null)
                    return NotFound(new { error = "Shipment not found or does not belong to you." });

                if (shipment.Order.OrderStatus == "Cancelled")
                    return BadRequest(new { error = "Cannot delete shipment for a cancelled order." });

                _context.Shipments.Remove(shipment);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Shipment deleted successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Failed to delete shipment.", details = ex.Message });
            }
        }

        // ================= GET ALL =================
        [HttpGet]
        public async Task<IActionResult> GetAllShipments()
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized(new { error = "Invalid or missing JWT token." });

            try
            {
                var shipments = await _context.Shipments
                    .Include(s => s.Order)
                    .Where(s => s.Order.UserId == userId)
                    .Select(s => new ShipmentDTO
                    {
                        ShipmentId = s.ShipmentId,
                        OrderId = s.OrderId,
                        CourierName = s.CourierName,
                        TrackingNumber = s.TrackingNumber,
                        ShippingCost = s.ShippingCost,
                        Status = s.Status,
                        ExpectedDeliveryDate = s.ExpectedDeliveryDate,
                        ShippedAt = s.ShippedAt,
                        DeliveredAt = s.DeliveredAt,
                        CreatedAt = s.CreatedAt
                    })
                    .ToListAsync();

                return Ok(shipments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch shipments.", details = ex.Message });
            }
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetShipmentById(long id)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized(new { error = "Invalid or missing JWT token." });

            try
            {
                var shipment = await _context.Shipments
                    .Include(s => s.Order)
                    .Where(s => s.ShipmentId == id && s.Order.UserId == userId)
                    .Select(s => new ShipmentDTO
                    {
                        ShipmentId = s.ShipmentId,
                        OrderId = s.OrderId,
                        CourierName = s.CourierName,
                        TrackingNumber = s.TrackingNumber,
                        ShippingCost = s.ShippingCost,
                        Status = s.Status,
                        ExpectedDeliveryDate = s.ExpectedDeliveryDate,
                        ShippedAt = s.ShippedAt,
                        DeliveredAt = s.DeliveredAt,
                        CreatedAt = s.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (shipment == null)
                    return NotFound(new { error = "Shipment not found." });

                return Ok(shipment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch shipment.", details = ex.Message });
            }
        }

        // ================= GET SHIPMENTS BY ORDER ID =================
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetShipmentsByOrderId(long orderId)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized(new { error = "Invalid or missing JWT token." });

            try
            {
                var shipments = await _context.Shipments
                    .Include(s => s.Order)
                    .Where(s => s.OrderId == orderId && s.Order.UserId == userId)
                    .Select(s => new ShipmentDTO
                    {
                        ShipmentId = s.ShipmentId,
                        OrderId = s.OrderId,
                        CourierName = s.CourierName,
                        TrackingNumber = s.TrackingNumber,
                        ShippingCost = s.ShippingCost,
                        Status = s.Status,
                        ExpectedDeliveryDate = s.ExpectedDeliveryDate,
                        ShippedAt = s.ShippedAt,
                        DeliveredAt = s.DeliveredAt,
                        CreatedAt = s.CreatedAt
                    })
                    .ToListAsync();

                if (!shipments.Any())
                    return NotFound(new { error = "No shipments found for this order." });

                return Ok(shipments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch shipments.", details = ex.Message });
            }
        }
    }
}