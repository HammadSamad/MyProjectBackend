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
    public class SearchHistoryController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public SearchHistoryController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // GET: api/SearchHistory
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SearchHistoryDTO>>> GetAll()
        {
            try
            {
                var histories = await _context.SearchHistories
                    .Select(h => new SearchHistoryDTO
                    {
                        SearchId = h.SearchId,
                        UserId = h.UserId,
                        SearchText = h.SearchText,
                        SearchedAt = h.SearchedAt
                    })
                    .ToListAsync();

                return Ok(histories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch search history.", details = ex.Message });
            }
        }

        // GET: api/SearchHistory/user/5
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<SearchHistoryDTO>>> GetByUser(int userId)
        {
            try
            {
                var histories = await _context.SearchHistories
                    .Where(h => h.UserId == userId)
                    .Select(h => new SearchHistoryDTO
                    {
                        SearchId = h.SearchId,
                        UserId = h.UserId,
                        SearchText = h.SearchText,
                        SearchedAt = h.SearchedAt
                    })
                    .ToListAsync();

                if (!histories.Any())
                    return NotFound(new { error = "No search history found for this user." });

                return Ok(histories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch search history for user.", details = ex.Message });
            }
        }

        // POST: api/SearchHistory
        [HttpPost]
        public async Task<ActionResult<SearchHistoryDTO>> Create([FromBody] CreateSearchHistory model)
        {
            try
            {
                int? userId = HttpContext.Items["UserId"] as int?;
                if (userId == null)
                    return Unauthorized(new { error = "User not logged in." });

                var history = new SearchHistory
                {
                    UserId = userId.Value,
                    SearchText = model.SearchText,
                    SearchedAt = DateTime.UtcNow
                };

                _context.SearchHistories.Add(history);
                await _context.SaveChangesAsync();

                var dto = new SearchHistoryDTO
                {
                    SearchId = history.SearchId,
                    UserId = history.UserId,
                    SearchText = history.SearchText,
                    SearchedAt = history.SearchedAt
                };

                return CreatedAtAction(nameof(GetByUser), new { userId = userId }, dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to create search history.", details = ex.Message });
            }
        }

        // DELETE: api/SearchHistory/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var history = await _context.SearchHistories.FindAsync(id);
                if (history == null)
                    return NotFound(new { error = "Search history not found." });

                _context.SearchHistories.Remove(history);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to delete search history.", details = ex.Message });
            }
        }

        // DELETE: api/SearchHistory/user/5/clear
        [HttpDelete("user/{userId}/clear")]
        public async Task<IActionResult> ClearUserHistory(int userId)
        {
            try
            {
                int? authUserId = HttpContext.Items["UserId"] as int?;
                if (authUserId == null || authUserId != userId)
                    return Unauthorized(new { error = "You can only clear your own search history." });

                var histories = await _context.SearchHistories
                    .Where(h => h.UserId == userId)
                    .ToListAsync();

                if (!histories.Any())
                    return NotFound(new { error = "No search history found for this user." });

                _context.SearchHistories.RemoveRange(histories);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to clear search history.", details = ex.Message });
            }
        }
    }
}
