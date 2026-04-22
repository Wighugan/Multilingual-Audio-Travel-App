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

            // Set localized UI texts
            PremiumMainTitle.Text = Languages.AppStrings.PremiumTitle;
            FeaturesLabel.Text = Languages.AppStrings.Features;
            FreeLabel.Text = Languages.AppStrings.Free;
            PaidLabel.Text = Languages.AppStrings.Paid;
            Feature1Label.Text = Languages.AppStrings.Feature1;
            Feature2Label.Text = Languages.AppStrings.Feature2;
            Feature3Label.Text = Languages.AppStrings.Feature3;
            Feature4Label.Text = Languages.AppStrings.Feature4;
            PlanYearLabel.Text = Languages.AppStrings.PlanYear;
            PlanMonthLabel.Text = Languages.AppStrings.PlanMonth;
            PlanWeekLabel.Text = Languages.AppStrings.PlanWeek;
            BuyButton.Text = Languages.AppStrings.SubscribeButton;

            if (Preferences.Get($"IsPremium_{email}", false))
            {
                BuyButton.Text = Languages.AppStrings.BtnPremiumActivated;
                BuyButton.BackgroundColor = Colors.Gray;
                BuyButton.IsEnabled = false;
                StatusLabel.Text = Languages.AppStrings.StatusPremiumActive;
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
                await DisplayAlert(Languages.AppStrings.MsgSelectPlanTitle, Languages.AppStrings.MsgSelectPlanBody, "OK");
                return;
            }

            BuyButton.IsEnabled = false;
            BuyButton.Text = Languages.AppStrings.BtnProcessing;
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

            BuyButton.Text = Languages.AppStrings.BtnPremiumActivated;
            BuyButton.BackgroundColor = Colors.Gray;
            StatusLabel.Text = Languages.AppStrings.StatusActivated;

            await DisplayAlert(Languages.AppStrings.MsgActivatedTitle,
                Languages.AppStrings.MsgActivatedBody, "OK");

            await Navigation.PopAsync();
        }
    }
}
