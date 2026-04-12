using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using multilingualAudioTravelApp.Services;
using System.Net.Http.Json;

namespace multilingualAudioTravelApp
{
    public partial class PremiumPage : ContentPage
    {
        private string _selectedPlan = string.Empty;

        public PremiumPage()
        {
            InitializeComponent();
            var email = Preferences.Get("userEmail", string.Empty);

            if (Preferences.Get($"IsPremium_{email}", false))
            {
                BuyButton.Text = "✅ Đã kích hoạt Premium";
                BuyButton.BackgroundColor = Colors.Gray;
                BuyButton.IsEnabled = false;
                StatusLabel.Text = "Tài khoản của bạn đang ở gói Premium";
            }
        }

        private void OnPlanSelected(object sender, TappedEventArgs e)
        {
            if (e.Parameter is not string plan) return;
            _selectedPlan = plan;

            var borders = new[] { (BorderNam, RadioNam), (BorderThang, RadioThang), (BorderTuan, RadioTuan) };
            foreach (var (b, r) in borders)
            {
                b.Stroke = new SolidColorBrush(Color.FromArgb("#E0E0E0"));
                b.BackgroundColor = Colors.Transparent;
                r.Stroke = new SolidColorBrush(Color.FromArgb("#CCCCCC"));
                r.Fill = new SolidColorBrush(Colors.White);
            }

            var isDark = Application.Current.UserAppTheme == AppTheme.Dark;
            var (selBorder, selRadio) = _selectedPlan switch
            {
                "nam" => (BorderNam, RadioNam),
                "thang" => (BorderThang, RadioThang),
                _ => (BorderTuan, RadioTuan)
            };

            selBorder.Stroke = new SolidColorBrush(Color.FromArgb("#1D9E75"));
            selBorder.BackgroundColor = Color.FromArgb(isDark ? "#0D2B20" : "#F0FAF6");
            selRadio.Stroke = new SolidColorBrush(Color.FromArgb("#1D9E75"));
            selRadio.Fill = new SolidColorBrush(Color.FromArgb("#1D9E75"));
        }

        private async void OnBuyClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedPlan))
            {
                await DisplayAlert("Chưa chọn gói", "Vui lòng chọn gói trước khi đăng ký.", "OK");
                return;
            }

            BuyButton.IsEnabled = false;
            BuyButton.Text = "⏳ Đang xử lý...";
            await Task.Delay(1200);

            var email = Preferences.Get("userEmail", "user");

            var months = _selectedPlan == "nam" ? 12 : _selectedPlan == "thang" ? 1 : 0;
            var days = _selectedPlan == "tuan" ? 7 : 0;

            var expiry = (days > 0
                ? DateTime.Now.AddDays(days)
                : DateTime.Now.AddMonths(months)).ToString("yyyy-MM-dd");

            var token = $"VKPREMIUM_{email}_{expiry}";

            Preferences.Set($"IsPremium_{email}", true);
            Preferences.Set($"PremiumToken_{email}", token);
            Preferences.Set($"PremiumExpiry_{email}", expiry);

            try
            {
                using var client = new HttpClient();
                string baseUrl = DeviceInfo.Platform == DevicePlatform.Android
                    ? "http://10.0.2.2:5068" : "http://localhost:5068";

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

            BuyButton.Text = "✅ Đã kích hoạt Premium";
            BuyButton.BackgroundColor = Colors.Gray;
            StatusLabel.Text = "🎉 Kích hoạt thành công!";

            await DisplayAlert("Thành công",
                "Bạn đã kích hoạt gói Premium!\nVào Profile → 'QR của tôi' để xem mã QR.", "OK");

            await Navigation.PopAsync();
        }
    }
}
