namespace multilingualAudioTravelApp;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        string action = await DisplayActionSheet("Chọn ngôn ngữ giọng đọc:", "Hủy", null, "Tiếng Việt", "English");

        if (action == "Tiếng Việt")
        {
            Preferences.Set("VoiceLanguage", "vi");
            await DisplayAlert("Thông báo", "Đã chuyển sang giọng Tiếng Việt", "OK");
        }
        else if (action == "English")
        {
            Preferences.Set("VoiceLanguage", "en");
            await DisplayAlert("Notification", "Switched to English voice", "OK");
        }
    }
}