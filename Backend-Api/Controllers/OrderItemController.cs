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
    [Authorize] // 🔐 JWT Enabled
    public class OrderItemController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public OrderItemController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ================= HELPER: GET USERID =================
        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out userId);
        }

        // ================= CREATE =================
        [HttpPost]
        public async Task<IActionResult> CreateOrderItem([FromBody] CreateOrderitem model)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            if (model == null)
                return BadRequest(new { message = "Request body cannot be empty." });

            if (model.Quantity <= 0)
                return BadRequest(new { message = "Quantity must be greater than zero." });

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == model.OrderId && o.UserId == userId);
            if (order == null)
                return BadRequest(new { message = "Order does not exist or access denied." });

            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                    .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(v => v.VariantId == model.VariantId);

            if (variant == null)
                return BadRequest(new { message = "Product Variant does not exist." });

            decimal finalPrice = CalculateFinalPrice(variant);

            var orderItem = new OrderItem
            {
                OrderId = model.OrderId,
                VariantId = model.VariantId,
                Quantity = model.Quantity,
                Price = finalPrice,
                VariantSpecificationOptionsId = model.VariantSpecificationOptionsId,
                CreatedAt = DateTime.UtcNow
            };

            _context.OrderItems.Add(orderItem);

            // Remove Cart Item if exists
            var cartItem = await _context.CartItems.FirstOrDefaultAsync(ci => ci.CartItemId == model.CartItemId);
            if (cartItem != null)
                _context.CartItems.Remove(cartItem);

            // Update UserRecentOrder
            var recentOrder = await _context.UserRecentOrders
                .FirstOrDefaultAsync(r => r.UserId == userId && r.OrderId == order.OrderId);

            if (recentOrder != null)
                recentOrder.CreatedAt = DateTime.UtcNow;
            else
                _context.UserRecentOrders.Add(new UserRecentOrder
                {
                    UserId = userId,
                    OrderId = order.OrderId,
                    CreatedAt = DateTime.UtcNow
                });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Order item added successfully.",
                orderItemId = orderItem.OrderItemId
            });
        }

        // ================= GET ALL (Optimized) =================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderItemDTO>>> GetAllOrderItems()
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            var items = await _context.OrderItems
                .Where(oi => oi.Order.UserId == userId)
                .Include(oi => oi.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.ProductImages)
                .Include(oi => oi.Variant)
                    .ThenInclude(v => v.VariantSpecificationOptions)
                        .ThenInclude(vso => vso.Option)
                            .ThenInclude(o => o.Specification)
                .ToListAsync();

            if (!items.Any())
                return NotFound(new { message = "No order items found." });

            var result = items.Select(oi =>
            {
                var variant = oi.Variant;
                var product = variant.Product;

                string coverImage = product.ProductImages.FirstOrDefault(i => i.IsCover == true)?.ImageUrl ?? "";
                decimal finalPrice = CalculateFinalPrice(variant);

                var specs = variant.VariantSpecificationOptions
                    .Where(vso => vso.Option.Specification.SpecificationName is "RAM" or "Storage" or "Color")
                    .Select(vso => new VariantSpecificationOptionDTO
                    {
                        OptionId = vso.OptionId,
                        SpecificationName = vso.Option.Specification.SpecificationName,
                        OptionValue = vso.Option.OptionValue
                    }).ToList();

                return new OrderItemDTO
                {
                    OrderItemId = oi.OrderItemId,
                    OrderId = oi.OrderId,
                    VariantId = oi.VariantId,
                    Quantity = oi.Quantity,
                    Price = finalPrice,
                    CreatedAt = oi.CreatedAt,
                    ProductName = product.ProductName,
                    Image = coverImage,
                    VariantSpecifications = specs,
                    VariantSpecificationOptionsId = oi.VariantSpecificationOptionsId
                };
            }).ToList();

            return Ok(result);
        }

        // ================= GET BY ORDER (Optimized) =================
        [HttpGet("order/{orderId}")]
        public async Task<ActionResult<IEnumerable<OrderItemDTO>>> GetItemsByOrder(long orderId)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            var items = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId && oi.Order.UserId == userId)
                .Include(oi => oi.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.ProductImages)
                .Include(oi => oi.Variant)
                    .ThenInclude(v => v.VariantSpecificationOptions)
                        .ThenInclude(vso => vso.Option)
                            .ThenInclude(o => o.Specification)
                .ToListAsync();

            if (!items.Any())
                return NotFound(new { message = "No order items found for this order." });

            var result = items.Select(oi =>
            {
                var variant = oi.Variant;
                var product = variant.Product;

                string coverImage = product.ProductImages.FirstOrDefault(i => i.IsCover == true)?.ImageUrl ?? "";
                decimal finalPrice = CalculateFinalPrice(variant);

                var specs = variant.VariantSpecificationOptions
                    .Where(vso => vso.Option.Specification.SpecificationName is "RAM" or "Storage" or "Color")
                    .Select(vso => new VariantSpecificationOptionDTO
                    {
                        OptionId = vso.OptionId,
                        SpecificationName = vso.Option.Specification.SpecificationName,
                        OptionValue = vso.Option.OptionValue
                    }).ToList();

                return new OrderItemDTO
                {
                    OrderItemId = oi.OrderItemId,
                    OrderId = oi.OrderId,
                    VariantId = oi.VariantId,
                    Quantity = oi.Quantity,
                    Price = finalPrice,
                    CreatedAt = oi.CreatedAt,
                    ProductName = product.ProductName,
                    Image = coverImage,
                    VariantSpecifications = specs,
                    VariantSpecificationOptionsId = oi.VariantSpecificationOptionsId
                };
            }).ToList();

            return Ok(result);
        }

        // ================= GET BY ID =================
        [HttpGet("{id}/details")]
        public async Task<ActionResult<OrderItemDTO>> GetOrderItemById(long id)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            var item = await _context.OrderItems
                .Include(oi => oi.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.ProductImages)
                .Include(oi => oi.Variant)
                    .ThenInclude(v => v.VariantSpecificationOptions)
                        .ThenInclude(vso => vso.Option)
                            .ThenInclude(o => o.Specification)
                .FirstOrDefaultAsync(oi => oi.OrderItemId == id && oi.Order.UserId == userId);

            if (item == null)
                return NotFound(new { message = "Order item not found or access denied." });

            var variant = item.Variant;
            var product = variant.Product;

            string coverImage = product.ProductImages.FirstOrDefault(i => i.IsCover == true)?.ImageUrl ?? "";
            decimal finalPrice = CalculateFinalPrice(variant);

            var specs = variant.VariantSpecificationOptions
                .Where(vso => vso.Option.Specification.SpecificationName is "RAM" or "Storage" or "Color")
                .Select(vso => new VariantSpecificationOptionDTO
                {
                    OptionId = vso.OptionId,
                    SpecificationName = vso.Option.Specification.SpecificationName,
                    OptionValue = vso.Option.OptionValue
                }).ToList();

            var dto = new OrderItemDTO
            {
                OrderItemId = item.OrderItemId,
                OrderId = item.OrderId,
                VariantId = item.VariantId,
                Quantity = item.Quantity,
                Price = finalPrice,
                CreatedAt = item.CreatedAt,
                ProductName = product.ProductName,
                Image = coverImage,
                VariantSpecifications = specs,
                VariantSpecificationOptionsId = item.VariantSpecificationOptionsId
            };

            return Ok(dto);
        }

        // ================= UPDATE =================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrderItem(long id, [FromBody] CreateOrderitem model)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            var orderItem = await _context.OrderItems
                .Include(oi => oi.Order)
                .FirstOrDefaultAsync(oi => oi.OrderItemId == id && oi.Order.UserId == userId);

            if (orderItem == null)
                return NotFound(new { message = "Order item not found or access denied." });

            var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.VariantId == model.VariantId);
            if (variant == null)
                return BadRequest(new { message = "Product Variant does not exist." });

            orderItem.VariantId = model.VariantId;
            orderItem.Quantity = model.Quantity;
            orderItem.VariantSpecificationOptionsId = model.VariantSpecificationOptionsId;
            orderItem.Price = CalculateFinalPrice(variant);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Order item updated successfully." });
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrderItem(long id)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            var orderItem = await _context.OrderItems
                .Include(oi => oi.Order)
                .FirstOrDefaultAsync(oi => oi.OrderItemId == id && oi.Order.UserId == userId);

            if (orderItem == null)
                return NotFound(new { message = "Order item not found or access denied." });

            _context.OrderItems.Remove(orderItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Order item deleted successfully." });
        }

        // ================= HELPER: CALCULATE FINAL PRICE =================
        private decimal CalculateFinalPrice(ProductVariant variant)
        {
            decimal finalPrice = variant.Price ?? 0;
            var now = DateTime.UtcNow;

            if ((!variant.DiscountStart.HasValue || variant.DiscountStart.Value <= now) &&
                (!variant.DiscountEnd.HasValue || variant.DiscountEnd.Value >= now))
            {
                if (variant.DiscountPercentage.HasValue && variant.DiscountPercentage.Value > 0)
                    finalPrice -= finalPrice * (variant.DiscountPercentage.Value / 100);

                if (variant.DiscountAmount.HasValue && variant.DiscountAmount.Value > 0)
                    finalPrice -= variant.DiscountAmount.Value;
            }

            return finalPrice < 0 ? 0 : finalPrice;
        }
    }
}