namespace multilingualAudioTravelApp;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
        BindingContext = new ProfileViewModel();
        _stars = new List<Label> { S1, S2, S3, S4, S5 }; 
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
                "Tiếng Việt", "English", "日本語", "中国人"/*, "한국어"*/);

                string langCode = "vi";
                if (action == "English") langCode = "en";
                else if (action == "日本語") langCode = "ja";
                else if (action == "中国人") langCode = "zh";
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
                await ShowFeedbackPopup();
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


    private int _feedbackRating = 0;
    private List<Label> _stars;

   

    // Mở popup feedback
    private async Task ShowFeedbackPopup()
    {
        _feedbackRating = 0;
        FeedbackEditor.Text = "";
        FeedbackErrorLabel.IsVisible = false;
        foreach (var s in _stars) s.Text = "☆";
        FeedbackOverlay.IsVisible = true;
    }

    // Bấm sao
    private void OnStarTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not string param) return;
        _feedbackRating = int.Parse(param);

        for (int i = 0; i < _stars.Count; i++)
            _stars[i].Text = i < _feedbackRating ? "★" : "☆";

    }

    // Nút Hủy
    private void OnFeedbackCancelClicked(object sender, EventArgs e)
    {
        FeedbackOverlay.IsVisible = false;
    }

    // Nút Gửi
    private async void OnFeedbackSubmitClicked(object sender, EventArgs e)
    {
        if (_feedbackRating == 0)
        {
            FeedbackErrorLabel.Text = "Vui lòng chọn số sao!";
            FeedbackErrorLabel.IsVisible = true;
            return;
        }

        FeedbackErrorLabel.IsVisible = false;

        var email = Preferences.Get("userEmail", "anonymous");
        var content = FeedbackEditor.Text?.Trim() ?? "";
        var db = new Services.DatabaseService();

        await db.SaveFeedbackAsync(email, _feedbackRating, content);
        FeedbackOverlay.IsVisible = false;

        await DisplayAlert("Cảm ơn!",
            $"Bạn đã đánh giá {new string('★', _feedbackRating)}{new string('☆', 5 - _feedbackRating)}\n" +
            (!string.IsNullOrEmpty(content) ? "Góp ý đã được ghi nhận." : ""),
            "OK");
    }


}
