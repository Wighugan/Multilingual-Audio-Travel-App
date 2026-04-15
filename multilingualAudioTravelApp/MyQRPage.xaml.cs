using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace multilingualAudioTravelApp
{
    public partial class MyQRPage : ContentPage
    {
        // Workaround: expose Title property so XAML attribute binding succeeds
        public new string Title { get; set; }

        public MyQRPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Set localized UI texts
            MyQRTitleLabel.Text = Languages.AppStrings.MyQRTitle;
            MyQRSubtitleLabel.Text = Languages.AppStrings.MyQRSubtitle;
            PremiumBadgeLabel.Text = Languages.AppStrings.PremiumBadge;
            MyQRExpiryLabel.Text = Languages.AppStrings.ExpiryLabel;
            MyQRStatusLabel.Text = Languages.AppStrings.StatusLabel;
            RenewButton.Text = Languages.AppStrings.RenewPremium;
            // set page title
            this.Title = Languages.AppStrings.MyQRTitle;

            var email = Preferences.Get("userEmail", "");
            var token = Preferences.Get($"PremiumToken_{email}", "");
            var expiry = Preferences.Get($"PremiumExpiry_{email}", "");

            // Chưa có Premium → quay về
            if (string.IsNullOrEmpty(token))
            {
                DisplayAlert("Chưa có Premium",
                    "Vui lòng đăng ký gói Premium trước.", "OK");
                Navigation.PopAsync();
                return;
            }

            // Hiện mã QR
            QRImage.Value = token;
            EmailLabel.Text = email;

            // Kiểm tra còn hạn không
            if (DateTime.TryParse(expiry, out var expDate))
            {
                var daysLeft = (expDate - DateTime.Today).Days;

                ExpiryLabel.Text = expDate.ToString("dd/MM/yyyy");

                if (daysLeft < 0)
                {
                    // Hết hạn
                    StatusLabel.Text = "Đã hết hạn";
                    StatusLabel.TextColor = Colors.Red;
                    RenewButton.IsVisible = true;
                }
                else if (daysLeft <= 7)
                {
                    // Sắp hết hạn
                    StatusLabel.Text = $"Còn {daysLeft} ngày";
                    StatusLabel.TextColor = Color.FromArgb("#FF6F00");
                    RenewButton.IsVisible = true;
                }
                else
                {
                    // Còn hạn
                    StatusLabel.Text = $"Còn {daysLeft} ngày";
                    StatusLabel.TextColor = Color.FromArgb("#1D9E75");
                }
            }
            else
            {
                ExpiryLabel.Text = expiry;
                StatusLabel.Text = "Đang hoạt động";
                StatusLabel.TextColor = Color.FromArgb("#1D9E75");
            }
        }

        private async void OnRenewClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PremiumPage());
        }
    }
}