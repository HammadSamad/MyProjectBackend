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
    public class OrderAddressController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public OrderAddressController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ✅ CREATE Order Address
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderAddress model)
        {
            if (model == null || model.OrderId <= 0 || model.AddressId <= 0)
                return BadRequest(new { message = "Invalid order address data." });

            var orderExists = await _context.Orders.AnyAsync(o => o.OrderId == model.OrderId);
            if (!orderExists)
                return NotFound(new { message = "Order not found." });

            var address = await _context.Addresses.Include(a => a.City)
                .FirstOrDefaultAsync(a => a.AddressId == model.AddressId);

            if (address == null)
                return NotFound(new { message = "Address not found." });

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

        // ✅ GET ALL Order Addresses
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var addresses = await _context.OrderAddresses
                .Include(o => o.Address)
                .ThenInclude(a => a.City)
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

        // ✅ GET Order Address by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var address = await _context.OrderAddresses
                .Include(o => o.Address)
                .ThenInclude(a => a.City)
                .Where(o => o.OrderAddressId == id)
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

        // ✅ GET Order Address by OrderId
        [HttpGet("by-order/{orderId}")]
        public async Task<IActionResult> GetByOrderId(long orderId)
        {
            var address = await _context.OrderAddresses
                .Include(o => o.Address)
                .ThenInclude(a => a.City)
                .Where(o => o.OrderId == orderId)
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
                return NotFound(new { message = "Order address not found for this order." });

            return Ok(address);
        }

        // ✅ UPDATE Order Address
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] CreateOrderAddress model)
        {
            if (model == null || model.AddressId <= 0)
                return BadRequest(new { message = "Invalid data." });

            var orderAddress = await _context.OrderAddresses.FindAsync(id);
            if (orderAddress == null)
                return NotFound(new { message = "Order address not found." });

            var addressExists = await _context.Addresses.AnyAsync(a => a.AddressId == model.AddressId);
            if (!addressExists)
                return NotFound(new { message = "Address not found." });

            orderAddress.AddressId = model.AddressId;
            orderAddress.RecipientName = model.RecipientName;
            orderAddress.Phone = model.Phone;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Order address updated successfully." });
        }

        // ✅ DELETE Order Address
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var orderAddress = await _context.OrderAddresses.FindAsync(id);
            if (orderAddress == null)
                return NotFound(new { message = "Order address not found." });

            _context.OrderAddresses.Remove(orderAddress);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order address deleted successfully." });
        }
    }
}
