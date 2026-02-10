using System.Collections.ObjectModel;

namespace multilingualAudioTravelApp;

public class ProfileViewModel
{
    public bool IsDarkMode
    {
        get => Application.Current.UserAppTheme == AppTheme.Dark;
        set
        {
            Application.Current.UserAppTheme =
                value ? AppTheme.Dark : AppTheme.Light;
        }
    }


    public ObservableCollection<ProfileMenu> ProfileMenus { get; set; }

    public ProfileViewModel()
    {
        ProfileMenus = new ObservableCollection<ProfileMenu>
        {
            new ProfileMenu
    {
        Key = "edit",
        Title = "Chỉnh sửa thông tin",
        Description = "Cập nhật tên, email, ảnh đại diện"
    },
            new ProfileMenu
            {
                Title = "Chương trình khuyến mãi",
                Description = "Thông tin các chương trình khuyến mãi"
            },
            new ProfileMenu
            {
                Title = "Cài đặt",
                Description = ""
            },
            new ProfileMenu
            {
                Title = "Ngôn ngữ",
                Description = "Bấm vào biểu tượng để chọn ngôn ngữ hiển thị"
            },
            new ProfileMenu
            {
                Title = "Bản đồ hình ảnh",
                Description = "Những điểm check-in và chụp ảnh đẹp nhất"
            }
        };
    }
}
