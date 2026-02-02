using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize]
    public class ShipmentController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public ShipmentController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ================= CREATE =================
        // POST: api/Shipment
        [HttpPost]
        public async Task<IActionResult> CreateShipment([FromBody] CreateShipment model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1️⃣ Validate Order
                var order = await _context.Orders
                    .Include(o => o.Shipments)
                    .FirstOrDefaultAsync(o => o.OrderId == model.OrderId);

                if (order == null)
                    return NotFound(new { error = "Order not found." });

                if (order.OrderStatus == "Cancelled")
                    return BadRequest(new { error = "Cannot create shipment for a cancelled order." });

                // Check if shipment already exists for this order
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
                    OrderId = model.OrderId,
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

                // 4️⃣ Return Response
                return Ok(new
                {
                    message = "Shipment created successfully",
                    shipmentId = shipment.ShipmentId,
                    trackingNumber = shipment.TrackingNumber
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
            // Format: SHP-yyMMddHHmmss-RND
            string prefix = "SHP";
            string timestamp = DateTime.UtcNow.ToString("yyMMddHHmmss");
            string randomPart = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();

            return $"{prefix}-{timestamp}-{randomPart}";
        }

        // ================= UPDATE =================
        // PUT: api/Shipment/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateShipment(long id, [FromBody] CreateShipment model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var shipment = await _context.Shipments
                    .Include(s => s.Order)
                    .FirstOrDefaultAsync(s => s.ShipmentId == id);

                if (shipment == null)
                    return NotFound(new { error = "Shipment not found." });

                if (shipment.Order != null && shipment.Order.OrderStatus == "Cancelled")
                    return BadRequest(new { error = "Cannot update shipment for a cancelled order." });

                // Validate Tracking Number uniqueness (if changed)
                if (!string.IsNullOrEmpty(model.TrackingNumber) &&
                    model.TrackingNumber != shipment.TrackingNumber)
                {
                    if (await _context.Shipments.AnyAsync(s => s.TrackingNumber == model.TrackingNumber && s.ShipmentId != id))
                        return BadRequest(new { error = "Tracking number already exists." });
                }

                shipment.CourierName = model.CourierName;
                shipment.ShippingCost = model.ShippingCost;
                shipment.ExpectedDeliveryDate = model.ExpectedDeliveryDate;

                // Only update tracking number if provided
                if (!string.IsNullOrEmpty(model.TrackingNumber))
                    shipment.TrackingNumber = model.TrackingNumber;

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
        // PATCH: api/Shipment/{id}/status
        // PATCH: api/Shipment/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateShipmentStatus(long id, [FromBody] UpdateShipmentStatusRequest request)
        {
            // Validate request
            if (request == null)
                return BadRequest(new { error = "Request body is required." });

            if (string.IsNullOrWhiteSpace(request.Status))
                return BadRequest(new { error = "Status is required." });

            // Validate status value
            var validStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled", "Returned" };
            if (!validStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
                return BadRequest(new
                {
                    error = "Invalid status value.",
                    validStatuses = string.Join(", ", validStatuses)
                });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Find shipment with order included
                var shipment = await _context.Shipments
                    .Include(s => s.Order)
                    .FirstOrDefaultAsync(s => s.ShipmentId == id);

                if (shipment == null)
                    return NotFound(new { error = "Shipment not found." });

                // Check if order exists and is not cancelled
                if (shipment.Order == null)
                    return BadRequest(new { error = "Associated order not found." });

                if (shipment.Order.OrderStatus == "Cancelled")
                    return BadRequest(new { error = "Cannot update shipment for a cancelled order." });

                // Prevent updating to same status
                if (shipment.Status?.ToLower() == request.Status.ToLower())
                    return BadRequest(new { error = $"Shipment is already in '{request.Status}' status." });

                // Validate status transitions
                var currentStatus = shipment.Status?.ToLower();
                var newStatus = request.Status.ToLower();

                // Define allowed transitions (you can customize this)
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

                // Update shipment
                var oldStatus = shipment.Status;
                shipment.Status = request.Status;

                // Set timestamps based on status
                if (request.Status.Equals("Shipped", StringComparison.OrdinalIgnoreCase))
                    shipment.ShippedAt = DateTime.UtcNow;
                else if (request.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
                    shipment.DeliveredAt = DateTime.UtcNow;
                else if (request.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    shipment.DeliveredAt = null;
                    shipment.ShippedAt = null;
                }

                shipment.UpdatedAt = DateTime.UtcNow;

                // Update order status if needed (optional)
                if (request.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
                {
                    shipment.Order.OrderStatus = "Delivered";
                    shipment.Order.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Log the status change (you could add a logging service here)
                Console.WriteLine($"Shipment {id} status changed from '{oldStatus}' to '{request.Status}'");

                return Ok(new
                {
                    message = $"Shipment status updated from '{oldStatus}' to '{request.Status}'.",
                    shipmentId = shipment.ShipmentId,
                    trackingNumber = shipment.TrackingNumber,
                    newStatus = request.Status,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (DbUpdateException dbEx)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    error = "Database error while updating shipment status.",
                    details = dbEx.InnerException?.Message ?? dbEx.Message
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log the full exception here (use ILogger in production)
                return StatusCode(500, new
                {
                    error = "An unexpected error occurred while updating shipment status.",
                    details = ex.Message
                });
            }
        }

        // Request model with validation attributes
        public class UpdateShipmentStatusRequest
        {
            [Required(ErrorMessage = "Status is required.")]
            [StringLength(50, MinimumLength = 1, ErrorMessage = "Status must be between 1 and 50 characters.")]
            [RegularExpression("^(Pending|Processing|Shipped|Delivered|Cancelled|Returned)$",
                ErrorMessage = "Status must be one of: Pending, Processing, Shipped, Delivered, Cancelled, Returned")]
            public string Status { get; set; } = string.Empty;

            // Optional: Add notes/reason for status change
            [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
            public string? Notes { get; set; }
        }

        // ================= DELETE =================
        // DELETE: api/Shipment/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShipment(long id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var shipment = await _context.Shipments
                    .Include(s => s.Order)
                    .FirstOrDefaultAsync(s => s.ShipmentId == id);

                if (shipment == null)
                    return NotFound(new { error = "Shipment not found." });

                if (shipment.Order != null && shipment.Order.OrderStatus == "Cancelled")
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
            try
            {
                var shipments = await _context.Shipments
                    .Include(s => s.Order)
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
            try
            {
                var shipment = await _context.Shipments
                    .Where(s => s.ShipmentId == id)
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
            try
            {
                var shipments = await _context.Shipments
                    .Where(s => s.OrderId == orderId)
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