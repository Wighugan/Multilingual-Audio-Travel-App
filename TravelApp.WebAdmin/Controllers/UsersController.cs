using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelApp.WebAdmin.Data;
using TravelApp.WebAdmin.Models;

namespace TravelApp.WebAdmin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        public UsersController(AppDbContext context) { _context = context; }

        // lấy danh sách users đưa lên web admin
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            return Ok(await _context.Users.ToListAsync());
        }

        // tạo user mới từ web admin hoặc từ app
        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] UserEntity user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(user);
        }

        // xóa user
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}