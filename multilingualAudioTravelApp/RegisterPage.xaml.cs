using multilingualAudioTravelApp.Services;

namespace multilingualAudioTravelApp;

public partial class RegisterPage : ContentPage
{
    private readonly DatabaseService _dbService = new DatabaseService();

    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        var fullName = FullNameEntry.Text?.Trim();
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;
        var confirm = ConfirmPasswordEntry.Text;

        // Kiểm tra nhập đủ thông tin
        if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowError("Vui lòng điền đầy đủ thông tin.");
            return;
        }

        // Kiểm tra mật khẩu khớp
        if (password != confirm)
        {
            ShowError("Mật khẩu xác nhận không khớp.");
            return;
        }

        // Kiểm tra độ dài mật khẩu
        if (password.Length < 6)
        {
            ShowError("Mật khẩu phải có ít nhất 6 ký tự.");
            return;
        }

        var success = await _dbService.RegisterAsync(email, password, fullName);

        if (success)
        {
            await DisplayAlert("Thành công", "Tài khoản đã được tạo! Vui lòng đăng nhập.", "OK");
            Application.Current.MainPage = new NavigationPage(new LoginPage());
        }
        else
        {
            ShowError("Email này đã được sử dụng.");
        }
    }

    private void OnGoToLoginClicked(object sender, EventArgs e)
    {
        Application.Current.MainPage = new NavigationPage(new LoginPage());
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}