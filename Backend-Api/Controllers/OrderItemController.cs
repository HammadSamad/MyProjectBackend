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
    public class OrderItemController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public OrderItemController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ================= CREATE =================
        // POST: api/OrderItem
        [HttpPost]
        public async Task<IActionResult> CreateOrderItem([FromBody] CreateOrderitem model)
        {
            if (model == null)
                return BadRequest(new { message = "Request body cannot be empty." });

            if (model.Quantity <= 0)
                return BadRequest(new { message = "Quantity must be greater than zero." });

            if (model.Price <= 0)
                return BadRequest(new { message = "Price must be greater than zero." });

            try
            {
                // Check Order
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == model.OrderId);
                if (order == null)
                    return BadRequest(new { message = "Order does not exist." });

                // Check Product Variant
                var variantExists = await _context.ProductVariants
                    .AnyAsync(v => v.VariantId == model.VariantId);

                if (!variantExists)
                    return BadRequest(new { message = "Product Variant does not exist." });

                var orderItem = new OrderItem
                {
                    OrderId = model.OrderId,
                    VariantId = model.VariantId,
                    Quantity = model.Quantity,
                    Price = model.Price,
                    CreatedAt = DateTime.UtcNow
                };

                _context.OrderItems.Add(orderItem);

                // -------------------------------
                // Update UserRecentOrder
                // -------------------------------
                var recentOrder = await _context.UserRecentOrders
                    .FirstOrDefaultAsync(r => r.UserId == order.UserId && r.OrderId == order.OrderId);

                if (recentOrder != null)
                {
                    recentOrder.CreatedAt = DateTime.UtcNow;
                }
                else
                {
                    var newRecent = new UserRecentOrder
                    {
                        UserId = order.UserId,
                        OrderId = order.OrderId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.UserRecentOrders.Add(newRecent);
                }

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
        // GET: api/OrderItem
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderItemDTO>>> GetAllOrderItems()
        {
            try
            {
                var items = await _context.OrderItems
                    .Select(oi => new OrderItemDTO
                    {
                        OrderItemId = oi.OrderItemId,
                        OrderId = oi.OrderId,
                        VariantId = oi.VariantId,
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                        CreatedAt = oi.CreatedAt
                    })
                    .ToListAsync();

                if (items.Count == 0)
                    return NotFound(new { message = "No order items found." });

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch order items.", details = ex.Message });
            }
        }

        // ================= GET BY ID =================
        // GET: api/OrderItem/5
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderItemDTO>> GetOrderItemById(long id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid order item ID." });

            try
            {
                var item = await _context.OrderItems
                    .Where(oi => oi.OrderItemId == id)
                    .Select(oi => new OrderItemDTO
                    {
                        OrderItemId = oi.OrderItemId,
                        OrderId = oi.OrderId,
                        VariantId = oi.VariantId,
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                        CreatedAt = oi.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (item == null)
                    return NotFound(new { message = "Order item not found." });

                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch order item.", details = ex.Message });
            }
        }

        // ================= GET BY ORDER =================
        // GET: api/OrderItem/order/10
        [HttpGet("order/{orderId}")]
        public async Task<ActionResult<IEnumerable<OrderItemDTO>>> GetItemsByOrder(long orderId)
        {
            if (orderId <= 0)
                return BadRequest(new { message = "Invalid order ID." });

            try
            {
                var items = await _context.OrderItems
                    .Where(oi => oi.OrderId == orderId)
                    .Select(oi => new OrderItemDTO
                    {
                        OrderItemId = oi.OrderItemId,
                        OrderId = oi.OrderId,
                        VariantId = oi.VariantId,
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                        CreatedAt = oi.CreatedAt
                    })
                    .ToListAsync();

                if (items.Count == 0)
                    return NotFound(new { message = "No order items found for this order." });

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch order items by order.", details = ex.Message });
            }
        }

        // ================= UPDATE =================
        // PUT: api/OrderItem/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrderItem(long id, [FromBody] CreateOrderitem model)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid order item ID." });

            if (model == null)
                return BadRequest(new { message = "Request body cannot be empty." });

            if (model.Quantity <= 0)
                return BadRequest(new { message = "Quantity must be greater than zero." });

            if (model.Price <= 0)
                return BadRequest(new { message = "Price must be greater than zero." });

            try
            {
                var orderItem = await _context.OrderItems.FindAsync(id);
                if (orderItem == null)
                    return NotFound(new { message = "Order item not found." });

                var variantExists = await _context.ProductVariants
                    .AnyAsync(v => v.VariantId == model.VariantId);

                if (!variantExists)
                    return BadRequest(new { message = "Product Variant does not exist." });

                orderItem.VariantId = model.VariantId;
                orderItem.Quantity = model.Quantity;
                orderItem.Price = model.Price;

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
        // DELETE: api/OrderItem/5
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
    }
}
