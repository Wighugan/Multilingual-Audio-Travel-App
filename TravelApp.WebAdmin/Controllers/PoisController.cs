using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelApp.WebAdmin.Data;
using TravelApp.WebAdmin.Models;

namespace TravelApp.WebAdmin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PoisController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PoisController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPois()
        {
            var poisFromServer = await _context.Pois.ToListAsync();
            return Ok(poisFromServer);
        }
        
        //them poi
        [HttpPost]
        public async Task<IActionResult> AddPoi([FromBody] PoiEntity newPoi)
        {
            _context.Pois.Add(newPoi);
            await _context.SaveChangesAsync();
            return Ok(newPoi);
        }

        //xoa poi
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePoi(int id)
        {
            var poi = await _context.Pois.FindAsync(id);
            if (poi == null) return NotFound();

            _context.Pois.Remove(poi);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePoi(int id, [FromBody] PoiEntity updatedPoi)
        {
            if (id != updatedPoi.Id)
                return BadRequest("ID không khớp.");
            _context.Entry(updatedPoi).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PoiExists(id)) return NotFound();
                else throw;
            }

            return Ok();
        }

        private bool PoiExists(int id)
        {
            return _context.Pois.Any(e => e.Id == id);
        }

        //them anh
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Chưa có file nào được chọn.");

            // 1. Chỉ định đường dẫn lưu vào thư mục wwwroot/images
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

            // 2. Nếu chưa có thư mục images thì tự động tạo mới
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // 3. Đổi tên file để tránh trùng lặp (ví dụ: 1234-5678_anhquoc.jpg)
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // 4. Lưu file vào ổ cứng
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 5. Trả về tên file để HTML lưu vào Database
            return Ok(new { fileName = uniqueFileName });
        }
    }
}