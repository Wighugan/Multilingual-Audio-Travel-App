namespace multilingualAudioTravelApp;

public partial class EditProfilePage : ContentPage
{
    public EditProfilePage()
    {
        InitializeComponent();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        string name = NameEntry.Text;
        string email = EmailEntry.Text;
        string phone = PhoneEntry.Text;

        // Giả lập lưu dữ liệu (sau này có thể lưu DB / API)
        await DisplayAlert(
            "Thành công",
            "Thông tin đã được cập nhật",
            "OK"
        );

        // Quay lại trang trước (Profile)
        await Shell.Current.GoToAsync("..");
    }
}
