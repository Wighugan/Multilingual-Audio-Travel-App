using System.Text.RegularExpressions;
using multilingualAudioTravelApp.Services;
using System.Net.Http;

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

        // kiểm tra tên hợp lệ hay không
        if (!fullName.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
        {
            ShowError(multilingualAudioTravelApp.Languages.AppStrings.ErrorName);
            return;
        }

        // kiểm tra email đúng dịnh dạng hay không
        if (!IsValidEmail(email))
        {
            ShowError(multilingualAudioTravelApp.Languages.AppStrings.ErrorInvalidEmail);
            return;
        }

        bool isDomainReal = await IsEmailDomainReal(email);
        if (!isDomainReal)
        {
            ShowError(multilingualAudioTravelApp.Languages.AppStrings.ErrorInvalidEmail);
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

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            // Pattern chuẩn kiểm tra định dạng email (VD: abc@xyz.com)
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
    private async Task<bool> IsEmailDomainReal(string email)
    {
        try
        {
            //Tách tên miền
            var parts = email.Split('@');
            if (parts.Length != 2) return false;
            string domain = parts[1];

            // hỏi tên miền có máy chủ nhận Email không
            string url = $"https://dns.google/resolve?name={domain}&type=MX";

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(3);
            var response = await client.GetStringAsync(url);

            if (response.Contains("\"Answer\":"))
            {
                return true;
            }
            return false;
        }
        catch
        {
            return true;
        }
    }
}