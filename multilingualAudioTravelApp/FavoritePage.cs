using multilingualAudioTravelApp.Services;
using static multilingualAudioTravelApp.Services.PoiEntity;

namespace multilingualAudioTravelApp;

public partial class FavoritePage : ContentPage
{
    private readonly DatabaseService _dbService = new DatabaseService();

    public FavoritePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadFavorites();
    }

    private async Task LoadFavorites()
    {
        var email = Preferences.Get("userEmail", "");
        var favorites = await _dbService.GetFavoritesAsync(email);
        FavCollection.ItemsSource = favorites;
    }

    private async void OnRemoveFavoriteTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not FavoriteEntity item) return;

        bool confirm = await DisplayAlert(
            "Bỏ yêu thích",
            $"Bỏ \"{item.PoiName}\" khỏi danh sách?",
            "Bỏ lưu", "Hủy");

        if (!confirm) return;

        var email = Preferences.Get("userEmail", "");
        await _dbService.RemoveFavoriteAsync(email, item.PoiName);
        await LoadFavorites();
    }
}
