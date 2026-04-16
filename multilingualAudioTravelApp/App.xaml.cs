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
        protected override void OnStart()
        {
            base.OnStart();
            Task.Run(async () =>
            {
                await SilentLoginOrRegisterAsync();
            });
        }
        private async Task SilentLoginOrRegisterAsync()
        {
            try { 
            string deviceId = Preferences.Get("DeviceId", "");
            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = Guid.NewGuid().ToString("N").Substring(0, 12); // Tạo mã 12 ký tự
                Preferences.Set("DeviceId", deviceId);
            }

            int savedUserId = Preferences.Get("userId", 0);

            if (savedUserId == 0)
            {
                string dummyEmail = $"device_{deviceId}@tour.app";
                string guestName = $"Khách_{deviceId.Substring(0, 4)}";

                var newUser = await _dbService.RegisterAsync(guestName, dummyEmail, "123456", "Customer");

                if (newUser != null)
                {
                    Preferences.Set("userId", newUser.Id);
                    Preferences.Set("userName", newUser.FullName);
                    Preferences.Set("userEmail", newUser.Email);
                    savedUserId = newUser.Id;
                }
            }

            if (savedUserId > 0)
            {
                var signalR = IPlatformApplication.Current?.Services.GetService<SignalRService>();
                if (signalR != null) await signalR.ConnectAsync(savedUserId);
            }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LỖI KHI ĐĂNG NHẬP NGẦM HOẶC KẾT NỐI SIGNALR: {ex.Message}");
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