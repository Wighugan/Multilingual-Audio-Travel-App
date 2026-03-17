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
            ShowError(multilingualAudioTravelApp.Languages.AppStrings.AskFullInfo);
            return;
        }

        // Kiểm tra mật khẩu khớp
        if (password != confirm)
        {
            ShowError(multilingualAudioTravelApp.Languages.AppStrings.ErrorRepass);
            return;
        }

        // Kiểm tra độ dài mật khẩu
        if (password.Length < 6)
        {
            ShowError(multilingualAudioTravelApp.Languages.AppStrings.ErrorPass);
            return;
        }

        var success = await _dbService.RegisterAsync(email, password, fullName);

        if (success)
        {
            await DisplayAlert(multilingualAudioTravelApp.Languages.AppStrings.Success, 
                multilingualAudioTravelApp.Languages.AppStrings.CreateAccSuccess,
                multilingualAudioTravelApp.Languages.AppStrings.ok);
            Application.Current.MainPage = new NavigationPage(new LoginPage());
        }
        else
        {
            ShowError(multilingualAudioTravelApp.Languages.AppStrings.ErrorSameEmail);
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