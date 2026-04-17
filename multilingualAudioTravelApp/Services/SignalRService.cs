using Microsoft.AspNetCore.SignalR.Client;

namespace multilingualAudioTravelApp.Services
{
    public class SignalRService
    {
        private HubConnection _hubConnection;
        private readonly string _hubUrl = "http://10.0.2.2:5068/apphub"; // Máy ảo Android
        public event Action<int> OnProfileUpdated;

        public SignalRService()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_hubUrl)
                .WithAutomaticReconnect() // Tự động kết nối lại nếu rớt mạng
                .Build();

            // Đăng ký các sự kiện lắng nghe từ Server
            _hubConnection.On<int>("ReceiveProfileUpdate", async (userId) =>
            {
                MainThread.BeginInvokeOnMainThread(async () => {
                    OnProfileUpdated?.Invoke(userId);
                    // Lấy ID người dùng hiện tại đang đăng nhập trong App
                    int currentAppUserId = Preferences.Get("userId", 0);

                    if (userId == currentAppUserId)
                    {
                        await Application.Current.MainPage.DisplayAlert("Thông báo", "Thông tin tài khoản của bạn đã được cập nhật!", "OK");
                    }
                });
            });
        }

        public async Task ConnectAsync(int userId)
        {
            try
            {
                if (_hubConnection.State == HubConnectionState.Disconnected)
                {
                    await _hubConnection.StartAsync();
                }

                if (_hubConnection.State == HubConnectionState.Connected)
                {
                    await _hubConnection.InvokeAsync("UpdateUserStatus", userId, true);
                    System.Diagnostics.Debug.WriteLine($"[SIGNALR] Đã báo Online cho User: {userId}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LỖI KẾT NỐI SIGNALR: {ex.Message}");
            }
        }

        public async Task DisconnectAsync(int userId)
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                // Báo Offline trước khi ngắt
                await _hubConnection.InvokeAsync("UpdateUserStatus", userId, false);
                await _hubConnection.StopAsync();
            }
        }
        public async Task SendLocationAsync(
        string identifier, double lat, double lng)
        {
            if (_hubConnection?.State != HubConnectionState.Connected) return;
            try
            {
                await _hubConnection.InvokeAsync(
                    "UpdateUserLocation", identifier, lat, lng);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] Lỗi gửi vị trí: {ex.Message}");
            }
        }

    }
}