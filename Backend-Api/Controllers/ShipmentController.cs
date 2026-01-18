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
            var order = await _context.Orders.FindAsync(model.OrderId);
            if (order == null)
                return NotFound("Order not found.");

            var shipment = new Shipment
            {
                OrderId = model.OrderId,
                CourierName = model.CourierName,
                TrackingNumber = model.TrackingNumber,
                ShippingCost = 0,                  // you can update later
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Shipments.Add(shipment);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Shipment created successfully",
                shipmentId = shipment.ShipmentId
            });
        }

        // ================= GET ALL =================
        // GET: api/Shipment
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShipmentDTO>>> GetAllShipments()
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

        // ================= GET BY ID =================
        // GET: api/Shipment/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ShipmentDTO>> GetShipmentById(long id)
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
                return NotFound("Shipment not found.");

            return Ok(shipment);
        }

        // ================= UPDATE =================
        // PUT: api/Shipment/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateShipment(long id, [FromBody] CreateShipment model)
        {
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null)
                return NotFound("Shipment not found.");

            shipment.CourierName = model.CourierName;
            shipment.TrackingNumber = model.TrackingNumber;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Shipment updated successfully." });
        }

        // ================= UPDATE STATUS =================
        // PATCH: api/Shipment/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateShipmentStatus(long id, [FromBody] string status)
        {
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null)
                return NotFound("Shipment not found.");

            shipment.Status = status;

            if (status == "Shipped")
                shipment.ShippedAt = DateTime.UtcNow;
            else if (status == "Delivered")
                shipment.DeliveredAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Shipment status updated to {status}" });
        }

        // ================= DELETE =================
        // DELETE: api/Shipment/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShipment(long id)
        {
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null)
                return NotFound("Shipment not found.");

            _context.Shipments.Remove(shipment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Shipment deleted successfully." });
        }
    }
}
