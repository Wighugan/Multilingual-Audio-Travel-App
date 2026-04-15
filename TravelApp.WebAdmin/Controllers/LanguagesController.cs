using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelApp.WebAdmin.Data;
using TravelApp.WebAdmin.Models;

namespace TravelApp.WebAdmin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LanguagesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LanguagesController(AppDbContext context) => _context = context;

        // lấy danh sách
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LanguageEntity>>> GetLanguages()
            => await _context.Languages.ToListAsync();

        // thêm mới
        [HttpPost]
        public async Task<ActionResult<LanguageEntity>> PostLanguage(LanguageEntity lang)
        {
            _context.Languages.Add(lang);
            await _context.SaveChangesAsync();
            return Ok(lang);
        }

        // cập nhật
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLanguage(int id, [FromBody] LanguageEntity updatedLang)
        {
            if (id != updatedLang.Id) return BadRequest("ID không khớp.");

            _context.Entry(updatedLang).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LanguageExists(id)) return NotFound();
                else throw;
            }

            return Ok();
        }

        // xóa
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLanguage(int id)
        {
            var lang = await _context.Languages.FindAsync(id);
            if (lang == null) return NotFound();

            _context.Languages.Remove(lang);
            await _context.SaveChangesAsync();
            return Ok();
        }
        private bool LanguageExists(int id)
        {
            return _context.Languages.Any(e => e.Id == id);
        }
    }
}