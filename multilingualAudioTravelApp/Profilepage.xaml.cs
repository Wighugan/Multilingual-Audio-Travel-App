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
                await DisplayAlert("Ngôn ngữ", "Chức năng đang phát triển", "OK");
                break;

            case "map":
                await DisplayAlert("Bản đồ hình ảnh", "Chức năng đang phát triển", "OK");
                break;

            default:
                await DisplayAlert(menu.Title, "Chức năng đang phát triển", "OK");
                break;
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Đăng xuất",
            "Bạn có chắc muốn đăng xuất không?",
            "Đăng xuất",
            "Hủy");

        if (!confirm) return;

        // xoá trạng thái login (giả lập)
        Preferences.Remove("isLoggedIn");

        // QUAN TRỌNG: reset toàn bộ Shell
        Application.Current.MainPage = new NavigationPage(new LoginPage());
    }
}
