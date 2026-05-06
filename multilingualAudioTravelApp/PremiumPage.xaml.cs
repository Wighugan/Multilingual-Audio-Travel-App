using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using multilingualAudioTravelApp.Services;
using System.Net.Http.Json;

namespace multilingualAudioTravelApp
{
    public partial class PremiumPage : ContentPage
    {
        public PremiumPage()
        {
            InitializeComponent();
            var email = Preferences.Get("userEmail", string.Empty);

            // ĐỒNG BỘ TOÀN BỘ SANG TIẾNG VIỆT
            PremiumMainTitle.Text = "Kích hoạt Tour";

            // Header của bảng
            FeaturesLabel.Text = "Quyền lợi";
            FreeLabel.Text = "Xem thử";
            PaidLabel.Text = "Trọn gói";

            // Nội dung 4 dòng quyền lợi (Viết đúng theo chức năng App của bạn)
            Feature1Label.Text = "Xem bản đồ và hình ảnh các quán ăn";
            Feature2Label.Text = "Mở khóa toàn bộ Audio thuyết minh";
            Feature3Label.Text = "Tự động phát Audio qua GPS khi đi dạo";
            Feature4Label.Text = "Không giới hạn số lần bấm nghe thủ công";

            BuyButton.Text = "Thanh toán ngay";

            // Nếu đã có vé rồi thì mờ nút đi
            if (Preferences.Get($"IsPremium_{email}", false))
            {
                BuyButton.Text = "Đã kích hoạt vé";
                BuyButton.BackgroundColor = Colors.Gray;
                BuyButton.IsEnabled = false;
                StatusLabel.Text = "Hệ thống Audio Guide đang hoạt động";
            }
        }

        // Đã xóa hàm OnPlanSelected vì không cần chọn gói nữa

        private async void OnBuyClicked(object sender, EventArgs e)
        {
            BuyButton.IsEnabled = false;
            BuyButton.Text = Languages.AppStrings.BtnProcessing;
            await Task.Delay(1200); // Giả lập thời gian kết nối ví điện tử/ngân hàng

            var email = Preferences.Get("userEmail", "user");

            // Kịch bản: Thanh toán 1 lần dùng mãi mãi -> Set hạn sử dụng là 10 năm sau
            var expiry = DateTime.Now.AddYears(10).ToString("yyyy-MM-dd");
            var token = $"VKPREMIUM_{email}_{expiry}";

            // Lưu vé vào bộ nhớ tạm của điện thoại
            Preferences.Set($"IsPremium_{email}", true);
            Preferences.Set($"PremiumToken_{email}", token);
            Preferences.Set($"PremiumExpiry_{email}", expiry);

            try
            {
                // Lưu lên Database Server
                using var client = new HttpClient();
                string baseUrl = DatabaseService.GlobalApiUrl;

                var users = await client.GetFromJsonAsync<List<UserEntity>>($"{baseUrl}/api/users");
                var user = users?.FirstOrDefault(u => u.Email == email);
                if (user != null)
                {
                    user.IsPremium = true;
                    user.PremiumToken = token;
                    user.PremiumExpiry = expiry;
                    await client.PutAsJsonAsync($"{baseUrl}/api/users/{user.Id}", user);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lưu Premium lên server thất bại: {ex.Message}");
            }

            StatusLabel.Text = Languages.AppStrings.StatusActivated;

            await DisplayAlert("Thành công", "Bạn đã mua vé thành công. Chào mừng đến với Khu phố ẩm thực Vĩnh Khánh!", "Vào App");

            // Mở cổng cho khách vào App chính
            Application.Current.MainPage = new AppShell();
        }
    }
}