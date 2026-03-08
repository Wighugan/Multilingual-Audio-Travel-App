using multilingualAudioTravelApp.Services;
using NetTopologySuite.Triangulate.Tri;

namespace multilingualAudioTravelApp;

public partial class LoginPage : ContentPage
{
    private readonly DatabaseService _dbService = new DatabaseService();

    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ErrorLabel.Text = "Vui lòng nhập email và mật khẩu.";
            ErrorLabel.IsVisible = true;
            return;
        }

        var user = await _dbService.LoginAsync(email, password);

        if (user != null)
        {
            // Lưu trạng thái đăng nhập
            Preferences.Set("isLoggedIn", true);
            Preferences.Set("userEmail", user.Email);
            Preferences.Set("userName", user.FullName);

            Application.Current.MainPage = new AppShell();
        }
        else
        {
            ErrorLabel.Text = "Email hoặc mật khẩu không đúng.";
            ErrorLabel.IsVisible = true;
        }
    }

    private void OnGoToRegisterClicked(object sender, EventArgs e)
    {
        Application.Current.MainPage = new NavigationPage(new RegisterPage());
    }
}

