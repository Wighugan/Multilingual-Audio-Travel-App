using System.Collections.ObjectModel;

namespace multilingualAudioTravelApp;

public class ProfileViewModel
{
    public string UserName { get; set; } = "Người dùng";
    public string UserEmail { get; set; } = "";
    public string WelcomeText => $"Xin chào, {UserName}";
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
                Title = "Chỉnh sửa thông tin",
                Description = "Cập nhật tên, email của bạn"
            },
            new ProfileMenu
            {
                Key = "language",
                Title = "Ngôn ngữ thuyết minh",
                Description = "Chọn ngôn ngữ phát âm thanh thuyết minh"
            },
            new ProfileMenu
            {
                Key = "favorite",
                Title = "Quán yêu thích",
                Description = "Danh sách quán ăn bạn đã bookmark"
            },
            new ProfileMenu
            {
                Key = "feedback",
                Title = "Đánh giá & Góp ý",
                Description = "Gửi phản hồi để cải thiện ứng dụng"
            }
        };
    }
}