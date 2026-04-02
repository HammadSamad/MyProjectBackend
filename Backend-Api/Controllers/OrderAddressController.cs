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
    [Authorize] // 🔐 JWT enabled
    public class OrderAddressController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public OrderAddressController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ================= HELPER =================
        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out userId);
        }

        // ================= CREATE =================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderAddress model)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            if (model == null || model.OrderId <= 0 || model.AddressId <= 0)
                return BadRequest(new { message = "Invalid order address data." });

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == model.OrderId && o.UserId == userId);

            if (order == null)
                return NotFound(new { message = "Order not found or access denied." });

            var addressExists = await _context.Addresses
                .AnyAsync(a => a.AddressId == model.AddressId && a.UserId == userId);

            if (!addressExists)
                return NotFound(new { message = "Address not found or access denied." });

            var orderAddress = new OrderAddress
            {
                OrderId = model.OrderId,
                AddressId = model.AddressId,
                RecipientName = model.RecipientName,
                Phone = model.Phone,
                CreatedAt = DateTime.UtcNow
            };

            _context.OrderAddresses.Add(orderAddress);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Order address created successfully.",
                orderAddressId = orderAddress.OrderAddressId
            });
        }

        // ================= GET ALL =================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            var addresses = await _context.OrderAddresses
                .Include(o => o.Address)
                .ThenInclude(a => a.City)
                .Where(o => o.Order.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderAdressDTO
                {
                    OrderAddressId = o.OrderAddressId,
                    OrderId = o.OrderId,
                    AddressId = o.AddressId,
                    RecipientName = o.RecipientName,
                    Phone = o.Phone,
                    CreatedAt = o.CreatedAt,
                    AddressLine1 = o.Address.AddressLine1,
                    Label = o.Address.AddressLine2,
                    CityName = o.Address.City.CityName
                })
                .ToListAsync();

            return Ok(addresses);
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            var address = await _context.OrderAddresses
                .Include(o => o.Address)
                .ThenInclude(a => a.City)
                .Where(o => o.OrderAddressId == id && o.Order.UserId == userId)
                .Select(o => new OrderAdressDTO
                {
                    OrderAddressId = o.OrderAddressId,
                    OrderId = o.OrderId,
                    AddressId = o.AddressId,
                    RecipientName = o.RecipientName,
                    Phone = o.Phone,
                    CreatedAt = o.CreatedAt,
                    AddressLine1 = o.Address.AddressLine1,
                    Label = o.Address.AddressLine2,
                    CityName = o.Address.City.CityName
                })
                .FirstOrDefaultAsync();

            if (address == null)
                return NotFound(new { message = "Order address not found." });

            return Ok(address);
        }

        // ================= GET BY ORDER =================
        [HttpGet("by-order/{orderId}")]
        public async Task<IActionResult> GetByOrderId(long orderId)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            var address = await _context.OrderAddresses
                .Include(o => o.Address)
                .ThenInclude(a => a.City)
                .Where(o => o.OrderId == orderId && o.Order.UserId == userId)
                .Select(o => new OrderAdressDTO
                {
                    OrderAddressId = o.OrderAddressId,
                    OrderId = o.OrderId,
                    AddressId = o.AddressId,
                    RecipientName = o.RecipientName,
                    Phone = o.Phone,
                    CreatedAt = o.CreatedAt,
                    AddressLine1 = o.Address.AddressLine1,
                    Label = o.Address.AddressLine2,
                    CityName = o.Address.City.CityName
                })
                .FirstOrDefaultAsync();

            if (address == null)
                return NotFound(new { message = "Order address not found." });

            return Ok(address);
        }

        // ================= UPDATE =================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] CreateOrderAddress model)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            var orderAddress = await _context.OrderAddresses
                .Include(o => o.Order)
                .FirstOrDefaultAsync(o => o.OrderAddressId == id && o.Order.UserId == userId);

            if (orderAddress == null)
                return NotFound(new { message = "Order address not found." });

            orderAddress.AddressId = model.AddressId;
            orderAddress.RecipientName = model.RecipientName;
            orderAddress.Phone = model.Phone;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Order address updated successfully." });
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            var orderAddress = await _context.OrderAddresses
                .Include(o => o.Order)
                .FirstOrDefaultAsync(o => o.OrderAddressId == id && o.Order.UserId == userId);

            if (orderAddress == null)
                return NotFound(new { message = "Order address not found." });

            _context.OrderAddresses.Remove(orderAddress);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order address deleted successfully." });
        }
    }
}