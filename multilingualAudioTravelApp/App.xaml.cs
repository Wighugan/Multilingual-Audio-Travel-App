using multilingualAudioTravelApp.Services;

namespace multilingualAudioTravelApp
{
    public partial class App : Application
    {
        private readonly DatabaseService _dbService = new DatabaseService();
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }
        protected override async void OnStart()
        {
            base.OnStart();
            await SilentLoginOrRegisterAsync();
        }
        private async Task SilentLoginOrRegisterAsync()
        {
            // 1. Kiểm tra xem máy này đã có Device ID chưa, chưa có thì tạo mới
            string deviceId = Preferences.Get("DeviceId", null);
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = Guid.NewGuid().ToString("N"); // Tạo 1 chuỗi mã độc nhất
                Preferences.Set("DeviceId", deviceId);
            }

            // 2. Chế tạo Email và Password giả từ Device ID
            string dummyEmail = $"device_{deviceId}@tour.app";
            string dummyPassword = deviceId; // Dùng luôn ID làm mật khẩu cho chắc chắn

            // 3. Thử Đăng nhập ngầm
            var user = await _dbService.LoginAsync(dummyEmail, dummyPassword);

            if (user == null)
            {
                // 4. Nếu đăng nhập thất bại (Máy mới tải app lần đầu) -> Tự động Đăng ký ngầm
                string guestName = $"Khách_{deviceId.Substring(0, 5)}"; // Tên hiển thị: Khách_a1b2c

                // Lưu ý: Đảm bảo bạn có hàm RegisterAsync trong DatabaseService
                user = await _dbService.RegisterAsync(guestName, dummyEmail, dummyPassword, "Customer");
            }

            // 5. Lưu thông tin và Bật SignalR báo Online
            if (user != null)
            {
                Preferences.Set("isLoggedIn", true);
                Preferences.Set("userId", user.Id);
                Preferences.Set("userEmail", user.Email);
                Preferences.Set("userName", user.FullName);

                var signalR = IPlatformApplication.Current?.Services.GetService<SignalRService>();
                if (signalR != null)
                {
                    await signalR.ConnectAsync(user.Id);
                }
            }
        }
        protected override void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);

            if (uri.Scheme.ToLower() == "vktour" && uri.Host.ToLower() == "poi")
            {
                string idStr = uri.Query.Replace("?id=", "");

                if (!string.IsNullOrEmpty(idStr))
                {
                    Preferences.Set("ScannedPoiId", idStr);

                    Shell.Current.GoToAsync("//ExplorePage");
                }
            }
        }
        protected override async void OnSleep()
        {
            base.OnSleep();

            int userId = Preferences.Get("userId", 0);
            if (userId > 0)
            {
                var signalR = IPlatformApplication.Current?.Services.GetService<SignalRService>();
                if (signalR != null)
                {
                    await signalR.DisconnectAsync(userId);
                }
            }
        }

        protected override async void OnResume()
        {
            base.OnResume();

            int userId = Preferences.Get("userId", 0);
            if (userId > 0)
            {
                var signalR = IPlatformApplication.Current?.Services.GetService<SignalRService>();
                if (signalR != null)
                {
                    await signalR.ConnectAsync(userId);
                }
            }
        }
    }
}