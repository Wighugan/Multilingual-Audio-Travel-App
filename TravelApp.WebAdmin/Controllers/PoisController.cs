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
        public async Task<IActionResult> GetAllPois([FromQuery] int userId = 0, [FromQuery] string role = "")
        {
            var query = _context.Pois.AsQueryable();

            if (role == "Owner" && userId > 0)
            {
                query = query.Where(p => p.OwnerId == userId);
            }

            var allPois = await _context.Pois.ToListAsync();
            return Ok(allPois);
        }

        [HttpPost]
        public async Task<IActionResult> AddPoi([FromBody] PoiEntity newPoi)
        {
            _context.Pois.Add(newPoi);
            await _context.SaveChangesAsync();
            return Ok(newPoi);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePoi(int id, [FromQuery] int userId = 0, [FromQuery] string role = "")
        {
            var poi = await _context.Pois.FindAsync(id);
            if (poi == null) return NotFound();

            if (role == "Owner" && poi.OwnerId != userId)
            {
                return BadRequest("Lỗi: Bạn không có quyền xóa quán của người khác!");
            }

            _context.Pois.Remove(poi);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePoi(int id, [FromBody] PoiEntity updatedPoi, [FromQuery] int userId = 0, [FromQuery] string role = "")
        {
            if (id != updatedPoi.Id)
                return BadRequest("ID không khớp.");

            if (role == "Owner")
            {
                var existingPoi = await _context.Pois.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

                if (existingPoi == null || existingPoi.OwnerId != userId)
                {
                    return BadRequest("Lỗi: Bạn không có quyền sửa thông tin quán này!");
                }

                updatedPoi.OwnerId = existingPoi.OwnerId;
            }

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

        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Chưa có file nào được chọn.");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new { fileName = uniqueFileName });
        }

        // upload nhiều ảnh
        [HttpPost("upload-multiple")]
        public async Task<IActionResult> UploadMultipleImages(List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("Chưa có file nào được chọn.");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uploadedFileNames = new List<string>();

            // Lặp qua từng file để lưu
            foreach (var file in files)
            {
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                uploadedFileNames.Add(uniqueFileName);
            }

            return Ok(new { fileNames = uploadedFileNames });
        }
    }
}