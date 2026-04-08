using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelApp.WebAdmin.Data;
using TravelApp.WebAdmin.Models;

namespace TravelApp.WebAdmin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbacksController : ControllerBase
    {
        private readonly AppDbContext _context;
        public FeedbacksController(AppDbContext context) { _context = context; }

        // lấy danh sách feedback lên bảng dashboard
        [HttpGet]
        public async Task<IActionResult> GetFeedbacks()
        {
            return Ok(await _context.Feedbacks.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> AddFeedback([FromBody] FeedbackEntity feedback)
        {
            feedback.CreatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();
            return Ok(feedback);
        }

        // xóa feedback
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFeedback(int id)
        {
            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback == null) return NotFound();

            _context.Feedbacks.Remove(feedback);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}