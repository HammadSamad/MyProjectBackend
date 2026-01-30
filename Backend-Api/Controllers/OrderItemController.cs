using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderItemController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public OrderItemController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ================= CREATE =================
        [HttpPost]
        public async Task<IActionResult> CreateOrderItem([FromBody] CreateOrderitem model)
        {
            if (model == null)
                return BadRequest(new { message = "Request body cannot be empty." });

            if (model.Quantity <= 0)
                return BadRequest(new { message = "Quantity must be greater than zero." });

            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == model.OrderId);
                if (order == null)
                    return BadRequest(new { message = "Order does not exist." });

                var variant = await _context.ProductVariants
                    .Include(v => v.Product)
                        .ThenInclude(p => p.ProductImages)
                    .FirstOrDefaultAsync(v => v.VariantId == model.VariantId);

                if (variant == null)
                    return BadRequest(new { message = "Product Variant does not exist." });

                // Calculate final price automatically
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

                // Update UserRecentOrder
                var recentOrder = await _context.UserRecentOrders
                    .FirstOrDefaultAsync(r => r.UserId == order.UserId && r.OrderId == order.OrderId);

                if (recentOrder != null)
                    recentOrder.CreatedAt = DateTime.UtcNow;
                else
                    _context.UserRecentOrders.Add(new UserRecentOrder
                    {
                        UserId = order.UserId,
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
            catch (DbUpdateException)
            {
                return StatusCode(500, new { message = "Database error occurred while creating the order item." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error occurred.", details = ex.Message });
            }
        }

        // ================= GET ALL =================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderItemDTO>>> GetAllOrderItems()
        {
            try
            {
                var items = await _context.OrderItems
                    .Include(oi => oi.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.ProductImages)
                    .ToListAsync();

                if (!items.Any())
                    return NotFound(new { message = "No order items found." });

                return Ok(items.Select(MapToDTO).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch order items.", details = ex.Message });
            }
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderItemDTO>> GetOrderItemById(long id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid order item ID." });

            try
            {
                var item = await _context.OrderItems
                    .Include(oi => oi.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.ProductImages)
                    .FirstOrDefaultAsync(oi => oi.OrderItemId == id);

                if (item == null)
                    return NotFound(new { message = "Order item not found." });

                return Ok(MapToDTO(item));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch order item.", details = ex.Message });
            }
        }

        // ================= GET BY ORDER =================
        [HttpGet("order/{orderId}")]
        public async Task<ActionResult<IEnumerable<OrderItemDTO>>> GetItemsByOrder(long orderId)
        {
            if (orderId <= 0)
                return BadRequest(new { message = "Invalid order ID." });

            try
            {
                var items = await _context.OrderItems
                    .Where(oi => oi.OrderId == orderId)
                    .Include(oi => oi.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.ProductImages)
                    .ToListAsync();

                if (!items.Any())
                    return NotFound(new { message = "No order items found for this order." });

                return Ok(items.Select(MapToDTO).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch order items by order.", details = ex.Message });
            }
        }

        // ================= UPDATE =================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrderItem(long id, [FromBody] CreateOrderitem model)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid order item ID." });

            if (model == null)
                return BadRequest(new { message = "Request body cannot be empty." });

            if (model.Quantity <= 0)
                return BadRequest(new { message = "Quantity must be greater than zero." });

            try
            {
                var orderItem = await _context.OrderItems.FindAsync(id);
                if (orderItem == null)
                    return NotFound(new { message = "Order item not found." });

                var variant = await _context.ProductVariants
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(v => v.VariantId == model.VariantId);

                if (variant == null)
                    return BadRequest(new { message = "Product Variant does not exist." });

                orderItem.VariantId = model.VariantId;
                orderItem.Quantity = model.Quantity;
                orderItem.VariantSpecificationOptionsId = model.VariantSpecificationOptionsId;

                // Automatically update price based on variant
                orderItem.Price = CalculateFinalPrice(variant);

                await _context.SaveChangesAsync();

                return Ok(new { message = "Order item updated successfully." });
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { message = "Database error occurred while updating the order item." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error occurred.", details = ex.Message });
            }
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrderItem(long id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid order item ID." });

            try
            {
                var orderItem = await _context.OrderItems.FindAsync(id);
                if (orderItem == null)
                    return NotFound(new { message = "Order item not found." });

                _context.OrderItems.Remove(orderItem);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Order item deleted successfully." });
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new { message = "Database error occurred while deleting the order item." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error occurred.", details = ex.Message });
            }
        }

        // ================= HELPER: MAP DTO =================
        private OrderItemDTO MapToDTO(OrderItem oi)
        {
            var variant = oi.Variant;
            var product = variant.Product;

            string coverImage = product.ProductImages.FirstOrDefault(i => i.IsCover == true)?.ImageUrl ?? "";

            decimal finalPrice = CalculateFinalPrice(variant);

            var specs = _context.VariantSpecificationOptions
                .Where(vso => vso.VariantId == oi.VariantId &&
                             (vso.Option.Specification.SpecificationName == "RAM" ||
                              vso.Option.Specification.SpecificationName == "Storage" ||
                              vso.Option.Specification.SpecificationName == "Color"))
                .Select(vso => new VariantSpecificationOptionDTO
                {
                    OptionId = vso.OptionId,
                    SpecificationName = vso.Option.Specification.SpecificationName,
                    OptionValue = vso.Option.OptionValue
                })
                .ToList();

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
