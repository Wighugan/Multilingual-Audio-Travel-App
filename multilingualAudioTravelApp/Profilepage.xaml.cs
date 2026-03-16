namespace multilingualAudioTravelApp;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
        BindingContext = new ProfileViewModel();
    }

    private async void OnMenuSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not ProfileMenu menu)
            return;

        // bỏ highlight
        ((CollectionView)sender).SelectedItem = null;

        switch (menu.Key)
        {
            case "edit":
                await Shell.Current.GoToAsync(nameof(EditProfilePage));
            break;

            case "language":
                string action = await DisplayActionSheet(
                multilingualAudioTravelApp.Languages.AppStrings.SelectLanguage,
                multilingualAudioTravelApp.Languages.AppStrings.Cancel,
                null,
                "Tiếng Việt", "English", "日本語"/*, "한국어"*/);

                string langCode = "vi";
                if (action == "English") langCode = "en";
                else if (action == "日本語") langCode = "ja";
/*                else if (action == "한국어") langCode = "ko";*/
                else if (action == multilingualAudioTravelApp.Languages.AppStrings.Cancel || string.IsNullOrEmpty(action)) return;
                Preferences.Set("AppLanguage", langCode);
                var culture = new System.Globalization.CultureInfo(langCode);
                multilingualAudioTravelApp.Languages.AppStrings.Culture = culture;
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;
                System.Threading.Thread.CurrentThread.CurrentUICulture = culture;

                await DisplayAlert(multilingualAudioTravelApp.Languages.AppStrings.Success, multilingualAudioTravelApp.Languages.AppStrings.UpdateLanguage, multilingualAudioTravelApp.Languages.AppStrings.ok);

                Application.Current.MainPage = new AppShell();
            break;

            case "favorite":
                await Shell.Current.GoToAsync("//FavoritePage");
                break;

            case "feedback":
                await DisplayAlert("Đánh giá & Góp ý", "Chức năng đang phát triển", "OK");
                break;

            default:
                await DisplayAlert(menu.Title, "Chức năng đang phát triển", "OK");
                break;
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            multilingualAudioTravelApp.Languages.AppStrings.LogOut,
            multilingualAudioTravelApp.Languages.AppStrings.AskLogOut,
            multilingualAudioTravelApp.Languages.AppStrings.LogOut,
            multilingualAudioTravelApp.Languages.AppStrings.Cancel);

        if (!confirm) return;

        // xoá trạng thái login (giả lập)
        Preferences.Remove("isLoggedIn");

        // QUAN TRỌNG: reset toàn bộ Shell
        Application.Current.MainPage = new NavigationPage(new LoginPage());
    }
}
