using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelApp.WebAdmin.Data;
using TravelApp.WebAdmin.Models;

namespace TravelApp.WebAdmin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FavoritesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FavoritesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetFavorites([FromQuery] string userEmail)
        {
            if (string.IsNullOrWhiteSpace(userEmail))
                return Ok(new List<FavoriteEntity>());

            var favorites = await _context.Favorites
                .Where(f => f.UserEmail == userEmail)
                .ToListAsync();

            return Ok(favorites);
        }

        [HttpPost]
        public async Task<IActionResult> AddFavorite([FromBody] FavoriteEntity favorite)
        {
            if (favorite == null || string.IsNullOrWhiteSpace(favorite.UserEmail) || string.IsNullOrWhiteSpace(favorite.PoiName))
                return BadRequest("Dữ liệu favorite không hợp lệ.");

            var exists = await _context.Favorites.AnyAsync(f =>
                f.UserEmail == favorite.UserEmail &&
                f.PoiName == favorite.PoiName);

            if (exists) return Ok(favorite);

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();
            return Ok(favorite);
        }

        [HttpDelete("by-user")]
        public async Task<IActionResult> DeleteFavoriteByUser([FromQuery] string userEmail, [FromQuery] string poiName)
        {
            if (string.IsNullOrWhiteSpace(userEmail) || string.IsNullOrWhiteSpace(poiName))
                return BadRequest("Thiếu userEmail hoặc poiName.");

            var favorite = await _context.Favorites.FirstOrDefaultAsync(f =>
                f.UserEmail == userEmail &&
                f.PoiName == poiName);

            if (favorite == null) return NotFound();

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
