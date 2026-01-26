namespace multilingualAudioTravelApp;

public partial class ExplorePage : ContentPage
{
    public ExplorePage()
    {
        InitializeComponent();
    }

    private void OnSearchClicked(object sender, EventArgs e)
    {
        DisplayAlert("Search", "Tìm kiếm", "OK");
    }

    private void OnNotifyClicked(object sender, EventArgs e)
    {
        DisplayAlert("Thông báo", "Chưa có thông báo", "OK");
    }
}
