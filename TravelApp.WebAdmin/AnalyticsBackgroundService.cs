using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using TravelApp.WebAdmin.Data;
using TravelApp.WebAdmin.Hubs;

namespace TravelApp.WebAdmin.Services
{
    // 1. CÁI RỔ ĐỰNG YÊU CẦU (QUEUE MẶC ĐỊNH TRÊN RAM)
    public static class AnalyticsQueue
    {
        // ConcurrentQueue giúp nhận hàng ngàn request cùng lúc mà không bị kẹt
        public static readonly ConcurrentQueue<(int PoiId, string Type)> Requests = new();
    }

    // 2. ANH CÔNG NHÂN CHẠY NGẦM GOM ĐƠN (Chạy mỗi 5 giây)
    public class AnalyticsBackgroundWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public AnalyticsBackgroundWorker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Cứ nghỉ 5 giây rồi mới làm việc 1 lần
                await Task.Delay(5000, stoppingToken);

                if (!AnalyticsQueue.Requests.IsEmpty)
                {
                    await ProcessQueueAsync();
                }
            }
        }

        private async Task ProcessQueueAsync()
        {
            var batch = new List<(int PoiId, string Type)>();

            // Lấy tất cả yêu cầu đang có trong rổ ra
            while (AnalyticsQueue.Requests.TryDequeue(out var request))
            {
                batch.Add(request);
            }

            if (!batch.Any()) return;
            // 1. GHI LOG MÀU VÀNG KHI BẮT ĐẦU GOM ĐƠN
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[BACKGROUND WORKER] Đã gom {batch.Count} yêu cau tu Queue. Đang tinh toan...");
            Console.ResetColor();
            // Gom nhóm theo từng quán (Để thay vì +1 đứt quãng, ta +500 một lần)
            var grouped = batch.GroupBy(x => x.PoiId);

            // Xin phép mở kết nối Database
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<AppHub>>();

            bool hasChanges = false;
            var today = DateTime.Now.ToString("yyyy-MM-dd");

            foreach (var group in grouped)
            {
                var poi = await context.Pois.FindAsync(group.Key);
                if (poi == null) continue;

                int listenBumps = group.Count(x => x.Type == "listen");
                int visitBumps = group.Count(x => x.Type == "visit");

                if (listenBumps > 0) poi.ListenCount += listenBumps;
                if (visitBumps > 0)
                {
                    poi.VisitCount += visitBumps;

                    // Cập nhật biểu đồ tuần
                    var dict = string.IsNullOrEmpty(poi.WeeklyVisitsJson)
                        ? new Dictionary<string, int>()
                        : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(poi.WeeklyVisitsJson);

                    if (dict.ContainsKey(today)) dict[today] += visitBumps;
                    else dict[today] = visitBumps;

                    poi.WeeklyVisitsJson = System.Text.Json.JsonSerializer.Serialize(dict);
                }
                hasChanges = true;
            }

            // Ghi vào Database ĐÚNG 1 LẦN DUY NHẤT cho hàng ngàn request
            if (hasChanges)
            {
                await context.SaveChangesAsync();

                //  cho Web Admin biết để vẽ lại biểu đồ
                await hubContext.Clients.All.SendAsync("ReceiveAnalyticsUpdate");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[DATABASE] Đã lưu thanh cong tong cong {batch.Count} luot tuong tac vao Database CÙNG 1 LÚC!\n");
                Console.ResetColor();
            }
        }
    }
}