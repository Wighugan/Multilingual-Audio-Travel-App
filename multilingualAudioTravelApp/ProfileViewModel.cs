using System.Collections.ObjectModel;

namespace multilingualAudioTravelApp;

public class ProfileViewModel
{
    public string UserName { get; set; } = "Người dùng";
    public string UserEmail { get; set; } = "";
    public string WelcomeText => $"{multilingualAudioTravelApp.Languages.AppStrings.Hello}, {UserName}";
    public string UserInitial => string.IsNullOrEmpty(UserName) ? "?"
        : UserName.Trim().Split(' ').Last().ToUpper()[0].ToString();

    public bool IsDarkMode
    {
        get => Application.Current.UserAppTheme == AppTheme.Dark;
        set => Application.Current.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
    }

    public ObservableCollection<ProfileMenu> ProfileMenus { get; set; }

    public ProfileViewModel()
    {
        UserName = Preferences.Get("userName", "Người dùng");
        UserEmail = Preferences.Get("userEmail", "");

        ProfileMenus = new ObservableCollection<ProfileMenu>
        {
            new ProfileMenu
            {
                Key = "edit",
                Title = multilingualAudioTravelApp.Languages.AppStrings.EditInfo,
                Description = multilingualAudioTravelApp.Languages.AppStrings.DesEditInfo
            },
            new ProfileMenu
            {
                Key = "language",
                Title = multilingualAudioTravelApp.Languages.AppStrings.Language,
                Description = multilingualAudioTravelApp.Languages.AppStrings.DesLanguage
            },
            new ProfileMenu
            {
                Key = "favorite",
                Title = multilingualAudioTravelApp.Languages.AppStrings.FvCafe,
                Description = multilingualAudioTravelApp.Languages.AppStrings.DesFvCafe
            },

            new ProfileMenu { 
                Key = "myqr", 
                Title = multilingualAudioTravelApp.Languages.AppStrings.MyQR, 
                Description = multilingualAudioTravelApp.Languages.AppStrings.MyQRDesc },
            new ProfileMenu
            {
                Key = "feedback",
                Title = multilingualAudioTravelApp.Languages.AppStrings.Feedback,
                Description = multilingualAudioTravelApp.Languages.AppStrings.DesFeedback
            }
        };
    }
}