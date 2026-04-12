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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserEntity updatedUser)
        {
            if (id != updatedUser.Id)
                return BadRequest("ID không khớp.");

            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null)
                return NotFound("Không tìm thấy tài khoản.");

            existingUser.FullName = updatedUser.FullName;
            existingUser.Email = updatedUser.Email;
            existingUser.Role = updatedUser.Role;
            existingUser.IsPremium = updatedUser.IsPremium;
            existingUser.PremiumToken = updatedUser.PremiumToken;
            existingUser.PremiumExpiry = updatedUser.PremiumExpiry;

            if (!string.IsNullOrWhiteSpace(updatedUser.Password))
            {
                existingUser.Password = updatedUser.Password;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            return Ok();
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