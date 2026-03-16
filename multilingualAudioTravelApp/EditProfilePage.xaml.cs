using multilingualAudioTravelApp.Services;

namespace multilingualAudioTravelApp;

public partial class EditProfilePage : ContentPage
{
    private readonly DatabaseService _dbService = new DatabaseService();
    private string _currentEmail;

    public EditProfilePage()
    {
        InitializeComponent();
        LoadCurrentInfo();
    }

    private void LoadCurrentInfo()
    {
        _currentEmail = Preferences.Get("userEmail", "");
        NameEntry.Text = Preferences.Get("userName", "");
        EmailEntry.Text = _currentEmail;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var newName = NameEntry.Text?.Trim();
        var newEmail = EmailEntry.Text?.Trim();
        var newPassword = NewPasswordEntry.Text;
        var confirm = ConfirmPasswordEntry.Text;

        // Kiểm tra tên và email
        if (string.IsNullOrEmpty(newName) || string.IsNullOrEmpty(newEmail))
        {
            ShowError(multilingualAudioTravelApp.Languages.AppStrings.ErrorEmail);
            return;
        }

        // Nếu có nhập mật khẩu mới thì kiểm tra
        if (!string.IsNullOrEmpty(newPassword))
        {
            if (newPassword.Length < 6)
            {
                ShowError(multilingualAudioTravelApp.Languages.AppStrings.ErrorPass);
                return;
            }

            if (newPassword != confirm)
            {
                ShowError(multilingualAudioTravelApp.Languages.AppStrings.ErrorRepass);
                return;
            }
        }

        ErrorLabel.IsVisible = false;

        // Lưu tất cả trong 1 lần
        var success = await _dbService.UpdateProfileAsync(
            _currentEmail,
            newName,
            newEmail,
            string.IsNullOrEmpty(newPassword) ? null : newPassword
        );

        if (success)
        {
            _currentEmail = newEmail;
            await DisplayAlert(multilingualAudioTravelApp.Languages.AppStrings.Success, multilingualAudioTravelApp.Languages.AppStrings.UpdateSuccess, multilingualAudioTravelApp.Languages.AppStrings.ok);
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            ShowError(multilingualAudioTravelApp.Languages.AppStrings.ErrorSameEmail);
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorLabel.IsVisible = true;
    }
}