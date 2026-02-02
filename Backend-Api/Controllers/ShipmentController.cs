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
                var order = await _context.Orders.FindAsync(model.OrderId);
                if (order == null)
                    return NotFound(new { error = "Order not found." });

                if (order.OrderStatus == "Cancelled")
                    return BadRequest(new { error = "Cannot create shipment for a cancelled order." });

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
                    ShippingCost = 0,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
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
                var shipment = await _context.Shipments.FindAsync(id);
                if (shipment == null)
                    return NotFound(new { error = "Shipment not found." });

                shipment.CourierName = model.CourierName;
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
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateShipmentStatus(long id, [FromBody] string status)
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

                shipment.Status = status;

                if (status == "Shipped")
                    shipment.ShippedAt = DateTime.UtcNow;
                else if (status == "Delivered")
                    shipment.DeliveredAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = $"Shipment status updated to {status}." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Failed to update shipment status.", details = ex.Message });
            }
        }

        // ================= DELETE =================
        // DELETE: api/Shipment/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShipment(long id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var shipment = await _context.Shipments.FindAsync(id);
                if (shipment == null)
                    return NotFound(new { error = "Shipment not found." });

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
                    .Select(s => new ShipmentDTO
                    {
                        ShipmentId = s.ShipmentId,
                        OrderId = s.OrderId,
                        CourierName = s.CourierName,
                        TrackingNumber = s.TrackingNumber,
                        Status = s.Status
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
                        Status = s.Status
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
    }
}
