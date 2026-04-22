using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
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
            var allPois = await _context.Pois.ToListAsync();
            bool hasChanges = false;

            // quét kiểm tra hạn sử dụng của toàn bộ poi
            foreach (var poi in allPois)
            {
                if (poi.SubscriptionTier != "Free" && poi.SubscriptionExpiry.HasValue)
                {
                    if (DateTime.Now > poi.SubscriptionExpiry.Value)
                    {
                        // Nếu hết hạn -> Tự động giáng cấp về Free và trừ Priority
                        poi.SubscriptionTier = "Free";
                        poi.Priority = 1;
                        poi.Radius = 50;
                        hasChanges = true;
                    }
                }
            }

            // Nếu có quán nào vừa rớt hạng thì lưu lại vào DB
            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }

            var query = _context.Pois.AsQueryable();

            if (role == "Owner" && userId > 0)
            {
                query = query.Where(p => p.OwnerId == userId);
            }
            return Ok(allPois);
        }

        [HttpPost]
        public async Task<IActionResult> AddPoi([FromBody] PoiEntity newPoi)
        {
            if (string.IsNullOrEmpty(newPoi.QrCodeToken))
            {
                newPoi.QrCodeToken = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();
            }
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

            var existingPoi = await _context.Pois.FirstOrDefaultAsync(p => p.Id == id); 
            if (existingPoi == null) return NotFound();
            
            if (role == "Owner")
            {
                if (existingPoi == null || existingPoi.OwnerId != userId)
                {
                    return BadRequest("Lỗi: Bạn không có quyền sửa thông tin quán này!");
                }
                updatedPoi.OwnerId = existingPoi.OwnerId;
            }
            existingPoi.Image = updatedPoi.Image;
            existingPoi.Priority = updatedPoi.Priority;
            existingPoi.Radius = updatedPoi.Radius;
            existingPoi.SubscriptionTier = updatedPoi.SubscriptionTier;
            existingPoi.Latitude = updatedPoi.Latitude;
            existingPoi.Longitude = updatedPoi.Longitude;
            existingPoi.CooldownMinutes = updatedPoi.CooldownMinutes;
            existingPoi.TranslationsJson = updatedPoi.TranslationsJson;

            if (role != "Owner")
            {
                existingPoi.OwnerId = updatedPoi.OwnerId;
            }

//            _context.Entry(updatedPoi).State = EntityState.Modified;

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


        // API lấy bảng xếp hạng
        [HttpGet("ranking")]
        public async Task<IActionResult> GetRanking()
        {
            var pois = await _context.Pois.ToListAsync();
            var result = pois.Select(p => new
            {
                p.Id,
                p.ListenCount,
                p.VisitCount,
                // Lấy tên tiếng Việt từ TranslationsJson
                Name = TryGetViName(p.TranslationsJson)
            });
            return Ok(result);
        }

        private static string TryGetViName(string json)
        {
            try
            {
                var dict = System.Text.Json.JsonSerializer
                    .Deserialize<Dictionary<string, JsonElement>>(json ?? "{}");
                return dict?["vi"].GetProperty("Name").GetString() ?? "—";
            }
            catch { return "—"; }
        }
        [HttpPost("{id}/analytics")]
        public async Task<IActionResult> UpdateAnalytics(int id, [FromQuery] string type)
        {
            var poi = await _context.Pois.FindAsync(id);
            if (poi == null) return NotFound();

            if (type == "listen")
            {
                poi.ListenCount++;
            }
            else if (type == "visit")
            {
                poi.VisitCount++;

                // DÙNG NGÀY THÁNG NĂM CHÍNH XÁC (VD: "2026-04-20") THAY VÌ TÊN THỨ
                var today = DateTime.Now.ToString("yyyy-MM-dd");

                // Đọc dữ liệu cũ
                var dict = string.IsNullOrEmpty(poi.WeeklyVisitsJson)
                    ? new Dictionary<string, int>()
                    : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(poi.WeeklyVisitsJson);

                // Cộng thêm 1 cho ngày hôm nay
                if (dict.ContainsKey(today)) dict[today]++;
                else dict[today] = 1;

                // Lưu ngược lại thành chuỗi JSON
                poi.WeeklyVisitsJson = System.Text.Json.JsonSerializer.Serialize(dict);
            }
            else return BadRequest("type phải là 'listen' hoặc 'visit'");

            await _context.SaveChangesAsync();
            return Ok(new { poi.Id, poi.ListenCount, poi.VisitCount });
        }


        [HttpGet("weekly-chart")]
        public async Task<IActionResult> GetWeeklyChart()
        {
            var pois = await _context.Pois.ToListAsync();

            var resultDict = new Dictionary<string, int>
        {
            { "Monday", 0 }, { "Tuesday", 0 }, { "Wednesday", 0 },
            { "Thursday", 0 }, { "Friday", 0 }, { "Saturday", 0 }, { "Sunday", 0 }
        };

            // Tính ra ngày bắt đầu (Thứ 2) và kết thúc (Chủ Nhật) của tuần hiện tại
            DateTime today = DateTime.Today;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime startOfWeek = today.AddDays(-1 * diff).Date;
            DateTime endOfWeek = startOfWeek.AddDays(6).Date;

            foreach (var poi in pois)
            {
                if (!string.IsNullOrEmpty(poi.WeeklyVisitsJson) && poi.WeeklyVisitsJson != "{}")
                {
                    try
                    {
                        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(poi.WeeklyVisitsJson);
                        foreach (var kvp in dict)
                        {
                            // Giải mã ngày và kiểm tra xem có nằm trong tuần hiện tại không
                            if (DateTime.TryParse(kvp.Key, out DateTime visitDate))
                            {
                                if (visitDate >= startOfWeek && visitDate <= endOfWeek)
                                {
                                    string dayName = visitDate.DayOfWeek.ToString(); // Biến ngày thành "Monday", "Tuesday"...
                                    if (resultDict.ContainsKey(dayName))
                                    {
                                        resultDict[dayName] += kvp.Value;
                                    }
                                }
                            }
                        }
                    }
                    catch { /* Bỏ qua nếu JSON bị lỗi */ }
                }
            }

            var result = new int[] {
            resultDict["Monday"], resultDict["Tuesday"], resultDict["Wednesday"],
            resultDict["Thursday"], resultDict["Friday"], resultDict["Saturday"], resultDict["Sunday"]
        };

            return Ok(result);
        }

        // API: GET /api/pois/verify/{token}
        [HttpGet("verify/{token}")]
        public async Task<IActionResult> VerifyQrToken(string token)
        {
            // Tra cứu Token trong bảng POIs
            var poi = await _context.Pois.FirstOrDefaultAsync(p => p.QrCodeToken == token);

            if (poi == null)
            {
                // Trả về lỗi
                return NotFound(new { message = "Mã QR không tồn tại trong hệ thống." });
            }
            return Ok(poi);
        }


        [HttpPost("{id}/generate-qr")]
        public async Task<IActionResult> GenerateQrForExistingPoi(int id)
        {
            var poi = await _context.Pois.FindAsync(id);
            if (poi == null) return NotFound();

            // Tạo Token cho quán cũ
            poi.QrCodeToken = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();
            await _context.SaveChangesAsync();

            return Ok(new { token = poi.QrCodeToken });
        }

        [HttpPost("{id}/upgrade")]
        public async Task<IActionResult> UpgradePoiSubscription(int id, [FromQuery] string tier, [FromQuery] string duration)
        {
            var poi = await _context.Pois.FindAsync(id);
            if (poi == null) return NotFound("Không tìm thấy quán.");
            bool isChangingTier = poi.SubscriptionTier != tier;
            // Tính toán ngày hết hạn
            DateTime newExpiry = poi.SubscriptionExpiry != null && poi.SubscriptionExpiry > DateTime.Now
                                 ? poi.SubscriptionExpiry.Value
                                 : DateTime.Now;

            if (duration.ToLower() == "month")
            {
                newExpiry = newExpiry.AddMonths(1);
            }
            else if (duration.ToLower() == "year")
            {
                newExpiry = newExpiry.AddYears(1);
            }
            else
            {
                return BadRequest("Thời hạn (duration) chỉ chấp nhận 'month' hoặc 'year'.");
            }

            // Nâng cấp Gói và gán điểm Priority tương ứng
            poi.SubscriptionTier = tier; // "Pro" hoặc "Max"
            poi.SubscriptionExpiry = newExpiry;

            if (tier.ToLower() == "pro")
            {
                poi.Priority = 10;
                poi.Radius = 100;
            }
            else if (tier.ToLower() == "max")
            {
                poi.Priority = 20;
                poi.Radius = 150;
            }
            else return BadRequest("Gói (tier) chỉ chấp nhận 'Pro' hoặc 'Max'.");

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Đã nâng cấp quán lên gói {tier.ToUpper()} thành công!",
                expiry = poi.SubscriptionExpiry
            });
        }
    }
}